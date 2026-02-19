using System.Net;
using Moq;
using Microsoft.Extensions.Configuration;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Demos;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class DemosControllerTests
{
    private DemosController CreateController(PortalDbContext context)
    {
        var config = new Mock<IConfiguration>();
        return new DemosController(context, config.Object);
    }

    [Fact]
    public async Task GetDemo_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var demoId = Guid.NewGuid();
        context.Demos.Add(new Demo
        {
            DemoId = demoId,
            GameType = (int)GameType.CallOfDuty4,
            Title = "TestDemo"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDemosApi)controller;
        var result = await api.GetDemo(demoId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetDemo_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IDemosApi)controller;
        var result = await api.GetDemo(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetDemos_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Demos.Add(new Demo
        {
            DemoId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Title = "Demo1"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDemosApi)controller;
        var result = await api.GetDemos(null, null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task CreateDemo_CreatesEntity()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IDemosApi)controller;

        var dto = new CreateDemoDto(GameType.CallOfDuty4, Guid.NewGuid());

        var result = await api.CreateDemo(dto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.Demos);
    }

    [Fact]
    public async Task DeleteDemo_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var demoId = Guid.NewGuid();
        context.Demos.Add(new Demo
        {
            DemoId = demoId,
            GameType = (int)GameType.CallOfDuty4,
            Title = "ToDelete"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IDemosApi)controller;
        var result = await api.DeleteDemo(demoId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(context.Demos);
    }

    [Fact]
    public async Task DeleteDemo_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IDemosApi)controller;
        var result = await api.DeleteDemo(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
