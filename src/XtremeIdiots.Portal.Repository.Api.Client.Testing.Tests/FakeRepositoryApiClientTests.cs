using XtremeIdiots.Portal.Repository.Api.Client.Testing;
using XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Api.Client.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Tests;

[Trait("Category", "Unit")]
public class FakeRepositoryApiClientTests
{
    [Fact]
    public void AllVersionedApiProperties_ReturnNonNull()
    {
        var client = new FakeRepositoryApiClient();

        Assert.NotNull(client.AdminActions);
        Assert.NotNull(client.BanFileMonitors);
        Assert.NotNull(client.ChatMessages);
        Assert.NotNull(client.DataMaintenance);
        Assert.NotNull(client.Demos);
        Assert.NotNull(client.GameServers);
        Assert.NotNull(client.GameServersEvents);
        Assert.NotNull(client.GameServersStats);
        Assert.NotNull(client.GameTrackerBanner);
        Assert.NotNull(client.Maps);
        Assert.NotNull(client.ConnectedPlayers);
        Assert.NotNull(client.Screenshots);

        Assert.NotNull(client.PlayerAnalytics);
        Assert.NotNull(client.Players);
        Assert.NotNull(client.RecentPlayers);
        Assert.NotNull(client.Reports);
        Assert.NotNull(client.UserProfiles);
        Assert.NotNull(client.Tags);
        Assert.NotNull(client.MapRotations);
        Assert.NotNull(client.Dashboard);
        Assert.NotNull(client.ApiHealth);
        Assert.NotNull(client.ApiInfo);
        Assert.NotNull(client.ConnectedPlayers);
        Assert.NotNull(client.GlobalAnalytics);
        Assert.NotNull(client.GameAnalytics);
        Assert.NotNull(client.ServerAnalytics);
        Assert.NotNull(client.DashboardAnalytics);
        Assert.NotNull(client.MapAnalytics);
        Assert.NotNull(client.PlayerAnalyticsV2);
    }

    [Fact]
    public void ImplementsIRepositoryApiClient()
    {
        IRepositoryApiClient client = new FakeRepositoryApiClient();

        Assert.NotNull(client.AdminActions);
        Assert.NotNull(client.Players);
        Assert.NotNull(client.ApiHealth);
        Assert.NotNull(client.ApiInfo);
        Assert.NotNull(client.Tags);
        Assert.NotNull(client.Screenshots);
        Assert.NotNull(client.MapRotations);
        Assert.NotNull(client.Dashboard);
        Assert.NotNull(client.GlobalAnalytics);
        Assert.NotNull(client.GameAnalytics);
        Assert.NotNull(client.ServerAnalytics);
        Assert.NotNull(client.DashboardAnalytics);
        Assert.NotNull(client.MapAnalytics);
        Assert.NotNull(client.PlayerAnalyticsV2);
    }

    [Fact]
    public void Players_V1_ReturnsFakePlayersApiInstance()
    {
        var client = new FakeRepositoryApiClient();

        Assert.Same(client.PlayersApi, client.Players.V1);
    }

    [Fact]
    public async Task ApiHealth_CheckHealth_ReturnsOkByDefault()
    {
        var client = new FakeRepositoryApiClient();

        var result = await client.ApiHealth.V1.CheckHealth();

        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public void GameServers_V1_ReturnsFakeGameServersApiInstance()
    {
        var client = new FakeRepositoryApiClient();

        Assert.Same(client.GameServersApi, client.GameServers.V1);
    }

    [Fact]
    public async Task Reset_ClearsAllFakeState()
    {
        var client = new FakeRepositoryApiClient();
        var player = RepositoryDtoFactory.CreatePlayer();
        client.PlayersApi.AddPlayer(player);
        var gameServer = RepositoryDtoFactory.CreateGameServer();
        client.GameServersApi.AddGameServer(gameServer);

        client.Reset();

        var playerResult = await client.Players.V1.GetPlayer(player.PlayerId, default);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, playerResult.StatusCode);

        var gsResult = await client.GameServers.V1.GetGameServer(gameServer.GameServerId);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, gsResult.StatusCode);
    }

    [Fact]
    public async Task CanConfigureAndRetrieveResponses_ThroughClientHierarchy()
    {
        var client = new FakeRepositoryApiClient();
        var player = RepositoryDtoFactory.CreatePlayer(username: "HierarchyTest");
        client.PlayersApi.AddPlayer(player);

        var result = await client.Players.V1.GetPlayer(player.PlayerId, default);

        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("HierarchyTest", result.Result!.Data!.Username);
    }

    [Fact]
    public async Task ConnectedPlayers_V1_CanCreateAndListLinks()
    {
        var client = new FakeRepositoryApiClient();
        var userProfileId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        var createResult = await client.ConnectedPlayers.V1.CreateConnectedPlayerLink(new CreateConnectedPlayerLinkDto
        {
            PlayerId = playerId,
            UserProfileId = userProfileId
        });

        Assert.Equal(System.Net.HttpStatusCode.Created, createResult.StatusCode);

        var listResult = await client.ConnectedPlayers.V1.GetConnectedPlayersByUserProfile(userProfileId, 0, 20);
        Assert.Equal(System.Net.HttpStatusCode.OK, listResult.StatusCode);
        Assert.Single(listResult.Result!.Data!.Items!);
    }

    [Fact]
    public async Task GlobalAnalytics_V1_GetTimeseriesComparisonOverload_ReturnsConfiguredResponse()
    {
        var client = new FakeRepositoryApiClient();
        var timeseries = RepositoryDtoFactory.CreateGlobalTimeseries();
        client.GlobalAnalyticsApi.SetTimeseries(timeseries);

        var result = await client.GlobalAnalytics.V1.GetTimeseries(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            AnalyticsBucket.OneDay,
            AnalyticsCompareMode.PreviousPeriod,
            2,
            AnalyticsAlignMode.Week,
            "Europe/London",
            true);

        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Same(timeseries, result.Result!.Data);
    }

    [Fact]
    public async Task GameAnalytics_V1_GetTimeseriesComparisonOverload_ReturnsConfiguredResponse()
    {
        var client = new FakeRepositoryApiClient();
        var timeseries = RepositoryDtoFactory.CreateGameTimeseries();
        client.GameAnalyticsApi.SetTimeseries(timeseries);

        var result = await client.GameAnalytics.V1.GetTimeseries(
            GameType.CallOfDuty4,
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            AnalyticsBucket.OneDay,
            AnalyticsCompareMode.RollingPeriods,
            3,
            AnalyticsAlignMode.Month,
            "UTC",
            false);

        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Same(timeseries, result.Result!.Data);
    }

    [Fact]
    public async Task ServerAnalytics_V1_GetTimeseriesComparisonOverload_ReturnsConfiguredResponse()
    {
        var client = new FakeRepositoryApiClient();
        var timeseries = RepositoryDtoFactory.CreateServerTimeseries();
        client.ServerAnalyticsApi.SetTimeseries(timeseries);

        var result = await client.ServerAnalytics.V1.GetTimeseries(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            AnalyticsBucket.OneDay,
            AnalyticsCompareMode.PreviousPeriod,
            1,
            AnalyticsAlignMode.None,
            "UTC",
            false);

        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Same(timeseries, result.Result!.Data);
    }

    [Fact]
    public async Task PlayerAnalyticsV2_V1_GetPlayerTimeseriesComparisonOverload_ReturnsConfiguredResponse()
    {
        var client = new FakeRepositoryApiClient();
        var trends = RepositoryDtoFactory.CreatePlayerTrends();
        client.PlayerAnalyticsV2Api.SetPlayerTimeseries(trends);

        var result = await client.PlayerAnalyticsV2.V1.GetPlayerTimeseries(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            AnalyticsBucket.OneDay,
            AnalyticsCompareMode.RollingPeriods,
            4,
            AnalyticsAlignMode.Week,
            "Europe/London",
            true);

        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Same(trends, result.Result!.Data);
    }
}
