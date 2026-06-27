using System.Net;

using Moq;

using Xunit;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.LiveStatus;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.Api.V1.TableStorage;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

[Trait("Category", "Unit")]
public class DashboardAnalyticsControllerTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly DateTime FromUtc = Now.AddDays(-7);
    private static readonly DateTime ToUtc = Now.AddDays(1);

    private static IDashboardAnalyticsApi CreateApi(PortalDbContext context)
    {
        var live = new Mock<ILiveStatusStore>();
        live.Setup(x => x.GetServerLiveStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((GameServerLiveStatusDto?)null);
        live.Setup(x => x.GetLivePlayersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<LivePlayerDto>());
        return new DashboardAnalyticsController(context, live.Object);
    }

    private static GameServer SeedServer(PortalDbContext context)
    {
        var server = new GameServer
        {
            GameServerId = Guid.NewGuid(),
            Title = "Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "host",
            BanFileRootPath = "",
            Deleted = false
        };
        context.GameServers.Add(server);
        return server;
    }

    [Fact]
    public async Task GetHome_ReturnsSummaryTrendAndComposition()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        context.GameServerStats.Add(new GameServerStat { GameServerStatId = Guid.NewGuid(), GameServerId = server.GameServerId, MapName = "mp_one", PlayerCount = 6, Timestamp = Now.AddHours(-1) });
        context.Players.Add(new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "P", FirstSeen = Now.AddDays(-2), LastSeen = Now });
        context.RecentPlayers.Add(new RecentPlayer { RecentPlayerId = Guid.NewGuid(), PlayerId = Guid.NewGuid(), GameServerId = server.GameServerId, GameType = (int)GameType.CallOfDuty4, Name = "P", Timestamp = Now.AddHours(-1) });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetHome(FromUtc, ToUtc, AnalyticsBucket.OneDay, 10);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(1, result.Result!.Data!.Summary.ActiveServersCount);
        Assert.NotEmpty(result.Result.Data.Composition.TopServers);
        Assert.NotEmpty(result.Result.Data.TrendPoints);
    }

    [Fact]
    public async Task GetHome_InvalidWindow_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var result = await CreateApi(context).GetHome(ToUtc, FromUtc, AnalyticsBucket.OneDay, 10);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GetServer_NotFound_WhenServerMissing()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var result = await CreateApi(context).GetServer(Guid.NewGuid(), FromUtc, ToUtc, AnalyticsBucket.OneDay);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetServer_ReturnsSnapshot_WhenServerExists()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        context.GameServerStats.Add(new GameServerStat { GameServerStatId = Guid.NewGuid(), GameServerId = server.GameServerId, MapName = "mp_one", PlayerCount = 9, Timestamp = Now.AddHours(-1) });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetServer(server.GameServerId, FromUtc, ToUtc, AnalyticsBucket.OneDay);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(server.GameServerId, result.Result!.Data!.GameServerId);
        Assert.Equal(9, result.Result.Data.PeakPlayers);
    }
}
