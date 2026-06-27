using System.Net;

using Xunit;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

[Trait("Category", "Unit")]
public class MapAnalyticsControllerTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly DateTime FromUtc = Now.AddDays(-7);
    private static readonly DateTime ToUtc = Now.AddDays(1);

    private static IMapAnalyticsApi CreateApi(PortalDbContext context) => new MapAnalyticsController(context);

    private static GameServer SeedServer(PortalDbContext context, GameType gameType = GameType.CallOfDuty4)
    {
        var server = new GameServer
        {
            GameServerId = Guid.NewGuid(),
            Title = "Server",
            GameType = (int)gameType,
            Hostname = "host",
            BanFileRootPath = "",
            Deleted = false
        };
        context.GameServers.Add(server);
        return server;
    }

    private static void SeedStat(PortalDbContext context, Guid serverId, string mapName, int playerCount)
    {
        context.GameServerStats.Add(new GameServerStat
        {
            GameServerStatId = Guid.NewGuid(),
            GameServerId = serverId,
            MapName = mapName,
            PlayerCount = playerCount,
            Timestamp = Now.AddHours(-1)
        });
    }

    [Fact]
    public async Task GetOverview_AggregatesPlaysMapsAndVotes()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        SeedStat(context, server.GameServerId, "mp_one", 5);
        SeedStat(context, server.GameServerId, "mp_two", 8);
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = Guid.NewGuid(), PlayerId = Guid.NewGuid(), GameServerId = server.GameServerId, Like = true, Timestamp = Now.AddHours(-1) });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetOverview(FromUtc, ToUtc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, result.Result!.Data!.TotalPlays);
        Assert.Equal(2, result.Result.Data.TotalMaps);
        Assert.Equal(1, result.Result.Data.TotalVotes);
    }

    [Fact]
    public async Task GetHotspots_OrdersByAvgPlayers()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        SeedStat(context, server.GameServerId, "busy", 20);
        SeedStat(context, server.GameServerId, "quiet", 2);
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetHotspots(FromUtc, ToUtc, 10);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("busy", result.Result!.Data!.Items[0].MapName);
    }

    [Fact]
    public async Task GetTopPlayed_ComputesSharePercent()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        SeedStat(context, server.GameServerId, "a", 5);
        SeedStat(context, server.GameServerId, "a", 5);
        SeedStat(context, server.GameServerId, "b", 5);
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetTopPlayed(FromUtc, ToUtc, 10);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("a", result.Result!.Data!.Items[0].MapName);
        Assert.Equal(2, result.Result.Data.Items[0].PlaysCount);
    }

    [Fact]
    public async Task GetTopVoted_ResolvesMapName()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var mapId = Guid.NewGuid();
        context.Maps.Add(new Map { MapId = mapId, MapName = "mp_voted", GameType = (int)GameType.CallOfDuty4 });
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = mapId, PlayerId = Guid.NewGuid(), GameServerId = Guid.NewGuid(), Like = true, Timestamp = Now.AddHours(-1) });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetTopVoted(FromUtc, ToUtc, 10);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("mp_voted", result.Result!.Data!.Items[0].MapName);
        Assert.Equal(1, result.Result.Data.Items[0].VotesCount);
    }

    [Fact]
    public async Task GetByServer_NotFound_WhenServerMissing()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var result = await CreateApi(context).GetByServer(Guid.NewGuid(), FromUtc, ToUtc, 10);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetByServer_ReturnsMapBreakdown()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context);
        SeedStat(context, server.GameServerId, "mp_one", 5);
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetByServer(server.GameServerId, FromUtc, ToUtc, 10);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("mp_one", result.Result!.Data!.Items[0].MapName);
    }

    [Fact]
    public async Task GetMapDetail_NotFound_WhenMapMissing()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var result = await CreateApi(context).GetMapDetail(Guid.NewGuid(), FromUtc, ToUtc);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetMapDetail_ReturnsDetail_WhenMapExists()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var mapId = Guid.NewGuid();
        context.Maps.Add(new Map { MapId = mapId, MapName = "mp_detail", GameType = (int)GameType.CallOfDuty4 });
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetMapDetail(mapId, FromUtc, ToUtc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("mp_detail", result.Result!.Data!.MapName);
    }

    [Fact]
    public async Task GetByGame_AggregatesPlaysAndMapsPerGame()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = SeedServer(context, GameType.CallOfDuty4);
        SeedStat(context, server.GameServerId, "mp_one", 5);
        SeedStat(context, server.GameServerId, "mp_two", 5);
        await context.SaveChangesAsync();

        var result = await CreateApi(context).GetByGame(FromUtc, ToUtc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var item = Assert.Single(result.Result!.Data!.Items);
        Assert.Equal(GameType.CallOfDuty4, item.GameType);
        Assert.Equal(2, item.TotalPlays);
        Assert.Equal(2, item.MapsPlayed);
    }
}
