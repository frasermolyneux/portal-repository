using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class GameServerConfigurationsControllerTests
{
    private GameServerConfigurationsController CreateController(PortalDbContext context)
    {
        return new GameServerConfigurationsController(context);
    }

    private GameServer CreateTestGameServer(Guid? gameServerId = null)
    {
        return new GameServer
        {
            GameServerId = gameServerId ?? Guid.NewGuid(),
            Title = "Test Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960,
            Deleted = false
        };
    }

    [Fact]
    public async Task GetConfigurations_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateTestGameServer();
        context.GameServers.Add(gs);
        context.GameServerConfigurations.Add(new GameServerConfiguration
        {
            GameServerId = gs.GameServerId,
            Namespace = "ns1",
            Configuration = "{\"key\":\"value1\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        context.GameServerConfigurations.Add(new GameServerConfiguration
        {
            GameServerId = gs.GameServerId,
            Namespace = "ns2",
            Configuration = "{\"key\":\"value2\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;
        var result = await api.GetConfigurations(gs.GameServerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, result.Result!.Data!.Items!.Count());
    }

    [Fact]
    public async Task GetConfigurations_EmptyDb_ReturnsEmpty()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateTestGameServer();
        context.GameServers.Add(gs);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;
        var result = await api.GetConfigurations(gs.GameServerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(result.Result!.Data!.Items!);
    }

    [Fact]
    public async Task GetConfigurations_NonExistentServer_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;
        var result = await api.GetConfigurations(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetConfigurations_FiltersToCorrectServer()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs1 = CreateTestGameServer();
        var gs2 = CreateTestGameServer();
        context.GameServers.AddRange(gs1, gs2);
        context.GameServerConfigurations.Add(new GameServerConfiguration
        {
            GameServerId = gs1.GameServerId,
            Namespace = "ns1",
            Configuration = "{}",
            LastModifiedUtc = DateTime.UtcNow
        });
        context.GameServerConfigurations.Add(new GameServerConfiguration
        {
            GameServerId = gs2.GameServerId,
            Namespace = "ns2",
            Configuration = "{}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;
        var result = await api.GetConfigurations(gs1.GameServerId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(result.Result!.Data!.Items!);
    }

    [Fact]
    public async Task GetConfiguration_ExistingNamespace_ReturnsConfig()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateTestGameServer();
        context.GameServers.Add(gs);
        context.GameServerConfigurations.Add(new GameServerConfiguration
        {
            GameServerId = gs.GameServerId,
            Namespace = "test-ns",
            Configuration = "{\"setting\":true}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;
        var result = await api.GetConfiguration(gs.GameServerId, "test-ns");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("test-ns", result.Result!.Data!.Namespace);
        Assert.Equal("{\"setting\":true}", result.Result!.Data!.Configuration);
    }

    [Fact]
    public async Task GetConfiguration_NonExistent_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;
        var result = await api.GetConfiguration(Guid.NewGuid(), "nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task UpsertConfiguration_CreatesNew()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateTestGameServer();
        context.GameServers.Add(gs);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto { Configuration = "{\"new\":true}" };
        var result = await api.UpsertConfiguration(gs.GameServerId, "new-ns", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var entity = context.GameServerConfigurations.Single();
        Assert.Equal(gs.GameServerId, entity.GameServerId);
        Assert.Equal("new-ns", entity.Namespace);
        Assert.Equal("{\"new\":true}", entity.Configuration);
    }

    [Fact]
    public async Task UpsertConfiguration_UpdatesExisting()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateTestGameServer();
        context.GameServers.Add(gs);
        context.GameServerConfigurations.Add(new GameServerConfiguration
        {
            GameServerId = gs.GameServerId,
            Namespace = "existing-ns",
            Configuration = "{\"old\":true}",
            LastModifiedUtc = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto { Configuration = "{\"updated\":true}" };
        var result = await api.UpsertConfiguration(gs.GameServerId, "existing-ns", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var entity = context.GameServerConfigurations.Single();
        Assert.Equal("{\"updated\":true}", entity.Configuration);
    }

    [Fact]
    public async Task DeleteConfiguration_ExistingNamespace_RemovesConfig()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateTestGameServer();
        context.GameServers.Add(gs);
        context.GameServerConfigurations.Add(new GameServerConfiguration
        {
            GameServerId = gs.GameServerId,
            Namespace = "delete-ns",
            Configuration = "{}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;
        var result = await api.DeleteConfiguration(gs.GameServerId, "delete-ns");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(context.GameServerConfigurations);
    }

    [Fact]
    public async Task DeleteConfiguration_NonExistent_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;
        var result = await api.DeleteConfiguration(Guid.NewGuid(), "nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task UpsertConfiguration_NonExistentServer_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto { Configuration = "{}" };
        var result = await api.UpsertConfiguration(Guid.NewGuid(), "ns", dto);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task UpsertConfiguration_InvalidJson_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateTestGameServer();
        context.GameServers.Add(gs);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto { Configuration = "not-json" };
        var result = await api.UpsertConfiguration(gs.GameServerId, "ns", dto);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UpsertConfiguration_EmptyNamespace_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateTestGameServer();
        context.GameServers.Add(gs);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGameServerConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto { Configuration = "{}" };
        var result = await api.UpsertConfiguration(gs.GameServerId, "", dto);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
