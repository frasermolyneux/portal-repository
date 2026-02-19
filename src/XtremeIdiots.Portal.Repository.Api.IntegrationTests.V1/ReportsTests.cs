using System.Net;
using System.Text;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class ReportsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetReports_ReturnsOk()
    {
        var response = await _client.GetAsync("/v1.0/reports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReport_ReturnsOk_WhenExists()
    {
        var reportId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.Reports.Add(new Report
            {
                ReportId = reportId,
                GameType = (int)GameType.CallOfDuty4,
                Comments = "Test report",
                Timestamp = DateTime.UtcNow,
                Closed = false
            });
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync($"/v1.0/reports/{reportId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test report", content);
    }

    [Fact]
    public async Task GetReport_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.GetAsync($"/v1.0/reports/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateReports_ReturnsCreated()
    {
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();
        var serverId = Guid.NewGuid();

        _factory.SeedDatabase(ctx =>
        {
            ctx.GameServers.Add(new GameServer
            {
                GameServerId = serverId,
                Title = "Report Test Server",
                GameType = (int)GameType.CallOfDuty4,
                Hostname = "127.0.0.1",
                QueryPort = 28960
            });
            ctx.SaveChanges();
        });

        var dtoPayload = new[]
        {
            new { PlayerId = playerId, UserProfileId = userProfileId, Comments = "Cheating in game", GameServerId = serverId }
        };

        var json = JsonConvert.SerializeObject(dtoPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1.0/reports", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateReports_ReturnsBadRequest_WhenGameServerDoesNotExist()
    {
        var dtoPayload = new[]
        {
            new { PlayerId = Guid.NewGuid(), UserProfileId = Guid.NewGuid(), Comments = "Bad report", GameServerId = Guid.NewGuid() }
        };

        var json = JsonConvert.SerializeObject(dtoPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1.0/reports", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
