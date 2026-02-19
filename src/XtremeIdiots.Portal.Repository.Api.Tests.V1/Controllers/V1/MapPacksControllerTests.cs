using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapPacks;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class MapPacksControllerTests
{
    private MapPacksController CreateController(PortalDbContext context)
    {
        return new MapPacksController(context);
    }

    [Fact]
    public async Task GetMapPack_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var mapPackId = Guid.NewGuid();
        context.MapPacks.Add(new MapPack
        {
            MapPackId = mapPackId,
            Title = "Test Pack",
            Description = "Test",
            GameMode = "TDM",
            Deleted = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapPacksApi)controller;
        var result = await api.GetMapPack(mapPackId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetMapPack_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IMapPacksApi)controller;
        var result = await api.GetMapPack(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetMapPack_DeletedPack_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var mapPackId = Guid.NewGuid();
        context.MapPacks.Add(new MapPack
        {
            MapPackId = mapPackId,
            Title = "Deleted Pack",
            Description = "Test",
            GameMode = "TDM",
            Deleted = true
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapPacksApi)controller;
        var result = await api.GetMapPack(mapPackId);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetMapPacks_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.MapPacks.Add(new MapPack
        {
            MapPackId = Guid.NewGuid(),
            Title = "Pack 1",
            Description = "Test",
            GameMode = "TDM",
            Deleted = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapPacksApi)controller;
        var result = await api.GetMapPacks(null, null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task CreateMapPack_CreatesEntity()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IMapPacksApi)controller;

        var dto = new CreateMapPackDto(Guid.NewGuid(), "New Pack", "TDM")
        {
            Description = "Test"
        };

        var result = await api.CreateMapPack(dto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.MapPacks);
    }

    [Fact]
    public async Task UpdateMapPack_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var mapPackId = Guid.NewGuid();
        context.MapPacks.Add(new MapPack
        {
            MapPackId = mapPackId,
            Title = "Original",
            Description = "Test",
            GameMode = "TDM"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapPacksApi)controller;

        var dto = new UpdateMapPackDto(mapPackId)
        {
            Title = "Updated"
        };

        var result = await api.UpdateMapPack(dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task UpdateMapPack_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IMapPacksApi)controller;

        var dto = new UpdateMapPackDto(Guid.NewGuid())
        {
            Title = "Updated"
        };

        var result = await api.UpdateMapPack(dto);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task DeleteMapPack_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var mapPackId = Guid.NewGuid();
        context.MapPacks.Add(new MapPack
        {
            MapPackId = mapPackId,
            Title = "ToDelete",
            Description = "Test",
            GameMode = "TDM"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IMapPacksApi)controller;
        var result = await api.DeleteMapPack(mapPackId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task DeleteMapPack_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IMapPacksApi)controller;
        var result = await api.DeleteMapPack(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
