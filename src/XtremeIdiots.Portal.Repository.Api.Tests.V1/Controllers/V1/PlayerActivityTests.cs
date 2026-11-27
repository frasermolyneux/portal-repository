using System.Data.Common;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class PlayerActivityTests
{
    [Fact]
    public async Task GetPlayerActivity_RejectsInvalidWindowAndMissingPlayer()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        IPlayerAnalyticsV2Api api = new PlayerAnalyticsV2Controller(context);
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(HttpStatusCode.BadRequest, (await api.GetPlayerActivity(Guid.NewGuid(), from, from.AddDays(90).AddTicks(1), [])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.GetPlayerActivity(Guid.NewGuid(), DateTime.SpecifyKind(from, DateTimeKind.Unspecified), from.AddDays(1), [])).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetPlayerActivity(Guid.NewGuid(), from, from.AddDays(90), [])).StatusCode);
    }

    [Fact]
    [Trait("Category", "SqlIntegration")]
    public Task GetPlayerActivity_ClipsAndScopesRecordedActivity() => SqlServerTestDatabase.RunAsync(async connectionString =>
    {
        await using var context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options);
        IPlayerAnalyticsV2Api api = new PlayerAnalyticsV2Controller(context);
        var midnight = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var from = midnight.AddSeconds(-10.25);
        var to = midnight.AddSeconds(25.5);
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = 1, FirstSeen = from, LastSeen = to };
        var other = new Player { PlayerId = Guid.NewGuid(), GameType = 1, FirstSeen = from, LastSeen = to };
        var primary = Server("Primary");
        var secondary = Server("Secondary");
        var excluded = Server("Excluded");
        var deleted = Server("Deleted");
        deleted.Deleted = true;
        var otherGame = Server("Other game");
        otherGame.GameType = 2;
        context.AddRange(player, other, primary, secondary, excluded, deleted, otherGame);

        var open = Session(primary, player, midnight.AddMinutes(-1), midnight.AddSeconds(12), open: true);
        var unknown = Session(secondary, player, midnight.AddSeconds(20), midnight.AddSeconds(40));
        var stale = Session(secondary, player, from.AddMinutes(-1), from, open: true);
        var bot = Session(primary, player, midnight, midnight.AddSeconds(20));
        bot.IsBot = true;
        var hidden = Session(excluded, player, midnight, to);
        var removed = Session(deleted, player, midnight, to);
        var differentPlayer = Session(primary, other, midnight, to);
        var map = new ServerMapRun { RunId = Guid.NewGuid(), ServerId = primary.GameServerId, GameType = 1, StartUtc = from };
        context.AddRange(open, unknown, stale, bot, hidden, removed, differentPlayer, map);
        context.AddRange(
            Segment(open, midnight.AddSeconds(-30), midnight.AddSeconds(5.25), map.RunId),
            Segment(open, midnight.AddSeconds(5.25), midnight.AddSeconds(12), map.RunId),
            Segment(unknown, midnight.AddSeconds(20), midnight.AddSeconds(40)),
            Segment(bot, midnight, midnight.AddSeconds(20)),
            Segment(hidden, midnight, to),
            Segment(removed, midnight, to),
            Segment(differentPlayer, midnight, to),
            new RecentPlayer { RecentPlayerId = Guid.NewGuid(), PlayerId = player.PlayerId, GameServerId = primary.GameServerId, Name = "Player", Timestamp = midnight.AddSeconds(13) },
            Chat(primary, player, from, locked: true),
            Chat(primary, player, midnight, locked: false),
            Chat(primary, player, to, locked: true),
            Chat(secondary, player, midnight, locked: true),
            Chat(excluded, player, midnight, locked: true),
            Chat(deleted, player, midnight, locked: true),
            Chat(otherGame, player, midnight, locked: true),
            Chat(primary, other, midnight, locked: true));
        await context.SaveChangesAsync();

        var scope = new[] { primary.GameServerId, primary.GameServerId, secondary.GameServerId, deleted.GameServerId, otherGame.GameServerId, Guid.NewGuid() };
        var result = await api.GetPlayerActivity(player.PlayerId, from, to, scope, includeChat: true);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var activity = Assert.IsType<PlayerActivityDto>(result.Result?.Data);
        Assert.Equal(22.25m, activity.MeasuredSeconds);
        Assert.Equal(5.5m, activity.UnknownMapSeconds);
        Assert.Equal(2, activity.TrackedSessions);
        Assert.True(activity.HasOpenSession);
        Assert.Equal(2, activity.ActiveDays);
        Assert.Equal("unknown", activity.PlaytimeCoverage);
        Assert.Collection(activity.Daily,
            day => { Assert.Equal(midnight.AddDays(-1), day.DateUtc); Assert.Equal(10.25m, day.MeasuredSeconds); },
            day => { Assert.Equal(midnight, day.DateUtc); Assert.Equal(12m, day.MeasuredSeconds); });
        Assert.Collection(activity.Servers,
            server => { Assert.Equal(primary.GameServerId, server.ServerId); Assert.Equal(22.25m, server.MeasuredSeconds); Assert.Equal(1, server.TrackedSessions); },
            server => { Assert.Equal(secondary.GameServerId, server.ServerId); Assert.Equal(0m, server.MeasuredSeconds); Assert.Equal(1, server.TrackedSessions); });
        Assert.Equal(midnight.AddSeconds(13), activity.LastObservedUtc);
        Assert.Equal("recentPlayer", activity.LastObservedSource);
        Assert.Equal(2L, activity.ChatMessages);
        Assert.True(activity.ChatRetentionCutoffUtc > to);

        var restricted = (await api.GetPlayerActivity(player.PlayerId, to.AddDays(-90), to, [primary.GameServerId])).Result!.Data!;
        Assert.Null(restricted.ChatMessages);
        Assert.Null(restricted.ChatRetentionCutoffUtc);
        Assert.Equal(1, restricted.TrackedSessions);
        Assert.Equal(42m, restricted.MeasuredSeconds);
        Assert.Equal(0m, restricted.UnknownMapSeconds);
        Assert.Single(restricted.Servers);

        var noScope = (await api.GetPlayerActivity(player.PlayerId, from, to, [], includeChat: true)).Result!.Data!;
        Assert.Empty(noScope.Servers);
        Assert.Empty(noScope.Daily);
        Assert.Null(noScope.LastObservedUtc);
        Assert.Null(noScope.ChatMessages);
        Assert.Equal("unavailable", noScope.PlaytimeCoverage);
    });

    [Theory]
    [InlineData(0, 2L, false)]
    [InlineData(1, 3L, true)]
    [InlineData(2, 1L, true)]
    [Trait("Category", "SqlIntegration")]
    public Task GetPlayerActivity_OnlyReadsLockedForWindowsBeforeRetention(int window, long expectedCount, bool readsLocked) =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var capture = new ChatCommandCapture();
            await using var context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(capture).Options);
            IPlayerAnalyticsV2Api api = new PlayerAnalyticsV2Controller(context);
            var today = DateTime.UtcNow.Date;
            var cutoffDay = today.AddMonths(-6);
            var from = window == 0 ? today.AddDays(-7) : cutoffDay.AddDays(-3);
            var to = window == 0 ? today : window == 1 ? cutoffDay.AddDays(3) : cutoffDay.AddDays(-1);
            var server = Server("Visible");
            var hidden = Server("Hidden");
            var player = new Player { PlayerId = Guid.NewGuid(), GameType = 1, FirstSeen = from, LastSeen = to };
            context.AddRange(server, hidden, player);
            context.AddRange(
                Chat(server, player, cutoffDay.AddDays(-2), locked: true),
                Chat(server, player, cutoffDay.AddDays(-2), locked: false),
                Chat(server, player, cutoffDay.AddDays(2), locked: true),
                Chat(server, player, cutoffDay.AddDays(2), locked: false),
                Chat(server, player, today.AddDays(-1), locked: true),
                Chat(server, player, today.AddDays(-1), locked: false),
                Chat(hidden, player, from.AddHours(1), locked: true),
                Chat(server, player, to, locked: true));
            await context.SaveChangesAsync();

            var result = await api.GetPlayerActivity(player.PlayerId, from, to, [server.GameServerId], includeChat: true);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(expectedCount, result.Result!.Data!.ChatMessages);
            Assert.NotNull(result.Result.Data.ChatRetentionCutoffUtc);
            var sql = Assert.Single(capture.Commands);
            Assert.Equal(readsLocked, sql.Contains("[c].[Locked]", StringComparison.Ordinal));
        });

    private sealed class ChatCommandCapture : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("SELECT COUNT_BIG(*)", StringComparison.Ordinal)
                && command.CommandText.Contains("[ChatMessages]", StringComparison.Ordinal))
            {
                Commands.Add(command.CommandText);
            }
            return new ValueTask<InterceptionResult<DbDataReader>>(result);
        }
    }

    private static GameServer Server(string name) =>
        new() { GameServerId = Guid.NewGuid(), Title = name, GameType = 1, Hostname = name, BanFileRootPath = "" };

    private static PlayerSession Session(GameServer server, Player player, DateTime start, DateTime lastConfirmed, bool open = false) =>
        new()
        {
            SessionId = Guid.NewGuid(),
            ServerId = server.GameServerId,
            PlayerId = player.PlayerId,
            StartUtc = start,
            LastConfirmedUtc = lastConfirmed,
            EndUtc = open ? null : lastConfirmed
        };

    private static PlayerSessionMapSegment Segment(PlayerSession session, DateTime start, DateTime end, Guid? mapRunId = null) =>
        new()
        {
            SegmentId = Guid.NewGuid(),
            SessionId = session.SessionId,
            StartUtc = start,
            EndUtc = end,
            UnknownMap = !mapRunId.HasValue,
            MapRunId = mapRunId
        };

    private static ChatMessage Chat(GameServer server, Player player, DateTime at, bool locked) =>
        new()
        {
            ChatMessageId = Guid.NewGuid(),
            GameServerId = server.GameServerId,
            PlayerId = player.PlayerId,
            Username = "Player",
            Message = "!command",
            Timestamp = at,
            Locked = locked
        };
}
