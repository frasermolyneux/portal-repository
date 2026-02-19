using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V2;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V2.Controllers.V2;

public class HealthControllerTests
{
    private readonly Mock<HealthCheckService> _healthCheckServiceMock;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _healthCheckServiceMock = new Mock<HealthCheckService>();
        _controller = new HealthController(_healthCheckServiceMock.Object);
    }

    [Fact]
    public async Task GetHealth_WhenHealthy_Returns200()
    {
        // Arrange
        var healthReport = new HealthReport(
            entries: new Dictionary<string, HealthReportEntry>(),
            status: HealthStatus.Healthy,
            totalDuration: TimeSpan.FromMilliseconds(100));

        _healthCheckServiceMock
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth(CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetHealth_WhenUnhealthy_Returns503()
    {
        // Arrange
        var healthReport = new HealthReport(
            entries: new Dictionary<string, HealthReportEntry>(),
            status: HealthStatus.Unhealthy,
            totalDuration: TimeSpan.FromMilliseconds(100));

        _healthCheckServiceMock
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth(CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetHealth_WhenDegraded_Returns503()
    {
        // Arrange
        var healthReport = new HealthReport(
            entries: new Dictionary<string, HealthReportEntry>(),
            status: HealthStatus.Degraded,
            totalDuration: TimeSpan.FromMilliseconds(100));

        _healthCheckServiceMock
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth(CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetHealth_WhenHealthy_ReturnsStatusInBody()
    {
        // Arrange
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["database"] = new HealthReportEntry(
                status: HealthStatus.Healthy,
                description: "Database is healthy",
                duration: TimeSpan.FromMilliseconds(50),
                exception: null,
                data: null,
                tags: null)
        };

        var healthReport = new HealthReport(
            entries: entries,
            status: HealthStatus.Healthy,
            totalDuration: TimeSpan.FromMilliseconds(100));

        _healthCheckServiceMock
            .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthReport);

        // Act
        var result = await _controller.GetHealth(CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.NotNull(objectResult.Value);
    }
}
