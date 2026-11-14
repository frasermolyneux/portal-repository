using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        Assert.Equal(ApiErrorCodes.InvalidDemoFile, result.Result?.Errors?.FirstOrDefault()?.Code);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetDemoFile_WithoutFiles_ReturnsNoFilesProvided(bool useNullCollection)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        IFormFileCollection? files = useNullCollection ? null : new FormFileCollection();

        var result = await controller.SetDemoFile(Guid.NewGuid(), files);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var response = Assert.IsType<MX.Api.Abstractions.ApiResponse<object>>(objectResult.Value);
        Assert.Equal(ApiErrorCodes.NoFilesProvided, Assert.Single(response.Errors!).Code);
    }

    [Fact]
    public async Task SetDemoFile_WithInvalidExtension_ReturnsInvalidFileType()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        await using var content = new MemoryStream([1, 2, 3]);
        var files = new FormFileCollection
        {
            new FormFile(content, 0, content.Length, "files", "invalid.txt")
        };
        var controller = CreateController(context);

        var result = await controller.SetDemoFile(Guid.NewGuid(), files);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var response = Assert.IsType<MX.Api.Abstractions.ApiResponse<object>>(objectResult.Value);
        Assert.Equal(ApiErrorCodes.InvalidFileType, Assert.Single(response.Errors!).Code);
    }

    [Fact]
    public async Task SetDemoFile_WithMultipleFiles_UsesFirstFile()
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
            .Setup(x => x.ProcessAsync(It.IsAny<string>(), GameType.CallOfDuty5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DemoFileProcessingResult(
                "stored.dm_6",
                new Uri("https://storage.example/demos/stored.dm_6"),
                new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc),
                "mp_cassino5",
                "xidmace3",
                "dm",
                "^1>XI< ^3DM3",
                798868));
        await using var firstContent = new MemoryStream([1, 2, 3]);
        await using var secondContent = new MemoryStream([4, 5, 6]);
        var files = new FormFileCollection
        {
            new FormFile(firstContent, 0, firstContent.Length, "files", "first.dm_6"),
            new FormFile(secondContent, 0, secondContent.Length, "files", "second.dm_6")
        };
        var controller = CreateController(context, processor);

        var result = await controller.SetDemoFile(demoId, files);

        Assert.Equal(StatusCodes.Status200OK, Assert.IsType<StatusCodeResult>(result).StatusCode);
        Assert.Equal("first", Assert.Single(context.Demos).Title);
        processor.Verify(
            x => x.ProcessAsync(It.IsAny<string>(), GameType.CallOfDuty5, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
