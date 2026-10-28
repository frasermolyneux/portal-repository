using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.Api.V1.Services.Caching;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class MapsControllerTests
{
    [Theory]
    [InlineData(GameType.CallOfDuty4)]
    [InlineData(GameType.CallOfDuty4x)]
    public async Task GetMaps_BothCod4FiltersReturnOnlyCanonicalMaps(GameType gameType)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        db.Maps.AddRange(
            new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_cod4" },
            new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4x, MapName = "mp_cod4x" },
            new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty5, MapName = "mp_cod5" });
        await db.SaveChangesAsync();
        var api = (IMapsApi)CreateController(db);

        var result = await api.GetMaps(gameType, null, null, null, 0, 20, null);

        Assert.Equal("mp_cod4", Assert.Single(result.Result!.Data!.Items!).MapName);
        Assert.Equal(1, result.Result.Pagination!.FilteredCount);

        var all = await api.GetMaps(null, null, null, null, 0, 20, null);
        Assert.Equal(2, all.Result!.Data!.Items!.Count());
        Assert.DoesNotContain(all.Result.Data.Items!, map => map.GameType == GameType.CallOfDuty4x);
    }

    [Theory]
    [InlineData(GameType.CallOfDuty4)]
    [InlineData(GameType.CallOfDuty4x)]
    public async Task GetMap_BothCod4TypesResolveCanonicalMapEvenBeforeLegacyRowsAreRemoved(GameType requestedType)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var canonical = new Map { MapId = Guid.Parse("00000000-0000-0000-0000-000000000002"), GameType = (int)GameType.CallOfDuty4, MapName = "mp_shared" };
        db.Maps.Add(canonical);
        db.Maps.Add(new Map { MapId = Guid.Parse("00000000-0000-0000-0000-000000000003"), GameType = (int)GameType.CallOfDuty4, MapName = "mp_shared" });
        db.Maps.Add(new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4x, MapName = "mp_shared" });
        db.Maps.Add(new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty5, MapName = "mp_shared" });
        await db.SaveChangesAsync();
        var api = (IMapsApi)CreateController(db);

        var result = await api.GetMap(requestedType, "mp_shared");
        Assert.Equal(canonical.MapId, result.Result!.Data!.MapId);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetMap(GameType.CallOfDuty2, "mp_shared")).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateCod4xMap_PersistsInCanonicalCatalogue(bool bulk)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var api = (IMapsApi)CreateController(db);
        var dto = new CreateMapDto(GameType.CallOfDuty4x, "mp_shared")
        {
            MapFiles = [new MapFileDto("mp_shared.ff", "https://redirect.test/mp_shared.ff")]
        };

        var created = bulk ? await api.CreateMaps([dto]) : await api.CreateMap(dto);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal((int)GameType.CallOfDuty4, Assert.Single(db.Maps).GameType);
        var result = await api.GetMap(GameType.CallOfDuty4x, "mp_shared");
        Assert.Equal(dto.MapFiles, result.Result!.Data!.MapFiles);
        var duplicate = bulk ? await api.CreateMaps([dto]) : await api.CreateMap(dto);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Single(db.Maps);
    }

    [Fact]
    public async Task Cod4xVoting_UsesSharedMapWhilePreservingProfilesServersAndRepeatVoteSemantics()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var map = new Map { MapId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, MapName = "mp_shared" };
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Username = "Classic", Guid = "classic" };
        var player4x = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4x, Username = "Native", Guid = "native" };
        var server = new GameServer { GameServerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Title = "Classic", Hostname = "classic.test", BanFileRootPath = "" };
        var server4x = new GameServer { GameServerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4x, Title = "Native", Hostname = "native.test", BanFileRootPath = "" };
        db.Maps.Add(map);
        db.Players.AddRange(player, player4x);
        db.GameServers.AddRange(server, server4x);
        await db.SaveChangesAsync();
        var api = (IMapsApi)CreateController(db);

        var lookup = await api.GetMap(GameType.CallOfDuty4x, "mp_shared");
        var sharedId = lookup.Result!.Data!.MapId;
        await api.UpsertMapVote(new UpsertMapVoteDto(sharedId, player.PlayerId, server.GameServerId, true));
        await api.UpsertMapVote(new UpsertMapVoteDto(sharedId, player4x.PlayerId, server4x.GameServerId, true));
        var originalVoteId = db.MapVotes.Single(vote => vote.PlayerId == player4x.PlayerId).MapVoteId;
        await api.UpsertMapVote(new UpsertMapVoteDto(sharedId, player4x.PlayerId, server4x.GameServerId, false));

        var votes = await api.GetMapVotes(GameType.CallOfDuty4x, null, 0, 20, null);
        Assert.Equal(2, votes.Result!.Pagination!.FilteredCount);
        Assert.All(votes.Result.Data!.Items!, vote => Assert.Equal(map.MapId, vote.MapId));
        var nativeVote = Assert.Single(votes.Result.Data.Items!, vote => vote.PlayerId == player4x.PlayerId);
        Assert.Equal(originalVoteId, nativeVote.MapVoteId);
        Assert.False(nativeVote.Like);
        Assert.Equal(server4x.GameServerId, nativeVote.GameServerId);
        Assert.Equal(GameType.CallOfDuty4x, nativeVote.Player!.GameType);
        Assert.Equal(GameType.CallOfDuty4x, nativeVote.GameServer!.GameType);
    }

    [Theory]
    [InlineData("MP_SHARED")]
    [InlineData("mp_shared ")]
    public async Task CreateMaps_RejectsConflictingCanonicalNamesWithinTheBatchWithoutPersisting(string duplicateName)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var api = (IMapsApi)CreateController(db);

        var result = await api.CreateMaps([
            new CreateMapDto(GameType.CallOfDuty4, "mp_shared"),
            new CreateMapDto(GameType.CallOfDuty4x, duplicateName)
        ]);

        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        Assert.Empty(db.Maps);
    }

    [Fact]
    public async Task SharedRatingsAndPopularityOrdering_IncludeLegacyRowsUntilCutover()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var canonical = new Map { MapId = Guid.NewGuid(), GameType = 2, MapName = "mp_shared", TotalLikes = 2, TotalDislikes = 1, TotalVotes = 3 };
        db.Maps.AddRange(canonical,
            new Map { MapId = Guid.NewGuid(), GameType = 24, MapName = "mp_shared", TotalLikes = 3, TotalDislikes = 1, TotalVotes = 4 },
            new Map { MapId = Guid.NewGuid(), GameType = 2, MapName = "mp_other", TotalLikes = 4, TotalVotes = 4 });
        await db.SaveChangesAsync();
        var api = (IMapsApi)CreateController(db);

        var listed = await api.GetMaps(GameType.CallOfDuty4x, null, null, null, 0, 1, MapsOrder.PopularityDesc);
        var top = Assert.Single(listed.Result!.Data!.Items!);
        Assert.Equal(canonical.MapId, top.MapId);
        Assert.Equal(5, top.TotalLikes);
        Assert.Equal(7, top.TotalVotes);
        Assert.Equal(2, listed.Result.Pagination!.TotalCount);
        var byName = (await api.GetMap(GameType.CallOfDuty4x, "mp_shared")).Result!.Data!;
        var byId = (await api.GetMap(canonical.MapId)).Result!.Data!;
        Assert.Equal((top.MapId, top.TotalLikes, top.TotalVotes), (byName.MapId, byName.TotalLikes, byName.TotalVotes));
        Assert.Equal((top.MapId, top.TotalLikes, top.TotalVotes), (byId.MapId, byId.TotalLikes, byId.TotalVotes));
        Assert.Equal(500.0 / 7, top.LikePercentage, 8);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RepeatVotesDuringCutover_ReuseHistoricalRowsAndPreserveExistingDuplicates(bool bulk, bool legacyInput)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var canonical = new Map { MapId = Guid.NewGuid(), GameType = 2, MapName = "mp_shared" };
        var legacy = new Map { MapId = Guid.NewGuid(), GameType = 24, MapName = "mp_shared" };
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow.AddDays(-1);
        var firstId = Guid.NewGuid();
        var duplicateId = Guid.NewGuid();
        db.Maps.AddRange(canonical, legacy);
        db.MapVotes.AddRange(
            new MapVote { MapVoteId = firstId, MapId = legacy.MapId, PlayerId = playerId, GameServerId = serverId, Like = true, Timestamp = timestamp },
            new MapVote { MapVoteId = duplicateId, MapId = legacy.MapId, PlayerId = playerId, GameServerId = serverId, Like = true, Timestamp = timestamp });
        await db.SaveChangesAsync();
        var api = (IMapsApi)CreateController(db);
        Assert.Equal(2, (await api.GetMapVotes(GameType.CallOfDuty4x, canonical.MapId, 0, 20, null)).Result!.Pagination!.FilteredCount);
        var vote = new UpsertMapVoteDto(legacyInput ? legacy.MapId : canonical.MapId, playerId, serverId, false);

        var result = bulk ? await api.UpsertMapVotes([vote, vote]) : await api.UpsertMapVote(vote);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var rows = db.MapVotes.ToList();
        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { firstId, duplicateId }.Order(), rows.Select(row => row.MapVoteId).Order());
        Assert.All(rows, row =>
        {
            Assert.Equal(canonical.MapId, row.MapId);
            Assert.Equal(playerId, row.PlayerId);
            Assert.Equal(serverId, row.GameServerId);
        });
        Assert.Equal(timestamp, Assert.Single(rows, row => row.Like).Timestamp);
        Assert.True(Assert.Single(rows, row => !row.Like).Timestamp > timestamp);
        Assert.Equal(HttpStatusCode.NotFound, (await api.UpsertMapVote(
            new UpsertMapVoteDto(Guid.NewGuid(), playerId, serverId, true))).StatusCode);
        Assert.Equal(2, db.MapVotes.Count());
    }

    private MapsController CreateController(PortalDbContext context)
    {
        var logger = new Mock<ILogger<MapsController>>();
        var mapReadService = new MapReadService(context);
        var cacheInvalidator = new NoOpRepositoryCacheInvalidator();
        return new MapsController(context, logger.Object, mapReadService, cacheInvalidator);
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
    public async Task CreateMap_PreservesRedirectFileVersion()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var api = (IMapsApi)CreateController(context);
        var file = new MapFileDto("mp_test.ff", "https://redirect.test/redirect/cod4/usermaps/mp_test/mp_test.ff")
        {
            RedirectVersion = "opaque-version-1"
        };

        var created = await api.CreateMap(new CreateMapDto(GameType.CallOfDuty4, "mp_test")
        {
            MapFiles = [file]
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var result = await api.GetMap(context.Maps.Single().MapId);

        Assert.NotNull(result.Result?.Data);
        Assert.Equal(file, Assert.Single(result.Result.Data.MapFiles));
    }

    [Fact]
    public async Task GetMap_ReadsLegacyFileMetadataWithoutRedirectVersion()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var mapId = Guid.NewGuid();
        context.Maps.Add(new Map
        {
            MapId = mapId,
            GameType = (int)GameType.CallOfDuty4,
            MapName = "mp_legacy",
            MapFiles = JsonConvert.SerializeObject(new[]
            {
                new { FileName = "mp_legacy.ff", Url = "https://redirect.test/legacy.ff" }
            })
        });
        await context.SaveChangesAsync();

        var result = await ((IMapsApi)CreateController(context)).GetMap(mapId);

        Assert.NotNull(result.Result?.Data);
        var file = Assert.Single(result.Result.Data.MapFiles);
        Assert.Equal("mp_legacy.ff", file.FileName);
        Assert.Null(file.RedirectVersion);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateMaps_EmptyFilesOnlyClearWhenReplacementIsExplicit(bool replaceMapFiles)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var api = (IMapsApi)CreateController(context);
        var file = new MapFileDto("mp_test.ff", "https://redirect.test/mp_test.ff");
        await api.CreateMap(new CreateMapDto(GameType.CallOfDuty4, "mp_test") { MapFiles = [file] });
        var mapId = context.Maps.Single().MapId;

        var updated = await api.UpdateMaps([
            new EditMapDto(mapId) { ReplaceMapFiles = replaceMapFiles }
        ]);
        var result = await api.GetMap(mapId);

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.NotNull(result.Result?.Data);
        if (replaceMapFiles)
        {
            Assert.Empty(result.Result.Data.MapFiles);
        }
        else
        {
            Assert.Equal(file, Assert.Single(result.Result.Data.MapFiles));
        }
    }

    [Fact]
    public async Task UpdateMap_PreservesReplacementVersionWithoutChangingFileCountOrUrl()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var api = (IMapsApi)CreateController(context);
        var file = new MapFileDto("mp_test.ff", "https://redirect.test/redirect/cod4/usermaps/mp_test/mp_test.ff")
        {
            RedirectVersion = "opaque-version-1"
        };
        await api.CreateMap(new CreateMapDto(GameType.CallOfDuty4, "mp_test") { MapFiles = [file] });
        var mapId = context.Maps.Single().MapId;
        var replacement = file with { RedirectVersion = "opaque-version-2" };

        var updated = await api.UpdateMap(new EditMapDto(mapId) { MapFiles = [replacement] });
        var result = await api.GetMap(mapId);

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.NotNull(result.Result?.Data);
        Assert.Equal(replacement, Assert.Single(result.Result.Data.MapFiles));
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
        {
            context.MapVotes.Add(new MapVote { MapVoteId = Guid.NewGuid(), MapId = map1.MapId, PlayerId = player.PlayerId, Like = true, Timestamp = DateTime.UtcNow.AddMinutes(-i) });
        }

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
