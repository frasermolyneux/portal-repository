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
    public async Task GetGameServer_AgentEnabledDefaultsFalse()
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
        Assert.False(result.Result!.Data!.AgentEnabled);
    }

    [Fact]
    public async Task GetGameServer_AgentEnabledTrue_MapsCorrectly()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Agent Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960,
            AgentEnabled = true,
            Deleted = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServersApi)controller;
        var result = await api.GetGameServer(gameServerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.True(result.Result!.Data!.AgentEnabled);
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
    public async Task CreateGameServer_MapsAllFieldsToEntity()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersApi)controller;

        var dto = new CreateGameServerDto("Full Server", GameType.CallOfDuty4, "fullhost", 28960)
        {
            FtpHostname = "ftp.example.com",
            FtpPort = 2121,
            FtpUsername = "user",
            FtpPassword = "pass",
            RconPassword = "rcon",
            ServerListPosition = 5,
            HtmlBanner = "<img src='banner.png' />",
            BotEnabled = true,
            AgentEnabled = true,
            BannerServerListEnabled = true,
            PortalServerListEnabled = true,
            LiveTrackingEnabled = true
        };

        var result = await api.CreateGameServer(dto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var entity = context.GameServers.Single();
        Assert.Equal("Full Server", entity.Title);
        Assert.Equal("fullhost", entity.Hostname);
        Assert.Equal(28960, entity.QueryPort);
        Assert.Equal((int)GameType.CallOfDuty4, entity.GameType);
        Assert.Equal("ftp.example.com", entity.FtpHostname);
        Assert.Equal(2121, entity.FtpPort);
        Assert.Equal("user", entity.FtpUsername);
        Assert.Equal("pass", entity.FtpPassword);
        Assert.Equal("rcon", entity.RconPassword);
        Assert.Equal(5, entity.ServerListPosition);
        Assert.Equal("<img src='banner.png' />", entity.HtmlBanner);
        Assert.True(entity.BotEnabled);
        Assert.True(entity.AgentEnabled);
        Assert.True(entity.BannerServerListEnabled);
        Assert.True(entity.PortalServerListEnabled);
        Assert.True(entity.LiveTrackingEnabled);
    }

    [Fact]
    public async Task CreateGameServer_WithAgentEnabled_PersistsValue()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersApi)controller;

        var dto = new CreateGameServerDto("Agent Server", GameType.CallOfDuty4, "newhost", 28960)
        {
            AgentEnabled = true
        };

        var result = await api.CreateGameServer(dto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var entity = context.GameServers.Single();
        Assert.True(entity.AgentEnabled);
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
    public async Task UpdateGameServer_AgentEnabled_UpdatesValue()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960,
            AgentEnabled = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServersApi)controller;

        var editDto = new EditGameServerDto(gameServerId)
        {
            AgentEnabled = true
        };

        var result = await api.UpdateGameServer(editDto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var entity = context.GameServers.Single();
        Assert.True(entity.AgentEnabled);
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

    [Fact]
    public async Task GetGameServers_AgentEnabledFilter_ReturnsOnlyAgentEnabled()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GameServers.Add(new GameServer
        {
            GameServerId = Guid.NewGuid(),
            Title = "Agent Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "host1",
            QueryPort = 28960,
            AgentEnabled = true,
            Deleted = false
        });
        context.GameServers.Add(new GameServer
        {
            GameServerId = Guid.NewGuid(),
            Title = "Normal Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "host2",
            QueryPort = 28961,
            AgentEnabled = false,
            Deleted = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServersApi)controller;
        var result = await api.GetGameServers(null, null, GameServerFilter.AgentEnabled, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Single(items);
        Assert.Equal("Agent Server", items[0].Title);
        Assert.True(items[0].AgentEnabled);
    }
}
