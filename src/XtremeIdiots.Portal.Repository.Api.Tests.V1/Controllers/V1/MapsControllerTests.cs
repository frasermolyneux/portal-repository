using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class MapsControllerTests
{
    private MapsController CreateController(PortalDbContext context)
    {
        var logger = new Mock<ILogger<MapsController>>();
        return new MapsController(context, logger.Object);
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
}
