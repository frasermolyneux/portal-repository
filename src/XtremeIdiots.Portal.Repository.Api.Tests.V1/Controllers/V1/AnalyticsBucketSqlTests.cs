using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.Api.V1.Analytics;
using XtremeIdiots.Portal.Repository.Api.V1.TableStorage;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class AnalyticsBucketSqlTests
{
    [Fact]
    public async Task GameOverview_DistinguishesNoSamplesFromObservedZero()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var server = new GameServer { GameServerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Title = "Server", Hostname = "host", BanFileRootPath = "" };
        context.GameServers.Add(server);
        await context.SaveChangesAsync();
        IGameAnalyticsApi api = new GameAnalyticsController(context);

        var empty = (await api.GetOverview(GameType.CallOfDuty4, from, from.AddDays(1))).Result!.Data!;
        Assert.Equal(0, empty.StatsSampleCount);
        context.GameServerStats.Add(new GameServerStat { GameServerStatId = Guid.NewGuid(), GameServerId = server.GameServerId, Timestamp = from, PlayerCount = 0, MapName = "map" });
        await context.SaveChangesAsync();
        var observed = (await api.GetOverview(GameType.CallOfDuty4, from, from.AddDays(1))).Result!.Data!;
        Assert.Equal(1, observed.StatsSampleCount);
        Assert.Equal(0, observed.AvgPlayers);
    }

    [Theory]
    [InlineData(AnalyticsBucket.FifteenMinutes)]
    [InlineData(AnalyticsBucket.OneHour)]
    [InlineData(AnalyticsBucket.OneDay)]
    public async Task BucketQueries_AggregateRelationalRows(AnalyticsBucket bucket)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>().UseSqlite(connection).Options);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE GameServerStats (GameServerStatId TEXT PRIMARY KEY, GameServerId TEXT, Timestamp TEXT NOT NULL, PlayerCount INTEGER NOT NULL, MapName TEXT NOT NULL)");
        await context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE RecentPlayers (RecentPlayerId TEXT PRIMARY KEY, PlayerId TEXT, GameServerId TEXT, Name TEXT NOT NULL, GameType INTEGER NOT NULL, Timestamp TEXT NOT NULL, IpAddress TEXT, Lat REAL, Long REAL, CountryCode TEXT)");
        var from = new DateTime(2026, 1, 1, 10, 7, 0, DateTimeKind.Utc);
        var player = Guid.NewGuid();
        for (var i = 0; i < 1000; i++)
        {
            context.GameServerStats.Add(new GameServerStat { GameServerStatId = Guid.NewGuid(), Timestamp = from.AddMinutes(i % 2 * 8), PlayerCount = i % 2 == 0 ? 2 : 4, MapName = "map" });
        }
        context.RecentPlayers.AddRange(
            new RecentPlayer { RecentPlayerId = Guid.NewGuid(), PlayerId = player, Name = "P", Timestamp = from },
            new RecentPlayer { RecentPlayerId = Guid.NewGuid(), PlayerId = player, Name = "P", Timestamp = from.AddMinutes(1) });
        await context.SaveChangesAsync();

        var counts = await AnalyticsBucketCounts.CountAsync(context.GameServerStats.Select(s => s.Timestamp), bucket, default);
        var stats = await AnalyticsBucketCounts.AggregatePlayersAsync(context.GameServerStats.Select(s =>
            new AnalyticsBucketCounts.PlayerSample { Timestamp = s.Timestamp, PlayerCount = s.PlayerCount }), bucket, default);
        var distinct = await AnalyticsBucketCounts.CountDistinctPlayersAsync(context.RecentPlayers.Where(p => p.PlayerId != null).Select(p =>
            new AnalyticsBucketCounts.PlayerObservation { Timestamp = p.Timestamp, PlayerId = p.PlayerId!.Value }), bucket, default);

        Assert.Equal(1000, counts.Values.Sum());
        Assert.Equal(bucket == AnalyticsBucket.FifteenMinutes ? 2 : 1, counts.Count);
        Assert.Equal(1000, stats.Values.Sum(s => s.SampleCount));
        Assert.Equal(1, distinct.Values.Sum());
    }

    [Theory]
    [InlineData(AnalyticsBucket.FifteenMinutes)]
    [InlineData(AnalyticsBucket.OneHour)]
    [InlineData(AnalyticsBucket.OneDay)]
    public void BucketQueries_TranslateToSqlServerAggregates(AnalyticsBucket bucket)
    {
        using var context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
            .UseSqlServer("Server=localhost;Database=translation-only;Integrated Security=true;TrustServerCertificate=true").Options);
        var counts = AnalyticsBucketCounts.CountQuery(context.ChatMessages.Select(c => c.Timestamp), bucket).ToQueryString();
        var stats = AnalyticsBucketCounts.PlayersQuery(context.GameServerStats.Select(s =>
            new AnalyticsBucketCounts.PlayerSample { Timestamp = s.Timestamp, PlayerCount = s.PlayerCount }), bucket).ToQueryString();
        var players = AnalyticsBucketCounts.DistinctPlayersQuery(context.RecentPlayers.Where(p => p.PlayerId != null).Select(p =>
            new AnalyticsBucketCounts.PlayerObservation { Timestamp = p.Timestamp, PlayerId = p.PlayerId!.Value }), bucket).ToQueryString();
        var servers = context.RecentPlayers.Where(p => p.PlayerId != null && p.GameServerId != null)
            .GroupBy(p => p.GameServerId!.Value)
            .Select(g => new { GameServerId = g.Key, Active = g.Select(p => p.PlayerId).Distinct().Count() })
            .OrderByDescending(x => x.Active).Take(10).ToQueryString();

        Assert.Contains("GROUP BY", counts, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GROUP BY", stats, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COUNT(DISTINCT", players, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COUNT(DISTINCT", servers, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(AnalyticsBucket.FifteenMinutes, 2)]
    [InlineData(AnalyticsBucket.OneHour, 1)]
    [InlineData(AnalyticsBucket.OneDay, 1)]
    [Trait("Category", "SqlIntegration")]
    public Task Timeseries_AggregatesInSqlAndPreservesBucketValues(AnalyticsBucket bucket, int expectedNonEmpty) =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var capture = new CommandCapture();
            await using var context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(capture).Options);
            var from = new DateTime(2026, 1, 1, 10, 7, 0, DateTimeKind.Utc);
            var to = from.AddMinutes(40);
            var server = new GameServer { GameServerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Title = "Server", Hostname = "host", BanFileRootPath = "" };
            var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, FirstSeen = from, LastSeen = to };
            context.AddRange(server, player);
            for (var i = 0; i < 1000; i++)
            {
                var timestamp = i % 2 == 0 ? from : from.AddMinutes(8);
                context.GameServerStats.Add(new GameServerStat { GameServerStatId = Guid.NewGuid(), GameServerId = server.GameServerId, Timestamp = timestamp, PlayerCount = i % 2 == 0 ? 2 : 4, MapName = "map" });
                context.ChatMessages.Add(new ChatMessage { ChatMessageId = Guid.NewGuid(), PlayerId = player.PlayerId, GameServerId = server.GameServerId, Timestamp = timestamp, Username = "P", Message = "hello" });
            }
            context.RecentPlayers.AddRange(
                new RecentPlayer { RecentPlayerId = Guid.NewGuid(), PlayerId = player.PlayerId, GameServerId = server.GameServerId, Timestamp = from, Name = "P" },
                new RecentPlayer { RecentPlayerId = Guid.NewGuid(), PlayerId = player.PlayerId, GameServerId = server.GameServerId, Timestamp = from.AddMinutes(1), Name = "P" });
            context.PlayerSessions.Add(new PlayerSession { SessionId = Guid.NewGuid(), PlayerId = player.PlayerId, ServerId = server.GameServerId, GameType = (int)GameType.CallOfDuty4, StartUtc = from, EndUtc = from.AddMinutes(1), LastConfirmedUtc = from.AddMinutes(1) });
            await context.SaveChangesAsync();
            capture.Commands.Clear();

            IGameAnalyticsApi game = new GameAnalyticsController(context);
            var gameResult = (await game.GetTimeseries(GameType.CallOfDuty4, from, to, bucket)).Result!.Data!;
            IGlobalAnalyticsApi global = new GlobalAnalyticsController(context, Mock.Of<ILiveStatusStore>());
            var globalResult = (await global.GetTimeseries(from, to, bucket)).Result!.Data!;
            IPlayerAnalyticsV2Api players = new PlayerAnalyticsV2Controller(context);
            var playerResult = (await players.GetTimeseries(from, to, bucket)).Result!.Data!;

            Assert.Equal(1000, gameResult.Points.Sum(p => p.ChatCount));
            Assert.Equal(1000, globalResult.Points.Sum(p => p.ChatCount));
            Assert.Equal(expectedNonEmpty, gameResult.Points.Count(p => p.ChatCount > 0));
            Assert.Equal(expectedNonEmpty, globalResult.Points.Count(p => p.ChatCount > 0));
            Assert.Equal(bucket == AnalyticsBucket.FifteenMinutes ? new[] { 2d, 4d } : [3d],
                gameResult.Points.Where(p => p.ChatCount > 0).Select(p => p.AvgPlayers));
            Assert.Equal(gameResult.Points.Select(p => p.AvgPlayers), globalResult.Points.Select(p => p.AvgPlayers));
            Assert.Equal(1, playerResult.Points.Sum(p => p.ActivePlayers));
            Assert.Equal(1, playerResult.Points.Sum(p => p.SessionStarts));
            Assert.Equal(9, capture.Commands.Count);
            Assert.All(capture.Commands, sql => Assert.Contains("GROUP BY", sql, StringComparison.OrdinalIgnoreCase));

            var overview = (await players.GetOverview(from, to)).Result!.Data!;
            Assert.Equal(1, overview.RecordedPlayers);
            Assert.Equal(1, overview.SessionStarts);
            var byServer = (await players.GetByServer(from, to, 10)).Result!.Data!;
            Assert.Equal(1, Assert.Single(byServer.Items).ActivePlayers);
        });

    private sealed class CommandCapture : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
