using System.Net;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class GameServersControllerTests
{
    private GameServersController CreateController(PortalDbContext context)
    {
        return new GameServersController(context);
    }

    [Fact]
    public async Task GetGameServer_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Test Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960,
            Deleted = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServersApi)controller;
        var result = await api.GetGameServer(gameServerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetGameServer_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersApi)controller;
        var result = await api.GetGameServer(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetGameServer_DeletedServer_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Deleted Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960,
            Deleted = true
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServersApi)controller;
        var result = await api.GetGameServer(gameServerId);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetGameServers_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GameServers.Add(new GameServer
        {
            GameServerId = Guid.NewGuid(),
            Title = "Server 1",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "host1",
            QueryPort = 28960,
            Deleted = false
        });
        context.GameServers.Add(new GameServer
        {
            GameServerId = Guid.NewGuid(),
            Title = "Server 2",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "host2",
            QueryPort = 28961,
            Deleted = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServersApi)controller;
        var result = await api.GetGameServers(null, null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task CreateGameServer_CreatesEntity()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersApi)controller;

        var dto = new CreateGameServerDto("New Server", GameType.CallOfDuty4, "newhost", 28960);

        var result = await api.CreateGameServer(dto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.GameServers);
    }

    [Fact]
    public async Task CreateGameServers_CreatesMultiple()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersApi)controller;

        var dtos = new List<CreateGameServerDto>
        {
            new("Server A", GameType.CallOfDuty4, "hostA", 28960),
            new("Server B", GameType.CallOfDuty4, "hostB", 28961)
        };

        var result = await api.CreateGameServers(dtos);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, context.GameServers.Count());
    }

    [Fact]
    public async Task UpdateGameServer_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Original Title",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServersApi)controller;

        var editDto = new EditGameServerDto(gameServerId)
        {
            Title = "Updated Title"
        };

        var result = await api.UpdateGameServer(editDto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task UpdateGameServer_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersApi)controller;

        var editDto = new EditGameServerDto(Guid.NewGuid())
        {
            Title = "Updated Title"
        };

        var result = await api.UpdateGameServer(editDto);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task DeleteGameServer_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersApi)controller;
        var result = await api.DeleteGameServer(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
