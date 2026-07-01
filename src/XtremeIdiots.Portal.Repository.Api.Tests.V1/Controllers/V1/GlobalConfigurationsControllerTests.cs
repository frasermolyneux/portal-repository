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
            Configuration = /*lang=json,strict*/ "{\"key\":\"value1\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "ns2",
            Configuration = /*lang=json,strict*/ "{\"key\":\"value2\"}",
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
            Configuration = /*lang=json,strict*/ "{\"setting\":true}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.GetConfiguration("test-ns");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("test-ns", result.Result!.Data!.Namespace);
        Assert.Equal(/*lang=json,strict*/ "{\"setting\":true}", result.Result!.Data!.Configuration);
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
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"enabled\":true,\"intervalSeconds\":60,\"messages\":[{\"message\":\"Welcome\",\"enabled\":true}]}"
        };
        var result = await api.UpsertConfiguration("broadcasts", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var entity = context.GlobalConfigurations.Single();
        Assert.Equal("broadcasts", entity.Namespace);
        Assert.Equal(dto.Configuration, entity.Configuration);

        var roundTrip = await api.GetConfiguration("broadcasts");
        Assert.Equal(HttpStatusCode.OK, roundTrip.StatusCode);
        Assert.Equal(dto.Configuration, roundTrip.Result!.Data!.Configuration);
    }

    [Fact]
    public async Task UpsertConfiguration_UpdatesExisting()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "broadcasts",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"enabled\":false,\"intervalSeconds\":30,\"messages\":[]}",
            LastModifiedUtc = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"enabled\":true,\"intervalSeconds\":45,\"messages\":[{\"message\":\"Updated\",\"enabled\":true}]}"
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
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"<b>Live</b>\"}"
        };
        var result = await api.UpsertConfiguration("serverList", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var entity = context.GlobalConfigurations.Single();
        Assert.Equal("serverlist", entity.Namespace);
        Assert.Equal(dto.Configuration, entity.Configuration);

        var roundTrip = await api.GetConfiguration("serverList");
        Assert.Equal(HttpStatusCode.OK, roundTrip.StatusCode);
        Assert.Equal(dto.Configuration, roundTrip.Result!.Data!.Configuration);
    }

    [Fact]
    public async Task UpsertConfiguration_ServerListAliasNamespace_UpdatesLegacyNamespaceWithoutDuplicate()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverList",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"old\"}",
            LastModifiedUtc = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"new\"}"
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
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"<b>Live</b>\"}",
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
    public async Task GetConfiguration_ServerListAliasNamespace_ReturnsLegacyRow_WhenOnlyLegacyExists()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverList",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"legacy\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.GetConfiguration("serverList");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("serverList", result.Result!.Data!.Namespace);
    }

    [Fact]
    public async Task GetConfiguration_ServerListAliasNamespace_PrefersCanonical_WhenBothExist()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverList",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"legacy\"}",
            LastModifiedUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverlist",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"canonical\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.GetConfiguration("serverList");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("serverlist", result.Result!.Data!.Namespace);
        Assert.Contains("canonical", result.Result!.Data!.Configuration);
    }

    [Fact]
    public async Task GetConfiguration_ServerListAliasNamespace_PrefersCanonical_WhenMixedCaseLegacyExists()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "ServerList",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"legacy\"}",
            LastModifiedUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverlist",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"canonical\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.GetConfiguration("serverList");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("serverlist", result.Result!.Data!.Namespace);
        Assert.Contains("canonical", result.Result!.Data!.Configuration);
    }

    [Fact]
    public async Task GetConfiguration_ServerListAliasNamespace_PrefersExactLegacy_WhenNoCanonicalExists()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "ServerList",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"mixed\"}",
            LastModifiedUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverList",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"legacy\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.GetConfiguration("serverList");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("serverList", result.Result!.Data!.Namespace);
        Assert.Contains("legacy", result.Result!.Data!.Configuration);
    }

    [Fact]
    public async Task GetConfiguration_ServerListAliasNamespace_FallbackPrefersNewestMixedCaseVariant()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "SERVERLIST",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"older\"}",
            LastModifiedUtc = DateTime.UtcNow.AddMinutes(-2)
        });
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "Serverlist",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"newer\"}",
            LastModifiedUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;
        var result = await api.GetConfiguration("serverList");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("Serverlist", result.Result!.Data!.Namespace);
        Assert.Contains("newer", result.Result!.Data!.Configuration);
    }

    [Fact]
    public async Task DeleteConfiguration_ServerListAliasNamespace_RemovesCanonicalRow()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverlist",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"<b>Live</b>\"}",
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
    public async Task DeleteConfiguration_ServerListAliasNamespace_RemovesLegacyRow_WhenOnlyLegacyExists()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverList",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"legacy\"}",
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
    public async Task DeleteConfiguration_ServerListAliasNamespace_RemovesBothVariants_WhenBothExist()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverList",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"legacy\"}",
            LastModifiedUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverlist",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"canonical\"}",
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
    public async Task DeleteConfiguration_ServerListAliasNamespace_RemovesMixedCaseLegacy_WhenPresent()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "ServerList",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"legacy\"}",
            LastModifiedUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverlist",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"canonical\"}",
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
    public async Task UpsertConfiguration_ServerListAliasNamespace_ConsolidatesToSingleCanonical_WhenBothExist()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverList",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"legacy\"}",
            LastModifiedUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverlist",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"canonical-old\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"new\"}"
        };

        var result = await api.UpsertConfiguration("serverList", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.GlobalConfigurations);
        var row = context.GlobalConfigurations.Single();
        Assert.Equal("serverlist", row.Namespace);
        Assert.Equal(dto.Configuration, row.Configuration);
    }

    [Fact]
    public async Task UpsertConfiguration_ServerListAliasNamespace_ConsolidatesMixedCaseLegacyToSingleCanonical()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "ServerList",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"legacy\"}",
            LastModifiedUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "serverlist",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"canonical-old\"}",
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"new\"}"
        };

        var result = await api.UpsertConfiguration("serverList", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.GlobalConfigurations);
        var row = context.GlobalConfigurations.Single();
        Assert.Equal("serverlist", row.Namespace);
        Assert.Equal(dto.Configuration, row.Configuration);
    }

    [Fact]
    public async Task UpsertConfiguration_ServerListAliasNamespace_NormalizesSingleMixedCaseRowToCanonical()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = "ServerList",
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"legacy\"}",
            LastModifiedUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"htmlBanner\":\"new\"}"
        };

        var result = await api.UpsertConfiguration("serverList", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.GlobalConfigurations);
        var row = context.GlobalConfigurations.Single();
        Assert.Equal("serverlist", row.Namespace);
        Assert.Equal(dto.Configuration, row.Configuration);
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
    public async Task UpsertConfiguration_Cod4xPluginNamespace_ValidPayload_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"enabled\":true,\"pluginRootDirectory\":\"/opt/cod4x/plugins\"}"
        };

        var result = await api.UpsertConfiguration("cod4xPlugin", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.GlobalConfigurations);
    }

    [Fact]
    public async Task UpsertConfiguration_Cod4xPluginNamespace_RelativeRootDirectory_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"enabled\":true,\"pluginRootDirectory\":\"plugins/cod4x\"}"
        };

        var result = await api.UpsertConfiguration("cod4xPlugin", dto);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Empty(context.GlobalConfigurations);
    }

    [Fact]
    public async Task UpsertConfiguration_Cod4xPowerNamespace_ValidPayload_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"enabled\":true,\"defaultPower\":50,\"tagMappings\":[{\"tag\":\"SeniorAdmin\",\"power\":80,\"enabled\":true}]}"
        };

        var result = await api.UpsertConfiguration("cod4xPower", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.GlobalConfigurations);
    }

    [Fact]
    public async Task UpsertConfiguration_Cod4xCommandsNamespace_ValidPayload_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"enabled\":true,\"commands\":{\"kick\":{\"enabled\":true,\"minPower\":35}}}"
        };

        var result = await api.UpsertConfiguration("cod4xCommands", dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.GlobalConfigurations);
    }

    [Fact]
    public async Task UpsertConfiguration_BroadcastsInvalidPayload_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":1,\"enabled\":true,\"intervalSeconds\":0,\"messages\":[{\"message\":\"\",\"enabled\":true}]}"
        };
        var result = await api.UpsertConfiguration("broadcasts", dto);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Empty(context.GlobalConfigurations);
    }

    [Fact]
    public async Task UpsertConfiguration_ServerListInvalidPayload_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGlobalConfigurationsApi)controller;

        var dto = new UpsertConfigurationDto
        {
            Configuration = /*lang=json,strict*/ "{\"schemaVersion\":999,\"htmlBanner\":\"<b>bad</b>\"}"
        };
        var result = await api.UpsertConfiguration("serverList", dto);

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

        var dto = new UpsertConfigurationDto { Configuration = /*lang=json,strict*/ "{\"schemaVersion\":999}" };
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

        var dto = new UpsertConfigurationDto { Configuration = /*lang=json,strict*/ "{\"schemaVersion\":0,\"pollIntervalMs\":1000}" };
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
