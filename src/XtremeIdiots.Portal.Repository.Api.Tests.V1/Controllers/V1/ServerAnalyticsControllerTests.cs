using System.Net;

using Moq;

using Xunit;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.LiveStatus;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.Api.V1.TableStorage;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

[Trait("Category", "Unit")]
public class ServerAnalyticsControllerTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly DateTime FromUtc = Now.AddDays(-7);
    private static readonly DateTime ToUtc = Now.AddDays(1);

    private static IServerAnalyticsApi CreateApi(PortalDbContext context)
    {
        var live = new Mock<ILiveStatusStore>();
        live.Setup(x => x.GetServerLiveStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((GameServerLiveStatusDto?)null);
        live.Setup(x => x.GetLivePlayersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<LivePlayerDto>());
        return new ServerAnalyticsController(context, live.Object);
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
    public async Task GetOverview_ReturnsWindowStats()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        context.GameServerStats.Add(new GameServerStat { GameServerStatId = Guid.NewGuid(), GameServerId = server.GameServerId, MapName = "mp_one", PlayerCount = 10, Timestamp = Now.AddHours(-1) });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetOverview(server.GameServerId, FromUtc, ToUtc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(server.GameServerId, result.Result!.Data!.GameServerId);
        Assert.Equal(10, result.Result.Data.PeakPlayers);
    }

    [Fact]
    public async Task GetPlayersCurrent_NotFound_WhenServerMissing()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var result = await CreateApi(context).GetPlayersCurrent(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetPlayersCurrent_ReturnsOfflineSnapshot_WhenNoLiveStatus()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetPlayersCurrent(server.GameServerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(result.Result!.Data!.Online);
        Assert.Empty(result.Result.Data.Players);
    }

    [Fact]
    public async Task GetEventsSummary_GroupsByEventType()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        context.GameServerEvents.Add(new GameServerEvent { GameServerEventId = Guid.NewGuid(), GameServerId = server.GameServerId, EventType = "Kill", Timestamp = Now.AddHours(-1) });
        context.GameServerEvents.Add(new GameServerEvent { GameServerEventId = Guid.NewGuid(), GameServerId = server.GameServerId, EventType = "Kill", Timestamp = Now.AddHours(-1) });
        context.GameServerEvents.Add(new GameServerEvent { GameServerEventId = Guid.NewGuid(), GameServerId = server.GameServerId, EventType = "Join", Timestamp = Now.AddHours(-1) });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetEventsSummary(server.GameServerId, FromUtc, ToUtc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(3, result.Result!.Data!.TotalEvents);
        Assert.Equal("Kill", result.Result.Data.ByType[0].EventType);
        Assert.Equal(2, result.Result.Data.ByType[0].Count);
    }

    [Fact]
    public async Task GetChatSummary_RanksTopChatters()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        var talker = Guid.NewGuid();
        context.ChatMessages.Add(new ChatMessage { ChatMessageId = Guid.NewGuid(), GameServerId = server.GameServerId, PlayerId = talker, Username = "Talker", ChatType = 0, Message = "hi", Timestamp = Now.AddHours(-1), Locked = false });
        context.ChatMessages.Add(new ChatMessage { ChatMessageId = Guid.NewGuid(), GameServerId = server.GameServerId, PlayerId = talker, Username = "Talker", ChatType = 0, Message = "hey", Timestamp = Now.AddHours(-1), Locked = false });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetChatSummary(server.GameServerId, FromUtc, ToUtc, 10);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, result.Result!.Data!.TotalMessages);
        Assert.Equal(1, result.Result.Data.UniqueChatters);
        Assert.Equal("Talker", result.Result.Data.TopChatters[0].Username);
    }

    [Fact]
    public async Task GetMapRotationPerformance_ReturnsPerMapShare()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        context.GameServerStats.Add(new GameServerStat { GameServerStatId = Guid.NewGuid(), GameServerId = server.GameServerId, MapName = "mp_one", PlayerCount = 5, Timestamp = Now.AddHours(-1) });
        context.GameServerStats.Add(new GameServerStat { GameServerStatId = Guid.NewGuid(), GameServerId = server.GameServerId, MapName = "mp_one", PlayerCount = 7, Timestamp = Now.AddHours(-2) });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetMapRotationPerformance(server.GameServerId, FromUtc, ToUtc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("mp_one", result.Result!.Data!.Maps[0].MapName);
        Assert.Equal(100, result.Result.Data.Maps[0].SharePercent);
    }
}
