using System.Net;
using Moq;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Demos;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class DemosControllerTests
{
    private static DemosController CreateController(
        PortalDbContext context,
        Mock<IDemoFileProcessor>? demoFileProcessor = null)
    {
        demoFileProcessor ??= new Mock<IDemoFileProcessor>();
        return new DemosController(context, demoFileProcessor.Object);
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

    [Fact]
    public async Task SetDemoFile_WithValidDemo_PersistsParsedMetadata()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var demoId = Guid.NewGuid();
        context.Demos.Add(new Demo
        {
            DemoId = demoId,
            GameType = (int)GameType.CallOfDuty5
        });
        await context.SaveChangesAsync();

        var processedDemo = new DemoFileProcessingResult(
            "stored.dm_6",
            new Uri("https://storage.example/demos/stored.dm_6"),
            new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc),
            "mp_cassino5",
            "xidmace3",
            "dm",
            "^1>XI< ^3DM3",
            798868);
        var processor = new Mock<IDemoFileProcessor>();
        processor
            .Setup(x => x.ProcessAsync("demo.tmp", GameType.CallOfDuty5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(processedDemo);
        var controller = CreateController(context, processor);
        var api = (IDemosApi)controller;

        var result = await api.SetDemoFile(demoId, "uploaded.dm_6", "demo.tmp");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var demo = Assert.Single(context.Demos);
        Assert.Equal("uploaded", demo.Title);
        Assert.Equal(processedDemo.BlobKey, demo.FileName);
        Assert.Equal(processedDemo.Created, demo.Created);
        Assert.Equal(processedDemo.Map, demo.Map);
        Assert.Equal(processedDemo.Mod, demo.Mod);
        Assert.Equal(processedDemo.GameMode, demo.GameMode);
        Assert.Equal(processedDemo.ServerName, demo.ServerName);
        Assert.Equal(processedDemo.FileSize, demo.FileSize);
        Assert.Equal(processedDemo.BlobUri.ToString(), demo.FileUri);
    }

    [Fact]
    public async Task SetDemoFile_WithInvalidDemo_ReturnsUnprocessableEntityWithoutPersistingSentinels()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var demoId = Guid.NewGuid();
        context.Demos.Add(new Demo
        {
            DemoId = demoId,
            GameType = (int)GameType.CallOfDuty5
        });
        await context.SaveChangesAsync();

        var processor = new Mock<IDemoFileProcessor>();
        processor
            .Setup(x => x.ProcessAsync("demo.tmp", GameType.CallOfDuty5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("Invalid demo"));
        var controller = CreateController(context, processor);
        var api = (IDemosApi)controller;

        var result = await api.SetDemoFile(demoId, "uploaded.dm_6", "demo.tmp");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.StatusCode);
        var demo = Assert.Single(context.Demos);
        Assert.Null(demo.Title);
        Assert.Null(demo.FileName);
        Assert.Null(demo.Map);
        Assert.Null(demo.Mod);
        Assert.Null(demo.GameMode);
        Assert.Null(demo.ServerName);
        Assert.Null(demo.FileUri);
        Assert.Equal(0, demo.FileSize);
    }
}
