using System.Net;

using Newtonsoft.Json;
using Xunit;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

[Trait("Category", "Unit")]
public class PlayerAnalyticsV2ControllerTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly DateTime FromUtc = Now.AddDays(-7);
    private static readonly DateTime ToUtc = Now.AddDays(1);

    private static IPlayerAnalyticsV2Api CreateApi(PortalDbContext context) => new PlayerAnalyticsV2Controller(context);

    [Fact]
    public void AdditiveMetrics_AreUnavailableWhenOlderApiOmitsThem()
    {
        var overview = JsonConvert.DeserializeObject<PlayersOverviewDto>("{}")!;
        var point = JsonConvert.DeserializeObject<PlayersTimeseriesPointDto>("{}")!;

        Assert.Null(overview.ObservedPlayerRecords);
        Assert.Null(overview.RecordedPlayers);
        Assert.Null(overview.SessionStarts);
        Assert.Null(overview.TrackingCoverage);
        Assert.Null(point.SessionStarts);
    }

    private static void AddRecentPlayer(PortalDbContext context, Guid playerId, DateTime timestamp)
    {
        context.RecentPlayers.Add(new RecentPlayer
        {
            RecentPlayerId = Guid.NewGuid(),
            PlayerId = playerId,
            GameServerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Name = "P",
            Timestamp = timestamp
        });
    }

    [Fact]
    public async Task GetOverview_ClassifiesNewActiveAndReturningPlayers()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Players.Add(new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "New", FirstSeen = Now.AddDays(-2), LastSeen = Now.AddHours(-1) });
        context.Players.Add(new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "Returning", FirstSeen = Now.AddDays(-30), LastSeen = Now.AddHours(-2) });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetOverview(FromUtc, ToUtc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, result.Result!.Data!.ActivePlayers);
        Assert.Equal(1, result.Result.Data.NewPlayers);
        Assert.Equal(1, result.Result.Data.ReturningPlayers);
    }

    [Fact]
    public async Task GetOverview_InvalidWindow_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var result = await CreateApi(context).GetOverview(ToUtc, FromUtc);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GetOverview_ReportsRecordedSessionsAndRetainedPlayerRecordsSeparately()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(1);
        var player = Guid.NewGuid();
        var bot = Guid.NewGuid();
        var server = new GameServer { GameServerId = Guid.NewGuid(), Title = "S", Hostname = "host", BanFileRootPath = "" };
        var deleted = new GameServer { GameServerId = Guid.NewGuid(), Title = "Deleted", Hostname = "host", BanFileRootPath = "", Deleted = true };
        context.GameServers.AddRange(server, deleted);
        context.Players.AddRange(
            new Player { PlayerId = player, FirstSeen = from.AddDays(-10), LastSeen = to.AddDays(1), GameType = 1 },
            new Player { PlayerId = bot, FirstSeen = from, LastSeen = from, GameType = 1 },
            new Player { PlayerId = Guid.NewGuid(), FirstSeen = to, LastSeen = to, GameType = 1 });
        context.PlayerSessions.AddRange(
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, PlayerId = player, StartUtc = from.AddHours(-1), EndUtc = from.AddHours(1), LastConfirmedUtc = from.AddHours(1) },
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, PlayerId = player, StartUtc = from, EndUtc = to, LastConfirmedUtc = to },
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, PlayerId = player, StartUtc = to, LastConfirmedUtc = to },
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, PlayerId = bot, StartUtc = from, EndUtc = to, LastConfirmedUtc = to, IsBot = true },
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = deleted.GameServerId, PlayerId = bot, StartUtc = from, EndUtc = to, LastConfirmedUtc = to });
        await context.SaveChangesAsync();

        var result = (await CreateApi(context).GetOverview(from, to)).Result!.Data!;

        Assert.Equal(2, result.ObservedPlayerRecords);
        Assert.Equal(1, result.RecordedPlayers);
        Assert.Equal(1, result.SessionStarts);
        Assert.Equal("unknown", result.TrackingCoverage);
    }

    [Fact]
    public async Task GetTimeseries_CountsRecordedStartsInUtcBucketsWithoutKnownBots()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var from = new DateTime(2026, 1, 1, 10, 7, 0, DateTimeKind.Utc);
        var to = from.AddMinutes(40);
        var server = new GameServer { GameServerId = Guid.NewGuid(), Title = "S", Hostname = "host", BanFileRootPath = "" };
        var deleted = new GameServer { GameServerId = Guid.NewGuid(), Title = "Deleted", Hostname = "host", BanFileRootPath = "", Deleted = true };
        context.GameServers.AddRange(server, deleted);
        context.PlayerSessions.AddRange(
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, StartUtc = from, LastConfirmedUtc = from },
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, StartUtc = from.AddMinutes(8), LastConfirmedUtc = from.AddMinutes(8) },
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, StartUtc = from.AddMinutes(20), LastConfirmedUtc = from.AddMinutes(20), IsBot = true },
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = server.GameServerId, StartUtc = to, LastConfirmedUtc = to },
            new PlayerSession { SessionId = Guid.NewGuid(), ServerId = deleted.GameServerId, StartUtc = from, LastConfirmedUtc = from });
        await context.SaveChangesAsync();

        var result = (await CreateApi(context).GetTimeseries(from, to, AnalyticsBucket.FifteenMinutes)).Result!.Data!;

        Assert.Equal("unknown", result.TrackingCoverage);
        Assert.Equal(new int?[] { 1, 1, 0, 0 }, result.Points.Select(p => p.SessionStarts));
    }

    [Fact]
    public async Task GetTimeseries_ReturnsSeriesWithNewAndActive()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player { PlayerId = playerId, GameType = (int)GameType.CallOfDuty4, Username = "P", FirstSeen = Now.AddDays(-2), LastSeen = Now.AddHours(-1) });
        AddRecentPlayer(context, playerId, Now.AddHours(-1));
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetTimeseries(FromUtc, ToUtc, AnalyticsBucket.OneDay);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Contains(result.Result!.Data!.Series, s => s.Key == "newPlayers");
        Assert.Contains(result.Result.Data.Series, s => s.Key == "activePlayers");
    }

    [Fact]
    public async Task GetTop_RanksBySessions()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var busy = Guid.NewGuid();
        var quiet = Guid.NewGuid();
        context.Players.Add(new Player { PlayerId = busy, GameType = (int)GameType.CallOfDuty4, Username = "Busy", FirstSeen = Now.AddDays(-5), LastSeen = Now });
        context.Players.Add(new Player { PlayerId = quiet, GameType = (int)GameType.CallOfDuty4, Username = "Quiet", FirstSeen = Now.AddDays(-5), LastSeen = Now });
        for (var i = 0; i < 3; i++)
        {
            AddRecentPlayer(context, busy, Now.AddHours(-i));
        }
        AddRecentPlayer(context, quiet, Now.AddHours(-1));
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetTop(FromUtc, ToUtc, 10);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(busy, result.Result!.Data!.Items[0].PlayerId);
        Assert.Equal(3, result.Result.Data.Items[0].SessionsCount);
    }

    [Fact]
    public async Task GetByGame_GroupsByGameType()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Players.Add(new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "A", FirstSeen = Now.AddDays(-2), LastSeen = Now });
        context.Players.Add(new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty2, Username = "B", FirstSeen = Now.AddDays(-30), LastSeen = Now });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetByGame(FromUtc, ToUtc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, result.Result!.Data!.Items.Count);
    }

    [Fact]
    public async Task GetByGame_ObservedRecordsIncludePlayersLastSeenOutsideWindow()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Players.AddRange(
            new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, FirstSeen = FromUtc, LastSeen = FromUtc },
            new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, FirstSeen = FromUtc.AddDays(-20), LastSeen = FromUtc.AddDays(-10) },
            new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, FirstSeen = ToUtc, LastSeen = ToUtc });
        await context.SaveChangesAsync();

        var result = (await CreateApi(context).GetByGame(FromUtc, ToUtc)).Result!.Data!;

        var game = Assert.Single(result.Items);
        Assert.Equal(1, game.ActivePlayers);
        Assert.Equal(2, game.ObservedPlayerRecords);
    }

    [Fact]
    public async Task GetPlayerDetail_NotFound_WhenPlayerMissing()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var result = await CreateApi(context).GetPlayerDetail(Guid.NewGuid(), FromUtc, ToUtc);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetPlayerDetail_ReturnsDetail_WhenPlayerExists()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player { PlayerId = playerId, GameType = (int)GameType.CallOfDuty4, Username = "Detail", FirstSeen = Now.AddDays(-5), LastSeen = Now });
        AddRecentPlayer(context, playerId, Now.AddHours(-1));
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetPlayerDetail(playerId, FromUtc, ToUtc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(playerId, result.Result!.Data!.PlayerId);
        Assert.Equal(1, result.Result.Data.SessionsCount);
    }

    [Fact]
    public async Task GetPlayerTimeseries_NotFound_WhenPlayerMissing()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var result = await CreateApi(context).GetPlayerTimeseries(Guid.NewGuid(), FromUtc, ToUtc, AnalyticsBucket.OneDay);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetByServer_CountsActivePlayersPerServer()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = Guid.NewGuid();
        context.GameServers.Add(new GameServer { GameServerId = serverId, Title = "S", GameType = (int)GameType.CallOfDuty4, Hostname = "h", BanFileRootPath = "", Deleted = false });
        context.RecentPlayers.Add(new RecentPlayer { RecentPlayerId = Guid.NewGuid(), PlayerId = Guid.NewGuid(), GameServerId = serverId, GameType = (int)GameType.CallOfDuty4, Name = "P", Timestamp = Now.AddHours(-1) });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetByServer(FromUtc, ToUtc, 10);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var item = Assert.Single(result.Result!.Data!.Items);
        Assert.Equal(serverId, item.GameServerId);
        Assert.Equal(1, item.ActivePlayers);
    }

    [Fact]
    public async Task GetTimeseries_PreviousPeriod_ProducesComparisonSeriesAndSummary()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var player = Guid.NewGuid();
        context.Players.Add(new Player { PlayerId = player, GameType = (int)GameType.CallOfDuty4, Username = "P", FirstSeen = Now.AddDays(-30), LastSeen = Now });
        AddRecentPlayer(context, player, Now.AddDays(-1));
        AddRecentPlayer(context, player, Now.AddDays(-8));
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetTimeseries(Now.AddDays(-7), Now.AddDays(1), AnalyticsBucket.OneDay, AnalyticsCompareMode.PreviousPeriod, 1, AnalyticsAlignMode.None, "UTC", false);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Contains(result.Result!.Data!.Series, s => s.Role == "comparison");
        Assert.NotNull(result.Result.Data.Summary);
    }
}
