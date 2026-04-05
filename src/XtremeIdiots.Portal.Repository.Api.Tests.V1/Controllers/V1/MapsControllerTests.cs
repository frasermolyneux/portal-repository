using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class MapsControllerTests
{
    private MapsController CreateController(PortalDbContext context)
    {
        var logger = new Mock<ILogger<MapsController>>();
        return new MapsController(context, logger.Object);
    }

    [Fact]
    public async Task GetMap_ById_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var mapId = Guid.NewGuid();
        context.Maps.Add(new Map
        {
            MapId = mapId,
            GameType = (int)GameType.CallOfDuty4,
            MapName = "mp_testmap"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMap(mapId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetMap_ById_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMap(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetMap_ByGameTypeAndName_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Maps.Add(new Map
        {
            MapId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            MapName = "mp_testmap"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMap(GameType.CallOfDuty4, "mp_testmap");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetMap_ByGameTypeAndName_NotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMap(GameType.CallOfDuty4, "nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetMaps_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Maps.Add(new Map
        {
            MapId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            MapName = "mp_map1"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMaps(null, null, null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task CreateMap_CreatesEntity()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IMapsApi)controller;

        var dto = new CreateMapDto(GameType.CallOfDuty4, "mp_newmap");

        var result = await api.CreateMap(dto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.Maps);
    }

    [Fact]
    public async Task DeleteMap_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.DeleteMap(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task DeleteMap_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var mapId = Guid.NewGuid();
        context.Maps.Add(new Map
        {
            MapId = mapId,
            GameType = (int)GameType.CallOfDuty4,
            MapName = "mp_delete"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.DeleteMap(mapId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetMapVotes_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var map = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_testmap" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "TestPlayer", Guid = "test-guid" };
        context.Maps.Add(map);
        context.Players.Add(player);
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map.MapId, PlayerId = player.PlayerId, Like = true, Timestamp = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMapVotes(null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Result?.Data?.Items);
        Assert.Single(result.Result.Data.Items);
    }

    [Fact]
    public async Task GetMapVotes_EmptyResult_ReturnsEmptyCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMapVotes(null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Result?.Data?.Items);
        Assert.Empty(result.Result.Data.Items);
    }

    [Fact]
    public async Task GetMapVotes_FilterByGameType_ReturnsFilteredResults()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var map1 = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_cod4map" };
        var map2 = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty5, MapName = "mp_cod5map" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "TestPlayer", Guid = "test-guid" };
        context.Maps.AddRange(map1, map2);
        context.Players.Add(player);
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map1.MapId, PlayerId = player.PlayerId, Like = true, Timestamp = DateTime.UtcNow });
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map2.MapId, PlayerId = player.PlayerId, Like = false, Timestamp = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMapVotes(GameType.CallOfDuty4, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(result.Result!.Data!.Items!);
    }

    [Fact]
    public async Task GetMapVotes_FilterByMapId_ReturnsFilteredResults()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var map1 = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_map1" };
        var map2 = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_map2" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "TestPlayer", Guid = "test-guid" };
        context.Maps.AddRange(map1, map2);
        context.Players.Add(player);
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map1.MapId, PlayerId = player.PlayerId, Like = true, Timestamp = DateTime.UtcNow });
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map2.MapId, PlayerId = player.PlayerId, Like = false, Timestamp = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMapVotes(null, map1.MapId, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(result.Result!.Data!.Items!);
        Assert.True(result.Result.Data.Items!.First().Like);
    }

    [Fact]
    public async Task GetMapVotes_FilterByGameTypeAndMapId_ReturnsFilteredResults()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var map1 = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_map1" };
        var map2 = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty5, MapName = "mp_map2" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "TestPlayer", Guid = "test-guid" };
        context.Maps.AddRange(map1, map2);
        context.Players.Add(player);
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map1.MapId, PlayerId = player.PlayerId, Like = true, Timestamp = DateTime.UtcNow });
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map2.MapId, PlayerId = player.PlayerId, Like = false, Timestamp = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMapVotes(GameType.CallOfDuty4, map1.MapId, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(result.Result!.Data!.Items!);
    }

    [Fact]
    public async Task GetMapVotes_Pagination_RespectsSkipAndTake()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var map = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_testmap" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "TestPlayer", Guid = "test-guid" };
        context.Maps.Add(map);
        context.Players.Add(player);
        for (int i = 0; i < 5; i++)
        {
            context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map.MapId, PlayerId = player.PlayerId, Like = true, Timestamp = DateTime.UtcNow.AddMinutes(-i) });
        }
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMapVotes(null, null, 1, 2, MapVotesOrder.TimestampDesc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, result.Result!.Data!.Items!.Count());
    }

    [Fact]
    public async Task GetMapVotes_DefaultOrder_ReturnsTimestampDesc()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var map = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_testmap" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "TestPlayer", Guid = "test-guid" };
        context.Maps.Add(map);
        context.Players.Add(player);
        var older = new MapVote { MapVoteId = Guid.NewGuid(), MapId = map.MapId, PlayerId = player.PlayerId, Like = false, Timestamp = DateTime.UtcNow.AddHours(-1) };
        var newer = new MapVote { MapVoteId = Guid.NewGuid(), MapId = map.MapId, PlayerId = player.PlayerId, Like = true, Timestamp = DateTime.UtcNow };
        context.MapVotes.AddRange(older, newer);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMapVotes(null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Equal(2, items.Count);
        Assert.True(items[0].Timestamp >= items[1].Timestamp);
    }

    [Fact]
    public async Task GetMapVotes_WithNullGameServerId_ReturnsResult()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var map = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_testmap" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "TestPlayer", Guid = "test-guid" };
        context.Maps.Add(map);
        context.Players.Add(player);
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map.MapId, PlayerId = player.PlayerId, GameServerId = null, Like = true, Timestamp = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMapVotes(null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(result.Result!.Data!.Items!);
        Assert.Null(result.Result.Data.Items!.First().GameServerId);
    }

    [Fact]
    public async Task GetMapVotes_IncludesNavigationProperties()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var map = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_testmap" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "TestPlayer", Guid = "test-guid" };
        context.Maps.Add(map);
        context.Players.Add(player);
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map.MapId, PlayerId = player.PlayerId, Like = true, Timestamp = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMapVotes(null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var vote = result.Result!.Data!.Items!.First();
        Assert.NotNull(vote.Map);
        Assert.NotNull(vote.Player);
        Assert.Equal("mp_testmap", vote.Map!.MapName);
        Assert.Equal("TestPlayer", vote.Player!.Username);
    }

    [Fact]
    public async Task GetMapVotes_OrderByMapNameAsc_ReturnsSorted()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var mapA = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_alpha" };
        var mapZ = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_zulu" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "TestPlayer", Guid = "test-guid" };
        context.Maps.AddRange(mapA, mapZ);
        context.Players.Add(player);
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = mapZ.MapId, PlayerId = player.PlayerId, Like = true, Timestamp = DateTime.UtcNow });
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = mapA.MapId, PlayerId = player.PlayerId, Like = false, Timestamp = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMapVotes(null, null, 0, 20, MapVotesOrder.MapNameAsc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("mp_alpha", items[0].Map!.MapName);
        Assert.Equal("mp_zulu", items[1].Map!.MapName);
    }

    [Fact]
    public async Task GetMapVotes_OrderByMapNameDesc_ReturnsSorted()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var mapA = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_alpha" };
        var mapZ = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_zulu" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "TestPlayer", Guid = "test-guid" };
        context.Maps.AddRange(mapA, mapZ);
        context.Players.Add(player);
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = mapA.MapId, PlayerId = player.PlayerId, Like = true, Timestamp = DateTime.UtcNow });
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = mapZ.MapId, PlayerId = player.PlayerId, Like = false, Timestamp = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result = await api.GetMapVotes(null, null, 0, 20, MapVotesOrder.MapNameDesc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("mp_zulu", items[0].Map!.MapName);
        Assert.Equal("mp_alpha", items[1].Map!.MapName);
    }

    [Fact]
    public async Task GetMapVotes_PaginationMetadata_ReturnsCorrectCounts()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var map1 = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_map1" };
        var map2 = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty5, MapName = "mp_map2" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "TestPlayer", Guid = "test-guid" };
        context.Maps.AddRange(map1, map2);
        context.Players.Add(player);
        for (int i = 0; i < 3; i++)
            context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map1.MapId, PlayerId = player.PlayerId, Like = true, Timestamp = DateTime.UtcNow.AddMinutes(-i) });
        context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map2.MapId, PlayerId = player.PlayerId, Like = false, Timestamp = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        // Filter by gameType=CoD4 which has 3 votes, total is 4
        var result = await api.GetMapVotes(GameType.CallOfDuty4, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(4, result.Result!.Pagination!.TotalCount);
        Assert.Equal(3, result.Result.Pagination.FilteredCount);
        Assert.Equal(3, result.Result.Data!.Items!.Count());
    }

    [Fact]
    public async Task GetMapVotes_StableTiebreaker_SameTimestamp_DeterministicOrder()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var map = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_testmap" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "TestPlayer", Guid = "test-guid" };
        context.Maps.Add(map);
        context.Players.Add(player);
        var sameTimestamp = DateTime.UtcNow;
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        context.MapVotes.Add(new MapVote { MapVoteId = id1, MapId = map.MapId, PlayerId = player.PlayerId, Like = true, Timestamp = sameTimestamp });
        context.MapVotes.Add(new MapVote { MapVoteId = id2, MapId = map.MapId, PlayerId = player.PlayerId, Like = false, Timestamp = sameTimestamp });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapsApi)controller;
        var result1 = await api.GetMapVotes(null, null, 0, 20, MapVotesOrder.TimestampDesc);
        var result2 = await api.GetMapVotes(null, null, 0, 20, MapVotesOrder.TimestampDesc);

        var items1 = result1.Result!.Data!.Items!.Select(v => v.MapVoteId).ToList();
        var items2 = result2.Result!.Data!.Items!.Select(v => v.MapVoteId).ToList();
        Assert.Equal(items1, items2);
    }
}
