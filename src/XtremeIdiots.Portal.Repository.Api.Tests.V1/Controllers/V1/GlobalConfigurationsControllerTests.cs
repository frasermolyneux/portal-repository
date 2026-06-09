using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class GlobalConfigurationsControllerTests
{
    private GlobalConfigurationsController CreateController(PortalDbContext context)
    {
        return new GlobalConfigurationsController(context);
    }

    [Fact]
    public async Task GetConfigurations_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "ns1",
            Configuration = "{\"key\":\"value1\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "ns2",
            Configuration = "{\"key\":\"value2\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.GetConfigurations();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, result.Result!.Data!.Items!.Count());
    }

    [Fact]
    public async Task GetConfigurations_EmptyDb_ReturnsEmpty()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.GetConfigurations();

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(result.Result!.Data!.Items!);
    }

    [Fact]
    public async Task GetConfiguration_ExistingNamespace_ReturnsConfig()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "test-ns",
            Configuration = "{\"setting\":true}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.GetConfiguration("test-ns");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("test-ns", result.Result!.Data!.Namespace);
        Assert.Equal("{\"setting\":true}", result.Result!.Data!.Configuration);
    }

    [Fact]
    public async Task GetConfiguration_NonExistent_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.GetConfiguration("nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task UpsertConfiguration_CreatesNew()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = "{\"schemaVersion\":1,\"enabled\":true,\"intervalSeconds\":60,\"messages\":[{\"message\":\"Welcome\",\"enabled\":true}]}"
        };
        var result = await api.UpsertConfiguration("broadcasts", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var entity = context.GlobalConfigurations.Single();
        Assert.Equal("broadcasts", entity.Namespace);
        Assert.Equal(dto.Configuration, entity.Configuration);
    }

    [Fact]
    public async Task UpsertConfiguration_UpdatesExisting()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "broadcasts",
            Configuration = "{\"schemaVersion\":1,\"enabled\":false,\"intervalSeconds\":30,\"messages\":[]}",
            LastModifiedUtc = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = "{\"schemaVersion\":1,\"enabled\":true,\"intervalSeconds\":45,\"messages\":[{\"message\":\"Updated\",\"enabled\":true}]}"
        };
        var result = await api.UpsertConfiguration("broadcasts", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var entity = context.GlobalConfigurations.Single();
        Assert.Equal(dto.Configuration, entity.Configuration);
    }

    [Fact]
    public async Task UpsertConfiguration_ServerListAliasNamespace_CreatesWithCanonicalNamespace()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = "{\"schemaVersion\":1,\"htmlBanner\":\"<b>Live</b>\"}"
        };
        var result = await api.UpsertConfiguration("serverList", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var entity = context.GlobalConfigurations.Single();
        Assert.Equal("serverlist", entity.Namespace);
        Assert.Equal(dto.Configuration, entity.Configuration);
    }

    [Fact]
    public async Task UpsertConfiguration_ServerListAliasNamespace_UpdatesLegacyNamespaceWithoutDuplicate()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverList",
            Configuration = "{\"schemaVersion\":1,\"htmlBanner\":\"old\"}",
            LastModifiedUtc = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = "{\"schemaVersion\":1,\"htmlBanner\":\"new\"}"
        };
        var result = await api.UpsertConfiguration("serverList", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.GlobalConfigurations);
        var entity = context.GlobalConfigurations.Single();
        Assert.Equal("serverlist", entity.Namespace);
        Assert.Equal(dto.Configuration, entity.Configuration);
    }

    [Fact]
    public async Task GetConfiguration_ServerListAliasNamespace_ReturnsCanonicalRow()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverlist",
            Configuration = "{\"schemaVersion\":1,\"htmlBanner\":\"<b>Live</b>\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.GetConfiguration("serverList");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("serverlist", result.Result!.Data!.Namespace);
    }

    [Fact]
    public async Task DeleteConfiguration_ServerListAliasNamespace_RemovesCanonicalRow()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverlist",
            Configuration = "{\"schemaVersion\":1,\"htmlBanner\":\"<b>Live</b>\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.DeleteConfiguration("serverList");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(context.GlobalConfigurations);
    }

    [Fact]
    public async Task UpsertConfiguration_UnknownNamespace_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto { Configuration = "{}" };
        var result = await api.UpsertConfiguration("unknown-namespace", dto);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Empty(context.GlobalConfigurations);
    }

    [Fact]
    public async Task DeleteConfiguration_ExistingNamespace_RemovesConfig()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "delete-ns",
            Configuration = "{}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.DeleteConfiguration("delete-ns");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(context.GlobalConfigurations);
    }

    [Fact]
    public async Task DeleteConfiguration_NonExistent_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.DeleteConfiguration("nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task UpsertConfiguration_InvalidJson_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto { Configuration = "not-json" };
        var result = await api.UpsertConfiguration("agent", dto);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UpsertConfiguration_KnownNamespace_UnsupportedSchemaVersion_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto { Configuration = "{\"schemaVersion\":999}" };
        var result = await api.UpsertConfiguration("agent", dto);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Empty(context.GlobalConfigurations);
    }

    [Fact]
    public async Task UpsertConfiguration_KnownNamespace_LegacySchemaVersion_Accepts()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto { Configuration = "{\"schemaVersion\":0,\"pollIntervalMs\":1000}" };
        var result = await api.UpsertConfiguration("agent", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.GlobalConfigurations);
    }

    [Fact]
    public async Task UpsertConfiguration_KnownNamespace_NullPayload_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto { Configuration = "null" };
        var result = await api.UpsertConfiguration("agent", dto);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Empty(context.GlobalConfigurations);
    }

    [Fact]
    public async Task UpsertConfiguration_EmptyNamespace_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto { Configuration = "{}" };
        var result = await api.UpsertConfiguration("", dto);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GetConfiguration_EmptyNamespace_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.GetConfiguration("");

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task DeleteConfiguration_EmptyNamespace_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.DeleteConfiguration("");

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
