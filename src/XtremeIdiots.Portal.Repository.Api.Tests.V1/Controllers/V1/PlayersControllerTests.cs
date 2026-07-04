using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using MX.Observability.ApplicationInsights.Auditing;
using Newtonsoft.Json;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class PlayersControllerTests
{
    private static (PlayersController Controller, Mock<IAuditLogger> AuditLoggerMock) CreateControllerWithAuditMock(PortalDbContext context)
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var auditLogger = new Mock<IAuditLogger>();
        return (new PlayersController(context, memoryCache, auditLogger.Object), auditLogger);
    }

    private static PlayersController CreateController(PortalDbContext context) => CreateControllerWithAuditMock(context).Controller;

    [Fact]
    public async Task GetPlayer_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetPlayer_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(Guid.NewGuid(), PlayerEntityOptions.None);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetPlayers_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Players.Add(new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.Players.Add(new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player2",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayers(null, null, null, 0, 20, null, PlayerEntityOptions.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetPlayers_EmptyDb_ReturnsEmptyCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayers(null, null, null, 0, 20, null, PlayerEntityOptions.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public void PlayersFilter_ContainsTagMember()
    {
        Assert.Contains(PlayersFilter.Tag, Enum.GetValues<PlayersFilter>());
    }

    [Fact]
    public async Task GetPlayers_WithTagFilter_ReturnsTaggedPlayersOnly()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var tagId = Guid.NewGuid();

        var taggedPlayerId = Guid.NewGuid();
        var untaggedPlayerId = Guid.NewGuid();

        context.Tags.Add(new Tag { TagId = tagId, Name = "tag-a", UserDefined = true });

        context.Players.Add(new Player
        {
            PlayerId = taggedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Tagged",
            Guid = "guid-tagged",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });

        context.Players.Add(new Player
        {
            PlayerId = untaggedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Untagged",
            Guid = "guid-untagged",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });

        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = Guid.NewGuid(),
            PlayerId = taggedPlayerId,
            TagId = tagId,
            Assigned = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var result = await api.GetPlayers(null, PlayersFilter.Tag, tagId.ToString(), 0, 20, null, PlayerEntityOptions.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var players = result.Result!.Data!.Items!.ToList();
        Assert.Single(players);
        Assert.Equal(taggedPlayerId, players[0].PlayerId);
    }

    [Fact]
    public async Task GetPlayers_WithTagFilterAndGameType_ReturnsIntersection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var tagId = Guid.NewGuid();

        var cod4PlayerId = Guid.NewGuid();
        var cod5PlayerId = Guid.NewGuid();

        context.Tags.Add(new Tag { TagId = tagId, Name = "tag-b", UserDefined = true });

        context.Players.Add(new Player
        {
            PlayerId = cod4PlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Cod4Tagged",
            Guid = "guid-cod4",
            FirstSeen = DateTime.UtcNow.AddDays(-3),
            LastSeen = DateTime.UtcNow
        });

        context.Players.Add(new Player
        {
            PlayerId = cod5PlayerId,
            GameType = (int)GameType.CallOfDuty5,
            Username = "Cod5Tagged",
            Guid = "guid-cod5",
            FirstSeen = DateTime.UtcNow.AddDays(-3),
            LastSeen = DateTime.UtcNow
        });

        context.PlayerTags.AddRange(
            new PlayerTag
            {
                PlayerTagId = Guid.NewGuid(),
                PlayerId = cod4PlayerId,
                TagId = tagId,
                Assigned = DateTime.UtcNow
            },
            new PlayerTag
            {
                PlayerTagId = Guid.NewGuid(),
                PlayerId = cod5PlayerId,
                TagId = tagId,
                Assigned = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var result = await api.GetPlayers(GameType.CallOfDuty4, PlayersFilter.Tag, tagId.ToString(), 0, 20, null, PlayerEntityOptions.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var players = result.Result!.Data!.Items!.ToList();
        Assert.Single(players);
        Assert.Equal(cod4PlayerId, players[0].PlayerId);
        Assert.Equal(GameType.CallOfDuty4, players[0].GameType);
    }

    [Fact]
    public async Task GetPlayers_WithMalformedTagFilter_ReturnsEmptyCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Players.Add(new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player1",
            Guid = "guid-1",
            FirstSeen = DateTime.UtcNow.AddDays(-1),
            LastSeen = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var result = await api.GetPlayers(null, PlayersFilter.Tag, "not-a-guid", 0, 20, null, PlayerEntityOptions.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(result.Result!.Data!.Items!);
    }

    [Fact]
    public async Task GetPlayers_WithTagsOption_PopulatesTagsWithTagMetadata()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        context.Tags.Add(new Tag { TagId = tagId, Name = "vip", Description = "VIP tag", UserDefined = true });

        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TaggedPlayer",
            Guid = "guid-tagged-player",
            FirstSeen = DateTime.UtcNow.AddDays(-2),
            LastSeen = DateTime.UtcNow
        });

        context.PlayerTags.Add(new PlayerTag
        {
            PlayerTagId = Guid.NewGuid(),
            PlayerId = playerId,
            TagId = tagId,
            Assigned = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var result = await api.GetPlayers(null, null, null, 0, 20, null, PlayerEntityOptions.Tags);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        var player = Assert.Single(result.Result!.Data!.Items!);
        var playerTag = Assert.Single(player.Tags);
        Assert.Equal(tagId, playerTag.TagId);
        Assert.NotNull(playerTag.Tag);
        Assert.Equal("vip", playerTag.Tag!.Name);
    }

    [Fact]
    public async Task GetPlayers_WithTagFilter_AppliesOrderingAndPagination()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var tagId = Guid.NewGuid();

        context.Tags.Add(new Tag { TagId = tagId, Name = "ordered-tag", UserDefined = true });

        var playerA = new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Username = "Alpha",
            Guid = "guid-alpha",
            FirstSeen = DateTime.UtcNow.AddDays(-3),
            LastSeen = DateTime.UtcNow
        };

        var playerB = new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Username = "Bravo",
            Guid = "guid-bravo",
            FirstSeen = DateTime.UtcNow.AddDays(-3),
            LastSeen = DateTime.UtcNow
        };

        var playerC = new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Username = "Charlie",
            Guid = "guid-charlie",
            FirstSeen = DateTime.UtcNow.AddDays(-3),
            LastSeen = DateTime.UtcNow
        };

        context.Players.AddRange(playerC, playerA, playerB);

        context.PlayerTags.AddRange(
            new PlayerTag { PlayerTagId = Guid.NewGuid(), PlayerId = playerA.PlayerId, TagId = tagId, Assigned = DateTime.UtcNow },
            new PlayerTag { PlayerTagId = Guid.NewGuid(), PlayerId = playerB.PlayerId, TagId = tagId, Assigned = DateTime.UtcNow },
            new PlayerTag { PlayerTagId = Guid.NewGuid(), PlayerId = playerC.PlayerId, TagId = tagId, Assigned = DateTime.UtcNow });

        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var result = await api.GetPlayers(null, PlayersFilter.Tag, tagId.ToString(), 1, 1, PlayersOrder.UsernameAsc, PlayerEntityOptions.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var page = result.Result!.Data!.Items!.ToList();
        Assert.Single(page);
        Assert.Equal("Bravo", page[0].Username);
    }

    [Fact]
    public async Task HeadPlayerByGameType_WhenExists_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var guid = "testguid123";
        context.Players.Add(new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Guid = guid,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.HeadPlayerByGameType(GameType.CallOfDuty4, guid);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task HeadPlayerByGameType_WhenNotExists_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.HeadPlayerByGameType(GameType.CallOfDuty4, "nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task CreatePlayer_CreatesEntityInDb()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IPlayersApi)controller;

        var createDto = new CreatePlayerDto("TestPlayer", "testguid", GameType.CallOfDuty4) { IpAddress = "127.0.0.1" };

        var result = await api.CreatePlayer(createDto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.Players);
    }

    [Fact]
    public async Task CreatePlayer_WithMeaningfulSteamId_PersistsNormalizedSteamId()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var api = (IPlayersApi)CreateController(context);

        var createDto = new CreatePlayerDto("TestPlayer", "testguid-steam-1", GameType.CallOfDuty4)
        {
            SteamId = " 76561198000000001 "
        };

        var result = await api.CreatePlayer(createDto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var player = Assert.Single(context.Players);
        Assert.Equal("76561198000000001", player.SteamId);
    }

    [Fact]
    public async Task CreatePlayer_WithZeroSteamId_DoesNotPersistSteamId()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var api = (IPlayersApi)CreateController(context);

        var createDto = new CreatePlayerDto("TestPlayer", "testguid-steam-2", GameType.CallOfDuty4)
        {
            SteamId = "0"
        };

        var result = await api.CreatePlayer(createDto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var player = Assert.Single(context.Players);
        Assert.Null(player.SteamId);
    }

    [Fact]
    public async Task CreatePlayers_WithInvalidSteamId_DoesNotPersistSteamId()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var api = (IPlayersApi)CreateController(context);

        var createDtos = new List<CreatePlayerDto>
        {
            new("TestPlayer1", "testguid-steam-batch-1", GameType.CallOfDuty4)
            {
                SteamId = "   "
            },
            new("TestPlayer2", "testguid-steam-batch-2", GameType.CallOfDuty4)
            {
                SteamId = "0"
            }
        };

        var result = await api.CreatePlayers(createDtos);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.All(context.Players, player => Assert.Null(player.SteamId));
    }

    [Fact]
    public async Task GetPlayer_WithAliasesOption_LoadsAliases()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.PlayerAliases.Add(new PlayerAlias
        {
            PlayerAliasId = Guid.NewGuid(),
            PlayerId = playerId,
            Name = "Alias1",
            LastUsed = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.Aliases);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    #region UpdatePlayerIpAddress Tests

    [Fact]
    public async Task UpdatePlayerIpAddress_WithValidIp_UpdatesPlayerAndHistory()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.UpdatePlayerIpAddress(new UpdatePlayerIpAddressDto(playerId, "192.168.1.100"));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var player = context.Players.First(p => p.PlayerId == playerId);
        Assert.Equal("192.168.1.100", player.IpAddress);
        Assert.Single(context.PlayerIpAddresses.Where(ip => ip.PlayerId == playerId));
    }

    [Fact]
    public async Task UpdatePlayerIpAddress_WithInvalidIp_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.UpdatePlayerIpAddress(new UpdatePlayerIpAddressDto(playerId, "not-an-ip"));

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UpdatePlayerIpAddress_WithEmptyIp_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.UpdatePlayerIpAddress(new UpdatePlayerIpAddressDto(Guid.NewGuid(), ""));

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UpdatePlayerIpAddress_NonExistentPlayer_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.UpdatePlayerIpAddress(new UpdatePlayerIpAddressDto(Guid.NewGuid(), "192.168.1.1"));

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task UpdatePlayerIpAddress_DuplicateIp_IncrementsConfidence()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            IpAddress = "192.168.1.1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.PlayerIpAddresses.Add(new PlayerIpAddress
        {
            PlayerIpAddressId = Guid.NewGuid(),
            PlayerId = playerId,
            Address = "192.168.1.1",
            Added = DateTime.UtcNow.AddDays(-10),
            LastUsed = DateTime.UtcNow.AddDays(-1),
            ConfidenceScore = 5
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.UpdatePlayerIpAddress(new UpdatePlayerIpAddressDto(playerId, "192.168.1.1"));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var ipEntry = context.PlayerIpAddresses.First(ip => ip.PlayerId == playerId);
        Assert.Equal(6, ipEntry.ConfidenceScore);
    }

    [Fact]
    public async Task UpdatePlayerIpAddress_DoesNotModifyLastSeen()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var originalLastSeen = DateTime.UtcNow.AddDays(-1);
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = originalLastSeen
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        await api.UpdatePlayerIpAddress(new UpdatePlayerIpAddressDto(playerId, "10.0.0.1"));

        var player = context.Players.First(p => p.PlayerId == playerId);
        Assert.Equal(originalLastSeen, player.LastSeen);
    }

    #endregion

    #region UpdatePlayerUsername Tests

    [Fact]
    public async Task UpdatePlayerUsername_WithNewName_CreatesAlias()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "OldName",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.UpdatePlayerUsername(new UpdatePlayerUsernameDto(playerId, "NewName"));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var player = context.Players.First(p => p.PlayerId == playerId);
        Assert.Equal("NewName", player.Username);
        Assert.Single(context.PlayerAliases.Where(a => a.PlayerId == playerId && a.Name == "NewName"));
    }

    [Fact]
    public async Task UpdatePlayerUsername_WithExistingName_IncrementsConfidence()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "SameName",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.PlayerAliases.Add(new PlayerAlias
        {
            PlayerAliasId = Guid.NewGuid(),
            PlayerId = playerId,
            Name = "SameName",
            Added = DateTime.UtcNow.AddDays(-10),
            LastUsed = DateTime.UtcNow.AddDays(-1),
            ConfidenceScore = 3
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.UpdatePlayerUsername(new UpdatePlayerUsernameDto(playerId, "SameName"));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var alias = context.PlayerAliases.First(a => a.PlayerId == playerId);
        Assert.Equal(4, alias.ConfidenceScore);
    }

    [Fact]
    public async Task UpdatePlayerUsername_WithEmptyName_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.UpdatePlayerUsername(new UpdatePlayerUsernameDto(Guid.NewGuid(), ""));

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UpdatePlayerUsername_NonExistentPlayer_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.UpdatePlayerUsername(new UpdatePlayerUsernameDto(Guid.NewGuid(), "Name"));

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task UpdatePlayerUsername_DoesNotModifyIpOrLastSeen()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var originalLastSeen = DateTime.UtcNow.AddDays(-1);
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "OldName",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = originalLastSeen
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        await api.UpdatePlayerUsername(new UpdatePlayerUsernameDto(playerId, "NewName"));

        var player = context.Players.First(p => p.PlayerId == playerId);
        Assert.Equal("10.0.0.1", player.IpAddress);
        Assert.Equal(originalLastSeen, player.LastSeen);
    }

    #endregion

    #region RecordPlayerSession Tests

    [Fact]
    public async Task RecordPlayerSession_UpdatesLastSeenAndAlias()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var oldLastSeen = DateTime.UtcNow.AddDays(-5);
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "OldName",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = oldLastSeen
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.RecordPlayerSession(new RecordPlayerSessionDto(playerId, "NewName"));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var player = context.Players.First(p => p.PlayerId == playerId);
        Assert.Equal("NewName", player.Username);
        Assert.True(player.LastSeen > oldLastSeen);
        Assert.Single(context.PlayerAliases.Where(a => a.PlayerId == playerId));
    }

    [Fact]
    public async Task RecordPlayerSession_WithIp_DoesNotUpdateIp()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.RecordPlayerSession(new RecordPlayerSessionDto(playerId, "Player"));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var player = context.Players.First(p => p.PlayerId == playerId);
        Assert.Equal("10.0.0.1", player.IpAddress);
        Assert.Empty(context.PlayerIpAddresses.Where(ip => ip.PlayerId == playerId));
    }

    [Fact]
    public async Task RecordPlayerSession_WithEmptyUsername_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.RecordPlayerSession(new RecordPlayerSessionDto(Guid.NewGuid(), ""));

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task RecordPlayerSession_NonExistentPlayer_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.RecordPlayerSession(new RecordPlayerSessionDto(Guid.NewGuid(), "Name"));

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task RecordPlayerSession_WithMeaningfulSteamId_UpdatesSteamId()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player",
            Guid = "guid-1",
            SteamId = null,
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var result = await api.RecordPlayerSession(new RecordPlayerSessionDto(playerId, "Player", "76561198000000001"));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var player = context.Players.First(p => p.PlayerId == playerId);
        Assert.Equal("76561198000000001", player.SteamId);
    }

    [Fact]
    public async Task RecordPlayerSession_WithZeroSteamId_DoesNotOverwriteExistingSteamId()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player",
            Guid = "guid-1",
            SteamId = "76561198000000042",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var api = (IPlayersApi)CreateController(context);
        var result = await api.RecordPlayerSession(new RecordPlayerSessionDto(playerId, "Player", "0"));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var player = context.Players.First(p => p.PlayerId == playerId);
        Assert.Equal("76561198000000042", player.SteamId);
    }

    [Fact]
    public async Task RecordPlayerSession_WhenSteamIdChanges_EmitsAudit()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Player",
            Guid = "guid-1",
            SteamId = "76561198000000042",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var (controller, auditLoggerMock) = CreateControllerWithAuditMock(context);
        var api = (IPlayersApi)controller;

        var result = await api.RecordPlayerSession(new RecordPlayerSessionDto(playerId, "Player", "76561198000000001"));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var auditInvocation = Assert.Single(auditLoggerMock.Invocations, invocation => invocation.Method.Name == "LogAudit");
        var auditPayload = Assert.Single(auditInvocation.Arguments);
        var auditPayloadJson = JsonConvert.SerializeObject(auditPayload);

        Assert.Contains("PlayerSteamIdChanged", auditPayloadJson, StringComparison.Ordinal);
        Assert.Contains("76561198000000042", auditPayloadJson, StringComparison.Ordinal);
        Assert.Contains("76561198000000001", auditPayloadJson, StringComparison.Ordinal);
    }

    #endregion

    #region Counts Tests

    [Fact]
    public async Task GetPlayer_WithCountsOption_ReturnsCounts()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.PlayerAliases.Add(new PlayerAlias { PlayerAliasId = Guid.NewGuid(), PlayerId = playerId, Name = "Alias1", LastUsed = DateTime.UtcNow });
        context.PlayerAliases.Add(new PlayerAlias { PlayerAliasId = Guid.NewGuid(), PlayerId = playerId, Name = "Alias2", LastUsed = DateTime.UtcNow });
        context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = playerId, Address = "10.0.0.1", LastUsed = DateTime.UtcNow });
        context.AdminActions.Add(new AdminAction { AdminActionId = Guid.NewGuid(), PlayerId = playerId, Type = (int)AdminActionType.Warning, Text = "Test", Created = DateTime.UtcNow });
        context.ProtectedNames.Add(new ProtectedName { ProtectedNameId = Guid.NewGuid(), PlayerId = playerId, Name = "PN1", CreatedOn = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.Counts);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var player = result.Result!.Data!;
        Assert.Equal(2, player.AliasCount);
        Assert.Equal(1, player.IpAddressCount);
        Assert.Equal(1, player.AdminActionCount);
        Assert.Equal(1, player.ProtectedNameCount);
    }

    [Fact]
    public async Task GetPlayer_WithCountsAndCollections_UsesLoadedCounts()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.PlayerAliases.Add(new PlayerAlias { PlayerAliasId = Guid.NewGuid(), PlayerId = playerId, Name = "Alias1", LastUsed = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.Aliases | PlayerEntityOptions.Counts);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var player = result.Result!.Data!;
        Assert.Equal(1, player.AliasCount);
        Assert.Single(player.PlayerAliases);
    }

    [Fact]
    public async Task GetPlayer_WithoutCountsOption_CountsAreZero()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.PlayerAliases.Add(new PlayerAlias { PlayerAliasId = Guid.NewGuid(), PlayerId = playerId, Name = "Alias1", LastUsed = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var player = result.Result!.Data!;
        Assert.Equal(0, player.AliasCount);
    }

    #endregion

    #region ProtectedName Listing Tests

    [Fact]
    public async Task GetProtectedNames_WhenGameTypeFilterProvided_ReturnsOnlyMatchingOwners()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var cod4OwnerId = Guid.NewGuid();
        var cod4xOwnerId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = cod4OwnerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Cod4Owner",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });

        context.Players.Add(new Player
        {
            PlayerId = cod4xOwnerId,
            GameType = (int)GameType.CallOfDuty4x,
            Username = "Cod4xOwner",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });

        context.ProtectedNames.Add(new ProtectedName
        {
            ProtectedNameId = Guid.NewGuid(),
            PlayerId = cod4OwnerId,
            Name = "Totty",
            CreatedOn = DateTime.UtcNow.AddDays(-2)
        });

        context.ProtectedNames.Add(new ProtectedName
        {
            ProtectedNameId = Guid.NewGuid(),
            PlayerId = cod4xOwnerId,
            Name = "TottyX",
            CreatedOn = DateTime.UtcNow.AddDays(-1)
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;

        var result = await api.GetProtectedNames(0, 20, GameType.CallOfDuty4);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items?.ToList() ?? [];
        Assert.Single(items);
        Assert.Equal("Totty", items[0].Name);
        Assert.Equal(GameType.CallOfDuty4, items[0].OwnerGameType);
    }

    [Fact]
    public async Task GetProtectedNames_WithoutFilter_ProjectsOwnerGameType()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var cod4OwnerId = Guid.NewGuid();
        var cod4xOwnerId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = cod4OwnerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Cod4Owner",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });

        context.Players.Add(new Player
        {
            PlayerId = cod4xOwnerId,
            GameType = (int)GameType.CallOfDuty4x,
            Username = "Cod4xOwner",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });

        context.ProtectedNames.Add(new ProtectedName
        {
            ProtectedNameId = Guid.NewGuid(),
            PlayerId = cod4OwnerId,
            Name = "Totty",
            CreatedOn = DateTime.UtcNow.AddDays(-2)
        });

        context.ProtectedNames.Add(new ProtectedName
        {
            ProtectedNameId = Guid.NewGuid(),
            PlayerId = cod4xOwnerId,
            Name = "TottyX",
            CreatedOn = DateTime.UtcNow.AddDays(-1)
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;

        var result = await api.GetProtectedNames(0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items?.ToList() ?? [];
        Assert.Equal(2, items.Count);
        Assert.Contains(items, item => item.Name == "Totty" && item.OwnerGameType == GameType.CallOfDuty4);
        Assert.Contains(items, item => item.Name == "TottyX" && item.OwnerGameType == GameType.CallOfDuty4x);
    }

    #endregion

    #region ProtectedName Scope Tests

    [Fact]
    public async Task CreateProtectedName_WhenSameNameExistsInSameGame_ReturnsConflict()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var existingOwnerId = Guid.NewGuid();
        var targetPlayerId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = existingOwnerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "ExistingOwner",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });

        context.Players.Add(new Player
        {
            PlayerId = targetPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TargetPlayer",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });

        context.ProtectedNames.Add(new ProtectedName
        {
            ProtectedNameId = Guid.NewGuid(),
            PlayerId = existingOwnerId,
            Name = "Totty",
            CreatedOn = DateTime.UtcNow.AddDays(-2)
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;

        var result = await api.CreateProtectedName(new CreateProtectedNameDto(targetPlayerId, "Totty", "admin"));

        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
    }

    [Fact]
    public async Task CreateProtectedName_WhenSameNameExistsInDifferentGame_ReturnsCreated()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var existingOwnerId = Guid.NewGuid();
        var targetPlayerId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = existingOwnerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "ExistingOwner",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });

        context.Players.Add(new Player
        {
            PlayerId = targetPlayerId,
            GameType = (int)GameType.CallOfDuty4x,
            Username = "TargetPlayer",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });

        context.ProtectedNames.Add(new ProtectedName
        {
            ProtectedNameId = Guid.NewGuid(),
            PlayerId = existingOwnerId,
            Name = "Totty",
            CreatedOn = DateTime.UtcNow.AddDays(-2)
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;

        var result = await api.CreateProtectedName(new CreateProtectedNameDto(targetPlayerId, "Totty", "admin"));

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Equal(2, context.ProtectedNames.Count(pn => pn.Name == "Totty"));
    }

    [Fact]
    public async Task CreateProtectedName_WhenTargetPlayerDoesNotExist_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var existingOwnerId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = existingOwnerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "ExistingOwner",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });

        context.ProtectedNames.Add(new ProtectedName
        {
            ProtectedNameId = Guid.NewGuid(),
            PlayerId = existingOwnerId,
            Name = "Totty",
            CreatedOn = DateTime.UtcNow.AddDays(-2)
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;

        var result = await api.CreateProtectedName(new CreateProtectedNameDto(Guid.NewGuid(), "Totty", "admin"));

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    #endregion

    #region ProtectedName Usage Report Tests

    [Fact]
    public async Task GetProtectedNameUsageReport_WhenAliasContainsProtectedName_IncludesUsageInstance()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var ownerId = Guid.NewGuid();
        var usagePlayerId = Guid.NewGuid();
        var protectedNameId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = ownerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Owner",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });

        context.Players.Add(new Player
        {
            PlayerId = usagePlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "AliasUser",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });

        context.ProtectedNames.Add(new ProtectedName
        {
            ProtectedNameId = protectedNameId,
            PlayerId = ownerId,
            Name = "Totty",
            CreatedOn = DateTime.UtcNow.AddDays(-2)
        });

        context.PlayerAliases.Add(new PlayerAlias
        {
            PlayerAliasId = Guid.NewGuid(),
            PlayerId = usagePlayerId,
            Name = "ClanTottyX",
            LastUsed = DateTime.UtcNow.AddHours(-1)
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;

        var result = await api.GetProtectedNameUsageReport(protectedNameId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var usage = Assert.Single(result.Result!.Data!.UsageInstances);
        Assert.Equal(usagePlayerId, usage.PlayerId);
        Assert.Equal("AliasUser", usage.Username);
        Assert.False(usage.IsOwner);
        Assert.Equal(1, usage.UsageCount);
    }

    [Fact]
    public async Task GetProtectedNameUsageReport_WhenProtectedNameContainsAlias_IncludesUsageInstance()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var ownerId = Guid.NewGuid();
        var usagePlayerId = Guid.NewGuid();
        var protectedNameId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = ownerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Owner",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });

        context.Players.Add(new Player
        {
            PlayerId = usagePlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "ShortAliasUser",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });

        context.ProtectedNames.Add(new ProtectedName
        {
            ProtectedNameId = protectedNameId,
            PlayerId = ownerId,
            Name = "TottyXtreme",
            CreatedOn = DateTime.UtcNow.AddDays(-2)
        });

        context.PlayerAliases.Add(new PlayerAlias
        {
            PlayerAliasId = Guid.NewGuid(),
            PlayerId = usagePlayerId,
            Name = "Totty",
            LastUsed = DateTime.UtcNow.AddHours(-1)
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;

        var result = await api.GetProtectedNameUsageReport(protectedNameId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var usage = Assert.Single(result.Result!.Data!.UsageInstances);
        Assert.Equal(usagePlayerId, usage.PlayerId);
        Assert.Equal("ShortAliasUser", usage.Username);
        Assert.False(usage.IsOwner);
        Assert.Equal(1, usage.UsageCount);
    }

    [Fact]
    public async Task GetProtectedNameUsageReport_WhenPlayerHasMultipleMatches_AggregatesUsageCount()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var ownerId = Guid.NewGuid();
        var usagePlayerId = Guid.NewGuid();
        var protectedNameId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = ownerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Owner",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });

        context.Players.Add(new Player
        {
            PlayerId = usagePlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "MultiAliasUser",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });

        context.ProtectedNames.Add(new ProtectedName
        {
            ProtectedNameId = protectedNameId,
            PlayerId = ownerId,
            Name = "Totty",
            CreatedOn = DateTime.UtcNow.AddDays(-2)
        });

        context.PlayerAliases.Add(new PlayerAlias
        {
            PlayerAliasId = Guid.NewGuid(),
            PlayerId = usagePlayerId,
            Name = "Totty",
            LastUsed = DateTime.UtcNow.AddHours(-2)
        });

        context.PlayerAliases.Add(new PlayerAlias
        {
            PlayerAliasId = Guid.NewGuid(),
            PlayerId = usagePlayerId,
            Name = "xTottyx",
            LastUsed = DateTime.UtcNow.AddHours(-1)
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;

        var result = await api.GetProtectedNameUsageReport(protectedNameId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var usage = Assert.Single(result.Result!.Data!.UsageInstances);
        Assert.Equal(usagePlayerId, usage.PlayerId);
        Assert.Equal("MultiAliasUser", usage.Username);
        Assert.False(usage.IsOwner);
        Assert.Equal(2, usage.UsageCount);
    }

    [Fact]
    public async Task GetProtectedNameUsageReport_WhenAliasIsEmpty_ExcludesUsageInstance()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var ownerId = Guid.NewGuid();
        var usagePlayerId = Guid.NewGuid();
        var protectedNameId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = ownerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Owner",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });

        context.Players.Add(new Player
        {
            PlayerId = usagePlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "EmptyAliasUser",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });

        context.ProtectedNames.Add(new ProtectedName
        {
            ProtectedNameId = protectedNameId,
            PlayerId = ownerId,
            Name = "Totty",
            CreatedOn = DateTime.UtcNow.AddDays(-2)
        });

        context.PlayerAliases.Add(new PlayerAlias
        {
            PlayerAliasId = Guid.NewGuid(),
            PlayerId = usagePlayerId,
            Name = string.Empty,
            LastUsed = DateTime.UtcNow.AddHours(-1)
        });

        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;

        var result = await api.GetProtectedNameUsageReport(protectedNameId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(result.Result!.Data!.UsageInstances);
    }

    #endregion

    #region RelatedPlayers Enrichment Tests

    [Fact]
    public async Task GetPlayer_WithRelatedPlayers_IncludesLastSeenAndLinkingIp()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var relatedPlayerId = Guid.NewGuid();
        var relatedLastSeen = DateTime.UtcNow.AddDays(-2);
        var viewedIpLastUsed = DateTime.UtcNow.AddDays(-1);
        var relatedIpLastUsed = DateTime.UtcNow.AddHours(-6);

        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "MainPlayer",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.Players.Add(new Player
        {
            PlayerId = relatedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "RelatedPlayer",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = relatedLastSeen
        });
        // Viewed player IP history
        context.PlayerIpAddresses.Add(new PlayerIpAddress
        {
            PlayerIpAddressId = Guid.NewGuid(),
            PlayerId = playerId,
            Address = "10.0.0.1",
            LastUsed = viewedIpLastUsed
        });
        // Related player IP history
        context.PlayerIpAddresses.Add(new PlayerIpAddress
        {
            PlayerIpAddressId = Guid.NewGuid(),
            PlayerId = relatedPlayerId,
            Address = "10.0.0.1",
            LastUsed = relatedIpLastUsed
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.RelatedPlayers);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var related = Assert.Single(result.Result!.Data!.RelatedPlayers);
        Assert.Equal(relatedPlayerId, related.PlayerId);
        Assert.Equal(relatedLastSeen, related.LastSeen);
        Assert.Equal("10.0.0.1", related.LinkingIpAddress);
        Assert.Equal(viewedIpLastUsed, related.LinkingIpLastUsedByPlayer);
        Assert.Equal(relatedIpLastUsed, related.LinkingIpLastUsedByRelated);
        Assert.True(related.IsCurrentIp);
        Assert.Equal(1, related.SharedIpCount);
    }

    [Fact]
    public async Task GetPlayer_WithRelatedPlayers_FindsHistoricalIpMatches()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var relatedPlayerId = Guid.NewGuid();

        // Viewed player's current IP is different from the historical shared one
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "MainPlayer",
            IpAddress = "10.0.0.2",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.Players.Add(new Player
        {
            PlayerId = relatedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "HistoricalRelated",
            IpAddress = "10.0.0.3",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });
        // Both players used 10.0.0.1 historically
        context.PlayerIpAddresses.Add(new PlayerIpAddress
        {
            PlayerIpAddressId = Guid.NewGuid(),
            PlayerId = playerId,
            Address = "10.0.0.1",
            LastUsed = DateTime.UtcNow.AddDays(-5)
        });
        context.PlayerIpAddresses.Add(new PlayerIpAddress
        {
            PlayerIpAddressId = Guid.NewGuid(),
            PlayerId = relatedPlayerId,
            Address = "10.0.0.1",
            LastUsed = DateTime.UtcNow.AddDays(-3)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.RelatedPlayers);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var related = Assert.Single(result.Result!.Data!.RelatedPlayers);
        Assert.Equal(relatedPlayerId, related.PlayerId);
        Assert.Equal("10.0.0.1", related.LinkingIpAddress);
        Assert.False(related.IsCurrentIp);
    }

    [Fact]
    public async Task GetPlayer_WithRelatedPlayers_MultipleSharedIps_ReportsCount()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var relatedPlayerId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "MainPlayer",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.Players.Add(new Player
        {
            PlayerId = relatedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "MultiIpRelated",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });
        // Both share two IPs
        foreach (var ip in new[] { "10.0.0.1", "10.0.0.2" })
        {
            context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = playerId, Address = ip, LastUsed = DateTime.UtcNow });
            context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = relatedPlayerId, Address = ip, LastUsed = DateTime.UtcNow });
        }
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.RelatedPlayers);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var related = Assert.Single(result.Result!.Data!.RelatedPlayers);
        Assert.Equal(2, related.SharedIpCount);
    }

    [Fact]
    public async Task GetPlayer_WithRelatedPlayers_DetectsActiveBan()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var relatedPlayerId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "MainPlayer",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.Players.Add(new Player
        {
            PlayerId = relatedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "BannedRelated",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });
        context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = playerId, Address = "10.0.0.1", LastUsed = DateTime.UtcNow });
        context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = relatedPlayerId, Address = "10.0.0.1", LastUsed = DateTime.UtcNow });
        context.AdminActions.Add(new AdminAction { AdminActionId = Guid.NewGuid(), PlayerId = relatedPlayerId, Type = (int)AdminActionType.Ban, Text = "Permanent ban", Created = DateTime.UtcNow.AddDays(-1), Expires = null });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.RelatedPlayers);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var related = Assert.Single(result.Result!.Data!.RelatedPlayers);
        Assert.True(related.HasActiveBan);
        Assert.Equal(1, related.AdminActionCount);
    }

    [Fact]
    public async Task GetPlayer_WithRelatedPlayers_ExpiredBanIsNotActive()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var relatedPlayerId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "MainPlayer",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.Players.Add(new Player
        {
            PlayerId = relatedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "ExpiredBanPlayer",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-5),
            LastSeen = DateTime.UtcNow
        });
        context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = playerId, Address = "10.0.0.1", LastUsed = DateTime.UtcNow });
        context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = relatedPlayerId, Address = "10.0.0.1", LastUsed = DateTime.UtcNow });
        context.AdminActions.Add(new AdminAction { AdminActionId = Guid.NewGuid(), PlayerId = relatedPlayerId, Type = (int)AdminActionType.TempBan, Text = "Expired", Created = DateTime.UtcNow.AddDays(-10), Expires = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.RelatedPlayers);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var related = Assert.Single(result.Result!.Data!.RelatedPlayers);
        Assert.False(related.HasActiveBan);
        Assert.Equal(1, related.AdminActionCount);
    }

    [Fact]
    public async Task GetPlayer_WithCountsAndRelatedPlayers_CountsRelatedPlayers()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();

        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "MainPlayer",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        // Viewed player's IP history
        context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = playerId, Address = "10.0.0.1", LastUsed = DateTime.UtcNow });

        for (var i = 0; i < 3; i++)
        {
            var rpId = Guid.NewGuid();
            context.Players.Add(new Player
            {
                PlayerId = rpId,
                GameType = (int)GameType.CallOfDuty4,
                Username = $"Related{i}",
                IpAddress = "10.0.0.1",
                FirstSeen = DateTime.UtcNow.AddDays(-5),
                LastSeen = DateTime.UtcNow
            });
            context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = rpId, Address = "10.0.0.1", LastUsed = DateTime.UtcNow });
        }
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.RelatedPlayers | PlayerEntityOptions.Counts);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var player = result.Result!.Data!;
        Assert.Equal(3, player.RelatedPlayerCount);
        Assert.Equal(3, player.RelatedPlayers.Count);
    }

    [Fact]
    public async Task GetPlayer_WithRelatedPlayers_NoIpHistory_ReturnsEmpty()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "NoHistory",
            IpAddress = "10.0.0.1",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        // No PlayerIpAddresses entries for this player
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.RelatedPlayers);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(result.Result!.Data!.RelatedPlayers);
    }

    [Fact]
    public async Task GetPlayer_WithRelatedPlayers_SortsBannedFirst()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var cleanPlayerId = Guid.NewGuid();
        var bannedPlayerId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = (int)GameType.CallOfDuty4, Username = "Main", IpAddress = "10.0.0.1", FirstSeen = DateTime.UtcNow.AddDays(-10), LastSeen = DateTime.UtcNow });
        context.Players.Add(new Player { PlayerId = cleanPlayerId, GameType = (int)GameType.CallOfDuty4, Username = "Clean", IpAddress = "10.0.0.1", FirstSeen = DateTime.UtcNow.AddDays(-5), LastSeen = DateTime.UtcNow });
        context.Players.Add(new Player { PlayerId = bannedPlayerId, GameType = (int)GameType.CallOfDuty4, Username = "Banned", IpAddress = "10.0.0.1", FirstSeen = DateTime.UtcNow.AddDays(-5), LastSeen = DateTime.UtcNow });

        context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = playerId, Address = "10.0.0.1", LastUsed = DateTime.UtcNow });
        context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = cleanPlayerId, Address = "10.0.0.1", LastUsed = DateTime.UtcNow });
        context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = bannedPlayerId, Address = "10.0.0.1", LastUsed = DateTime.UtcNow });
        context.AdminActions.Add(new AdminAction { AdminActionId = Guid.NewGuid(), PlayerId = bannedPlayerId, Type = (int)AdminActionType.Ban, Text = "Ban", Created = DateTime.UtcNow, Expires = null });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.RelatedPlayers);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var related = result.Result!.Data!.RelatedPlayers;
        Assert.Equal(2, related.Count);
        Assert.Equal(bannedPlayerId, related[0].PlayerId);
        Assert.Equal(cleanPlayerId, related[1].PlayerId);
    }

    #endregion
}
