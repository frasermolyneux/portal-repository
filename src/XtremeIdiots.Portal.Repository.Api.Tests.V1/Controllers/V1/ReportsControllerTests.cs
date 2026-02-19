using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Reports;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class ReportsControllerTests
{
    private ReportsController CreateController(PortalDbContext context)
    {
        return new ReportsController(context);
    }

    [Fact]
    public async Task GetReport_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var reportId = Guid.NewGuid();
        context.Reports.Add(new Report
        {
            ReportId = reportId,
            GameType = (int)GameType.CallOfDuty4,
            Comments = "Test report",
            Timestamp = DateTime.UtcNow,
            Closed = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IReportsApi)controller;
        var result = await api.GetReport(reportId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetReport_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IReportsApi)controller;
        var result = await api.GetReport(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetReports_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Reports.Add(new Report
        {
            ReportId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Comments = "Report 1",
            Timestamp = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IReportsApi)controller;
        var result = await api.GetReports(null, null, null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task CreateReports_WithNullGameServerId_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var controller = CreateController(context);
        var api = (IReportsApi)controller;

        var dtos = new List<CreateReportDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Report 1")
        };

        // GameServerId defaults to null (private set), so controller returns BadRequest
        var result = await api.CreateReports(dtos);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task CloseReport_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var reportId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();
        context.Reports.Add(new Report
        {
            ReportId = reportId,
            GameType = (int)GameType.CallOfDuty4,
            Comments = "Test report",
            Timestamp = DateTime.UtcNow,
            Closed = false
        });
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "Admin"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IReportsApi)controller;

        var closeDto = new CloseReportDto(userProfileId, "Closed by admin");

        var result = await api.CloseReport(reportId, closeDto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task CloseReport_WithInvalidReportId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IReportsApi)controller;

        var closeDto = new CloseReportDto(Guid.NewGuid(), "Closed");

        var result = await api.CloseReport(Guid.NewGuid(), closeDto);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
