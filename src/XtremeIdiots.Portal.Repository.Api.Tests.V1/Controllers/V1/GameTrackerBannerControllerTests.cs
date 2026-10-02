using System.Net;
using System.Reflection;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MX.Api.Abstractions;
using Moq;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameTracker;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class GameTrackerBannerControllerTests
{
    private GameTrackerBannerController CreateController(
        ILogger<GameTrackerBannerController>? logger = null,
        IConfiguration? configuration = null)
    {
        logger ??= new Mock<ILogger<GameTrackerBannerController>>().Object;
        configuration ??= new Mock<IConfiguration>().Object;
        return new GameTrackerBannerController(logger, configuration);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var mockConfig = new Mock<IConfiguration>();
        Assert.Throws<ArgumentNullException>(() => new GameTrackerBannerController(null!, mockConfig.Object));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var mockLogger = new Mock<ILogger<GameTrackerBannerController>>();
        Assert.Throws<ArgumentNullException>(() => new GameTrackerBannerController(mockLogger.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        var controller = CreateController();
        Assert.NotNull(controller);
    }

    [Fact]
    public async Task GetGameTrackerBanner_WithMissingBlobEndpoint_ReturnsInternalServerError()
    {
        var mockConfig = new Mock<IConfiguration>();
        var controller = CreateController(configuration: mockConfig.Object);
        var api = (IGameTrackerBannerApi)controller;

        var result = await api.GetGameTrackerBanner("192.168.1.1", "28960", "banner_1");

        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task GetGameTrackerBanner_WithEmptyBlobEndpoint_ReturnsInternalServerError()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["appdata_storage_blob_endpoint"]).Returns(string.Empty);
        var controller = CreateController(configuration: mockConfig.Object);
        var api = (IGameTrackerBannerApi)controller;

        var result = await api.GetGameTrackerBanner("192.168.1.1", "28960", "banner_1");

        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task GetGameTrackerBanner_WhenRetrievalFails_DoesNotLogRequestValues()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["appdata_storage_blob_endpoint"]).Returns("invalid-uri");
        var mockLogger = new Mock<ILogger<GameTrackerBannerController>>();
        var controller = CreateController(mockLogger.Object, mockConfig.Object);
        const string ipAddress = "request-ip-marker";
        const int port = 27182;
        var queryPort = port.ToString();
        const string imageName = "request-image-marker";

        var result = await controller.GetGameTrackerBanner(ipAddress, port, imageName);

        Assert.Equal((int)HttpStatusCode.InternalServerError, Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode);

        var logInvocation = Assert.Single(mockLogger.Invocations, invocation => invocation.Method.Name == nameof(ILogger.Log));
        var loggedMessage = logInvocation.Arguments[2]?.ToString();
        Assert.Equal(LogLevel.Error, logInvocation.Arguments[0]);
        Assert.Equal("Failed to retrieve game tracker banner", loggedMessage);
        var exception = Assert.IsAssignableFrom<Exception>(logInvocation.Arguments[3]);
        var loggedDetails = $"{loggedMessage} {exception}";
        Assert.DoesNotContain(ipAddress, loggedDetails);
        Assert.DoesNotContain(queryPort, loggedDetails);
        Assert.DoesNotContain(imageName, loggedDetails);
    }

    [Fact]
    public async Task UpdateBannerImageAndRedirect_WhenDownloadFails_LogsNoRequestValuesAndReturnsFallback()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["GameTracker:BannerBaseUrl"]).Returns("://");
        var mockLogger = new Mock<ILogger<GameTrackerBannerController>>();
        var controller = CreateController(mockLogger.Object, mockConfig.Object);
        const string ipAddress = "request-ip-marker";
        const string queryPort = "request-port-marker";
        const string imageName = "request-image-marker";
        var updateMethod = typeof(GameTrackerBannerController).GetMethod("UpdateBannerImageAndRedirect", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(updateMethod);

        var updateTask = (Task<ApiResult<GameTrackerBannerDto>>)updateMethod.Invoke(controller,
        [
            ipAddress,
            queryPort,
            imageName,
            new BlobClient(new Uri("https://example.blob.core.windows.net/gametracker/banner")),
            true,
            CancellationToken.None
        ])!;
        var result = await updateTask;

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(":/request-ip-marker:request-port-marker/request-image-marker", result.Result?.Data?.BannerUrl);

        var logInvocation = Assert.Single(mockLogger.Invocations, invocation => invocation.Method.Name == nameof(ILogger.Log));
        var loggedMessage = logInvocation.Arguments[2]?.ToString();
        Assert.Equal(LogLevel.Error, logInvocation.Arguments[0]);
        Assert.Equal("Failed to update banner image", loggedMessage);
        var exception = Assert.IsAssignableFrom<Exception>(logInvocation.Arguments[3]);
        var loggedDetails = $"{loggedMessage} {exception}";
        Assert.DoesNotContain(ipAddress, loggedDetails);
        Assert.DoesNotContain(queryPort, loggedDetails);
        Assert.DoesNotContain(imageName, loggedDetails);
    }

    [Fact(Skip = "Requires Azure Blob Storage")]
    public async Task GetGameTrackerBanner_WithValidConfig_ReturnsBanner()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["appdata_storage_blob_endpoint"]).Returns("https://fake.blob.core.windows.net");
        var controller = CreateController(configuration: mockConfig.Object);
        var api = (IGameTrackerBannerApi)controller;

        var result = await api.GetGameTrackerBanner("192.168.1.1", "28960", "banner_1");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}
