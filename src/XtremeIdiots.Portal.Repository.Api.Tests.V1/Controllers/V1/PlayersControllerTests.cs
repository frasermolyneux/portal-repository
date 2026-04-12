using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
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
    private PlayersController CreateController(PortalDbContext context)
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        return new PlayersController(context, memoryCache);
    }

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

    #region RelatedPlayers Enrichment Tests

    [Fact]
    public async Task GetPlayer_WithRelatedPlayers_IncludesLastSeen()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var relatedPlayerId = Guid.NewGuid();
        var relatedLastSeen = DateTime.UtcNow.AddDays(-2);

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
        context.PlayerIpAddresses.Add(new PlayerIpAddress
        {
            PlayerIpAddressId = Guid.NewGuid(),
            PlayerId = relatedPlayerId,
            Address = "10.0.0.1",
            LastUsed = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IPlayersApi)controller;
        var result = await api.GetPlayer(playerId, PlayerEntityOptions.RelatedPlayers);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var related = Assert.Single(result.Result!.Data!.RelatedPlayers);
        Assert.Equal(relatedPlayerId, related.PlayerId);
        Assert.Equal(relatedLastSeen, related.LastSeen);
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
        context.PlayerIpAddresses.Add(new PlayerIpAddress
        {
            PlayerIpAddressId = Guid.NewGuid(),
            PlayerId = relatedPlayerId,
            Address = "10.0.0.1",
            LastUsed = DateTime.UtcNow
        });
        context.AdminActions.Add(new AdminAction
        {
            AdminActionId = Guid.NewGuid(),
            PlayerId = relatedPlayerId,
            Type = (int)AdminActionType.Ban,
            Text = "Permanent ban",
            Created = DateTime.UtcNow.AddDays(-1),
            Expires = null
        });
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
        context.PlayerIpAddresses.Add(new PlayerIpAddress
        {
            PlayerIpAddressId = Guid.NewGuid(),
            PlayerId = relatedPlayerId,
            Address = "10.0.0.1",
            LastUsed = DateTime.UtcNow
        });
        context.AdminActions.Add(new AdminAction
        {
            AdminActionId = Guid.NewGuid(),
            PlayerId = relatedPlayerId,
            Type = (int)AdminActionType.TempBan,
            Text = "Temp ban expired",
            Created = DateTime.UtcNow.AddDays(-10),
            Expires = DateTime.UtcNow.AddDays(-1)
        });
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
            context.PlayerIpAddresses.Add(new PlayerIpAddress
            {
                PlayerIpAddressId = Guid.NewGuid(),
                PlayerId = rpId,
                Address = "10.0.0.1",
                LastUsed = DateTime.UtcNow
            });
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

    #endregion
}
