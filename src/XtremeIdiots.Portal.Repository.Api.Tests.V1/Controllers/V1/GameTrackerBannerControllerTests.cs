using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
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
