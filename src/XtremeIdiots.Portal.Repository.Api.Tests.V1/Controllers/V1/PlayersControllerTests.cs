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
}
