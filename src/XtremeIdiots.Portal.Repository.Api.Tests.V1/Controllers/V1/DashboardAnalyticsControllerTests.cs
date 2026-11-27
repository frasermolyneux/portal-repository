using System.Net;

using Microsoft.EntityFrameworkCore;
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
        Assert.NotEmpty(result.Result.Data.Composition!.TopServers);
        Assert.NotEmpty(result.Result.Data.TrendPoints);
    }

    [Fact]
    public async Task GetHome_WithoutComposition_ReturnsRecordedActivityAndNullComposition()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var result = await CreateApi(context).GetHome(FromUtc, ToUtc, AnalyticsBucket.OneDay, 10, includeComposition: false);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.Result!.Data!.Composition);
        Assert.NotEmpty(result.Result.Data.RecordedActivity!);
        Assert.Equal("unknown", result.Result.Data.RecordedActivityCoverage);
    }

    [Fact]
    public async Task GetHome_InvalidWindow_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var result = await CreateApi(context).GetHome(ToUtc, FromUtc, AnalyticsBucket.OneDay, 10);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GetHome_RecordedActivity_ClipsSessionsAndExcludesKnownBots()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        var day = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var playerId = Guid.NewGuid();
        var first = new PlayerSession
        {
            SessionId = Guid.NewGuid(),
            ServerId = server.GameServerId,
            PlayerId = playerId,
            StartUtc = day.AddHours(-1),
            LastConfirmedUtc = day.AddHours(2)
        };
        var second = new PlayerSession
        {
            SessionId = Guid.NewGuid(),
            ServerId = server.GameServerId,
            PlayerId = playerId,
            StartUtc = day.AddHours(1),
            LastConfirmedUtc = day.AddMinutes(90),
            EndUtc = day.AddHours(3),
            IsBot = false
        };
        var bot = new PlayerSession
        {
            SessionId = Guid.NewGuid(),
            ServerId = server.GameServerId,
            PlayerId = Guid.NewGuid(),
            StartUtc = day,
            LastConfirmedUtc = day.AddHours(2),
            IsBot = true
        };
        context.PlayerSessions.AddRange(first, second, bot);
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetHome(day.AddMinutes(30), day.AddHours(2), AnalyticsBucket.OneHour, 10);
        var points = result.Result!.Data!.RecordedActivity!;

        Assert.Equal(2, points.Count);
        Assert.Equal("unknown", result.Result.Data.RecordedActivityCoverage);
        Assert.Equal((1, 0, 1800m), (points[0].RecordedPlayers, points[0].SessionStarts, points[0].ObservedSeconds));
        Assert.Equal((1, 1, 5400m), (points[1].RecordedPlayers, points[1].SessionStarts, points[1].ObservedSeconds));
    }

    [Fact]
    public async Task GetServer_RecordedActivity_ExcludesOtherServersAndDoesNotExtrapolateOpenSessions()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        var other = SeedServer(context);
        var day = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        context.PlayerSessions.AddRange(
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, PlayerId = Guid.NewGuid(), StartUtc = day, LastConfirmedUtc = day.AddHours(1) },
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = other.GameServerId, PlayerId = Guid.NewGuid(), StartUtc = day, LastConfirmedUtc = day.AddHours(2) },
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, PlayerId = Guid.NewGuid(), StartUtc = day.AddHours(1), LastConfirmedUtc = day.AddHours(1) },
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, PlayerId = Guid.NewGuid(), StartUtc = day.AddHours(2), LastConfirmedUtc = day.AddHours(2) });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetServer(server.GameServerId, day, day.AddHours(2), AnalyticsBucket.OneHour);
        var points = result.Result!.Data!.RecordedActivity!;

        Assert.Equal(1, points[0].RecordedPlayers);
        Assert.Equal(1, points[0].SessionStarts);
        Assert.Equal(1, points[1].RecordedPlayers);
        Assert.Equal(1, points[1].SessionStarts);
        Assert.Equal(3600m, points[0].ObservedSeconds);
        Assert.Equal(0m, points[1].ObservedSeconds);
    }

    [Fact]
    [Trait("Category", "SqlIntegration")]
    public Task GetHome_RecordedActivity_UsesSqlServerWithClippedLastConfirmation() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            await using var context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options);
            var server = SeedServer(context);
            var deleted = SeedServer(context);
            deleted.Deleted = true;
            var day = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, FirstSeen = day, LastSeen = day };
            var bot = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, FirstSeen = day, LastSeen = day };
            var instantaneous = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, FirstSeen = day, LastSeen = day };
            context.Players.AddRange(player, bot, instantaneous);
            context.PlayerSessions.AddRange(
                new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, PlayerId = player.PlayerId, StartUtc = day.AddMinutes(-30), LastConfirmedUtc = day.AddMinutes(90), EndUtc = day.AddHours(3) },
                new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, PlayerId = bot.PlayerId, StartUtc = day, LastConfirmedUtc = day.AddHours(2), IsBot = true },
                new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, PlayerId = instantaneous.PlayerId, StartUtc = day.AddHours(1), LastConfirmedUtc = day.AddHours(1) },
                new PlayerSession { SessionId = Guid.NewGuid(), ServerId = deleted.GameServerId, PlayerId = player.PlayerId, StartUtc = day, LastConfirmedUtc = day.AddHours(2) });
            context.GameServerStats.Add(new GameServerStat { GameServerStatId = Guid.NewGuid(), GameServerId = server.GameServerId, Timestamp = day, PlayerCount = 7, MapName = "mp_one" });
            await context.SaveChangesAsync();

            var api = CreateApi(context);
            var home = await api.GetHome(day.AddMinutes(30), day.AddHours(2), AnalyticsBucket.OneHour, 10);
            var points = home.Result!.Data!.RecordedActivity!;
            Assert.Equal("unknown", home.Result.Data.RecordedActivityCoverage);
            Assert.Equal((1, 0, 1800m), (points[0].RecordedPlayers, points[0].SessionStarts, points[0].ObservedSeconds));
            Assert.Equal((2, 1, 1800m), (points[1].RecordedPlayers, points[1].SessionStarts, points[1].ObservedSeconds));

            var serverResult = await api.GetServer(server.GameServerId, day, day.AddHours(2), AnalyticsBucket.OneHour);
            Assert.Equal(7, serverResult.Result!.Data!.PeakPlayers);
            Assert.Equal(3600m, serverResult.Result.Data.RecordedActivity![0].ObservedSeconds);
            Assert.Equal(1800m, serverResult.Result.Data.RecordedActivity[1].ObservedSeconds);
        });

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
