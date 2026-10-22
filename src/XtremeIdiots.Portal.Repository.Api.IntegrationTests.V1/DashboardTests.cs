using System.Net;

using Newtonsoft.Json;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;
using XtremeIdiots.Portal.Repository.DataLib;

using DashboardHomeDto = XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard.DashboardHomeDto;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class DashboardTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DashboardTests(CustomWebApplicationFactory factory)
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
    public async Task GetAnalyticsDashboardHome_ReturnsWindowAndBucket_ForValidQuery()
    {
        var fromUtc = DateTime.UtcNow.AddDays(-1);
        var toUtc = DateTime.UtcNow;
        var response = await _client.GetAsync(
            $"/v1.0/analytics/dashboard/home?fromUtc={Uri.EscapeDataString(fromUtc.ToString("O"))}&toUtc={Uri.EscapeDataString(toUtc.ToString("O"))}&bucket={AnalyticsBucket.OneDay}&top=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiResponse<DashboardHomeDto>>(content);

        var data = Assert.IsType<DashboardHomeDto>(result?.Data);
        Assert.Equal(fromUtc, data.Window.FromUtc);
        Assert.Equal(toUtc, data.Window.ToUtc);
        Assert.Equal(AnalyticsBucket.OneDay, data.Bucket);
    }

    [Theory]
    [InlineData("2026-10-21T00:00:00Z", "2026-10-20T00:00:00Z", "OneDay", "10")]
    [InlineData("2026-10-20T00:00:00Z", "2026-10-21T00:00:00Z", "invalid", "10")]
    [InlineData("2026-10-20T00:00:00Z", "2026-10-21T00:00:00Z", "OneDay", "0")]
    public async Task GetAnalyticsDashboardHome_ReturnsBadRequest_ForInvalidQuery(
        string fromUtc,
        string toUtc,
        string bucket,
        string top)
    {
        var response = await _client.GetAsync(
            $"/v1.0/analytics/dashboard/home?fromUtc={Uri.EscapeDataString(fromUtc)}&toUtc={Uri.EscapeDataString(toUtc)}&bucket={bucket}&top={top}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboardSummary_ReturnsOk_WithValidStructure()
    {
        var response = await _client.GetAsync("/v1.0/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiResponse<DashboardSummaryDto>>(content);

        Assert.NotNull(result?.Data);
        Assert.True(result.Data.TotalServers >= 0);
        Assert.True(result.Data.TotalPlayersOnline >= 0);
        Assert.True(result.Data.UnclaimedBanCount >= 0);
        Assert.True(result.Data.OpenReportCount >= 0);
        Assert.NotNull(result.Data.RecentActions24h);
        Assert.NotNull(result.Data.RecentActions7d);
    }

    [Fact]
    public async Task GetDashboardSummary_ReturnsCorrectCounts_WithSeededData()
    {
        var serverId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var adminProfileId = Guid.NewGuid();

        _factory.SeedDatabase(ctx =>
        {
            ctx.GameServers.Add(new GameServer
            {
                GameServerId = serverId,
                Title = "Test Server",
                GameType = (int)GameType.CallOfDuty4,
                Hostname = "127.0.0.1",
                QueryPort = 28960,
                AgentEnabled = true,
            });

            ctx.Players.Add(new Player
            {
                PlayerId = playerId,
                GameType = (int)GameType.CallOfDuty4,
                Username = "DashboardTestPlayer",
                Guid = $"dash-{Guid.NewGuid()}",
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            });

            // Unclaimed ban (no UserProfileId)
            ctx.AdminActions.Add(new AdminAction
            {
                AdminActionId = Guid.NewGuid(),
                PlayerId = playerId,
                Type = (int)AdminActionType.Ban,
                Text = "Unclaimed ban",
                Created = DateTime.UtcNow
            });

            // Claimed ban
            ctx.UserProfiles.Add(new UserProfile
            {
                UserProfileId = adminProfileId,
                DisplayName = "TestAdmin"
            });
            ctx.AdminActions.Add(new AdminAction
            {
                AdminActionId = Guid.NewGuid(),
                PlayerId = playerId,
                UserProfileId = adminProfileId,
                Type = (int)AdminActionType.Ban,
                Text = "Claimed ban",
                Created = DateTime.UtcNow
            });

            // Open report
            ctx.Reports.Add(new Report
            {
                ReportId = Guid.NewGuid(),
                PlayerId = playerId,
                GameServerId = serverId,
                GameType = (int)GameType.CallOfDuty4,
                Comments = "Test report",
                Timestamp = DateTime.UtcNow,
                Closed = false
            });

            ctx.SaveChanges();
        });

        var response = await _client.GetAsync("/v1.0/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiResponse<DashboardSummaryDto>>(content);

        Assert.NotNull(result?.Data);
        Assert.True(result.Data.TotalServers >= 1);
        Assert.True(result.Data.UnclaimedBanCount >= 1);
        Assert.True(result.Data.OpenReportCount >= 1);
        Assert.True(result.Data.RecentActions24h.Bans >= 2);
    }

    [Fact]
    public async Task GetAdminLeaderboard_ReturnsOk()
    {
        var response = await _client.GetAsync("/v1.0/dashboard/admin-leaderboard?days=30");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiResponse<CollectionModel<AdminLeaderboardEntryDto>>>(content);

        Assert.NotNull(result?.Data);
        Assert.NotNull(result.Data.Items);
    }

    [Fact]
    public async Task GetModerationTrend_ReturnsOk()
    {
        var response = await _client.GetAsync("/v1.0/dashboard/moderation-trend?days=7");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiResponse<CollectionModel<ModerationTrendDataPointDto>>>(content);

        Assert.NotNull(result?.Data);
        Assert.NotNull(result.Data.Items);
    }

    [Fact]
    public async Task GetServerUtilization_ReturnsOk()
    {
        var response = await _client.GetAsync("/v1.0/dashboard/server-utilization");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiResponse<ServerUtilizationCollectionDto>>(content);

        Assert.NotNull(result?.Data);
        Assert.NotNull(result.Data.Servers);
    }

    [Fact]
    public async Task GetAdminLeaderboard_ReturnsAdminsOrderedByTotal()
    {
        var playerId = Guid.NewGuid();
        var admin1Id = Guid.NewGuid();
        var admin2Id = Guid.NewGuid();

        _factory.SeedDatabase(ctx =>
        {
            ctx.Players.Add(new Player
            {
                PlayerId = playerId,
                GameType = (int)GameType.CallOfDuty4,
                Username = "LeaderboardTestPlayer",
                Guid = $"lb-{Guid.NewGuid()}",
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            });

            ctx.UserProfiles.Add(new UserProfile { UserProfileId = admin1Id, DisplayName = "ActiveAdmin" });
            ctx.UserProfiles.Add(new UserProfile { UserProfileId = admin2Id, DisplayName = "CasualAdmin" });

            // Admin1: 3 actions
            for (var i = 0; i < 3; i++)
            {
                ctx.AdminActions.Add(new AdminAction
                {
                    AdminActionId = Guid.NewGuid(),
                    PlayerId = playerId,
                    UserProfileId = admin1Id,
                    Type = (int)AdminActionType.Kick,
                    Text = $"Kick {i}",
                    Created = DateTime.UtcNow
                });
            }

            // Admin2: 1 action
            ctx.AdminActions.Add(new AdminAction
            {
                AdminActionId = Guid.NewGuid(),
                PlayerId = playerId,
                UserProfileId = admin2Id,
                Type = (int)AdminActionType.Warning,
                Text = "Warning",
                Created = DateTime.UtcNow
            });

            ctx.SaveChanges();
        });

        var response = await _client.GetAsync("/v1.0/dashboard/admin-leaderboard?days=30");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiResponse<CollectionModel<AdminLeaderboardEntryDto>>>(content);

        Assert.NotNull(result?.Data?.Items);
        var items = result.Data.Items.ToList();
        Assert.True(items.Count >= 2);

        // Verify ordering: first entry should have highest total
        if (items.Count >= 2)
        {
            Assert.True(items[0].Total >= items[1].Total);
        }
    }
}
