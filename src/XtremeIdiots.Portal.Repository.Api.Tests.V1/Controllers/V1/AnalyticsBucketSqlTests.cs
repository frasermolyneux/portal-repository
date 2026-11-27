using Microsoft.EntityFrameworkCore;
using Moq;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
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

    [Fact]
    public async Task Timeseries_DistinguishesNoSamplesFromObservedZero()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var from = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var server = new GameServer { GameServerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Title = "Server", Hostname = "host", BanFileRootPath = "" };
        context.GameServers.Add(server);
        context.GameServerStats.Add(new GameServerStat { GameServerStatId = Guid.NewGuid(), GameServerId = server.GameServerId, Timestamp = from, PlayerCount = 0, MapName = "map" });
        await context.SaveChangesAsync();
        IGameAnalyticsApi game = new GameAnalyticsController(context);
        IGlobalAnalyticsApi global = new GlobalAnalyticsController(context, Mock.Of<ILiveStatusStore>());

        var gameResult = (await game.GetTimeseries(GameType.CallOfDuty4, from, from.AddHours(2), AnalyticsBucket.OneHour)).Result!.Data!;
        var globalResult = (await global.GetTimeseries(from, from.AddHours(2), AnalyticsBucket.OneHour)).Result!.Data!;

        Assert.Equal(new int?[] { 1, 0 }, gameResult.Points.Select(p => p.StatsSampleCount));
        Assert.Equal(new int?[] { 1, 0 }, globalResult.Points.Select(p => p.StatsSampleCount));
        Assert.All(gameResult.Points, p => Assert.Equal(0, p.AvgPlayers));
        Assert.All(globalResult.Points, p => Assert.Equal(0, p.AvgPlayers));
    }

    [Theory]
    [InlineData(AnalyticsBucket.FifteenMinutes, 2)]
    [InlineData(AnalyticsBucket.OneHour, 1)]
    [InlineData(AnalyticsBucket.OneDay, 1)]
    [Trait("Category", "SqlIntegration")]
    public Task Timeseries_ReportsSampleCountsAndRecordedStarts(AnalyticsBucket bucket, int expectedNonEmpty) =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            await using var context = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).Options);
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
            Assert.Equal(1000, gameResult.Points.Sum(p => p.StatsSampleCount));
            Assert.Equal(1000, globalResult.Points.Sum(p => p.StatsSampleCount));
            Assert.Equal(1, playerResult.Points.Sum(p => p.ActivePlayers));
            Assert.Equal(1, playerResult.Points.Sum(p => p.SessionStarts));

            var overview = (await players.GetOverview(from, to)).Result!.Data!;
            Assert.Equal(1, overview.RecordedPlayers);
            Assert.Equal(1, overview.SessionStarts);
            var byServer = (await players.GetByServer(from, to, 10)).Result!.Data!;
            Assert.Equal(1, Assert.Single(byServer.Items).ActivePlayers);
        });
}
