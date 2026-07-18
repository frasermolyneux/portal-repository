using XtremeIdiots.Portal.Repository.Api.Client.Testing;
using XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Api.Client.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;

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
    public async Task AdminActions_V1_EnsureAutomatedAction_ReusesEqualRuleAction()
    {
        var client = new FakeRepositoryApiClient();
        var playerId = Guid.NewGuid();
        var request = new EnsureAutomatedActionDto(
            playerId,
            AdminActionType.Observation,
            "VPN Protection: vpn",
            AutomationFeature.VpnProtection,
            "vpn");

        var first = await client.AdminActions.V1.EnsureAutomatedAction(request);
        var second = await client.AdminActions.V1.EnsureAutomatedAction(request);

        Assert.True(first.Result!.Data!.Created);
        Assert.False(second.Result!.Data!.Created);
        Assert.Equal(first.Result.Data.AdminAction.AdminActionId, second.Result.Data.AdminAction.AdminActionId);
    }

    [Fact]
    public async Task AdminActions_V1_EnsureAutomatedAction_LiftedBanStartsNewCycle()
    {
        var client = new FakeRepositoryApiClient();
        var request = new EnsureAutomatedActionDto(
            Guid.NewGuid(),
            AdminActionType.Ban,
            "VPN Protection: vpn",
            AutomationFeature.VpnProtection,
            "vpn");

        var first = await client.AdminActions.V1.EnsureAutomatedAction(request);
        await client.AdminActions.V1.UpdateAdminAction(new EditAdminActionDto(first.Result!.Data!.AdminAction.AdminActionId)
        {
            Expires = DateTime.UtcNow
        });

        var second = await client.AdminActions.V1.EnsureAutomatedAction(request);

        Assert.True(second.Result!.Data!.Created);
        Assert.NotEqual(first.Result.Data.AdminAction.AdminActionId, second.Result.Data.AdminAction.AdminActionId);
    }

    [Fact]
    public async Task AdminActions_V1_EnsureAutomatedAction_StrongerBanExpiresLowerBan()
    {
        var client = new FakeRepositoryApiClient();
        var playerId = Guid.NewGuid();
        var temporaryBan = new EnsureAutomatedActionDto(
            playerId,
            AdminActionType.TempBan,
            "VPN Protection: vpn",
            AutomationFeature.VpnProtection,
            "vpn")
        {
            Expires = DateTime.UtcNow.AddDays(1)
        };
        var permanentBan = new EnsureAutomatedActionDto(
            playerId,
            AdminActionType.Ban,
            "VPN Protection: vpn",
            AutomationFeature.VpnProtection,
            "vpn");

        var first = await client.AdminActions.V1.EnsureAutomatedAction(temporaryBan);
        var second = await client.AdminActions.V1.EnsureAutomatedAction(permanentBan);

        var actions = await client.AdminActions.V1.GetAdminActions(null, playerId, null, null, 0, 20, null);
        var expiredTemporaryBan = Assert.Single(actions.Result!.Data!.Items!, action => action.AdminActionId == first.Result!.Data!.AdminAction.AdminActionId);

        Assert.True(second.Result!.Data!.Created);
        Assert.True(expiredTemporaryBan.Expires <= DateTime.UtcNow);
    }

    [Fact]
    public async Task AdminActions_V1_RconBanImportPromotionRetainsForumTopic()
    {
        var client = new FakeRepositoryApiClient();
        var playerId = Guid.NewGuid();
        const string lifecycleId = "cod4x:server:canonical-puid";
        var temporary = await client.AdminActions.V1.EnsureAutomatedAction(new EnsureAutomatedActionDto(
            playerId,
            AdminActionType.TempBan,
            "VPN Protection: temporary rule",
            AutomationFeature.RconBanImport,
            lifecycleId)
        {
            Expires = DateTime.UtcNow.AddMinutes(30)
        });
        var actionId = temporary.Result!.Data!.AdminAction.AdminActionId;
        var claim = await client.AdminActions.V1.ClaimForumTopicPublication(actionId);
        await client.AdminActions.V1.CompleteForumTopicPublication(
            actionId,
            new CompleteForumTopicPublicationDto(Assert.IsType<Guid>(claim.Result?.Data?.ClaimId), 12345));

        var permanent = await client.AdminActions.V1.EnsureAutomatedAction(new EnsureAutomatedActionDto(
            playerId,
            AdminActionType.Ban,
            "VPN Protection: permanent rule",
            AutomationFeature.RconBanImport,
            lifecycleId));

        Assert.False(permanent.Result?.Data?.Created);
        Assert.Equal(actionId, permanent.Result?.Data?.AdminAction.AdminActionId);
        Assert.Equal(AdminActionType.Ban, permanent.Result?.Data?.AdminAction.Type);
        Assert.Equal(12345, permanent.Result?.Data?.AdminAction.ForumTopicId);
    }

    [Fact]
    public async Task AdminActions_V1_RconBanImportPromotionRetainsPublicationClaim()
    {
        var client = new FakeRepositoryApiClient();
        var playerId = Guid.NewGuid();
        const string lifecycleId = "cod4x:server:canonical-puid";
        var temporary = await client.AdminActions.V1.EnsureAutomatedAction(new EnsureAutomatedActionDto(
            playerId,
            AdminActionType.TempBan,
            "VPN Protection: temporary rule",
            AutomationFeature.RconBanImport,
            lifecycleId)
        {
            Expires = DateTime.UtcNow.AddMinutes(30)
        });
        var actionId = temporary.Result!.Data!.AdminAction.AdminActionId;
        var claim = await client.AdminActions.V1.ClaimForumTopicPublication(actionId);
        var claimId = Assert.IsType<Guid>(claim.Result?.Data?.ClaimId);

        var permanent = await client.AdminActions.V1.EnsureAutomatedAction(new EnsureAutomatedActionDto(
            playerId,
            AdminActionType.Ban,
            "VPN Protection: permanent rule",
            AutomationFeature.RconBanImport,
            lifecycleId));
        var complete = await client.AdminActions.V1.CompleteForumTopicPublication(
            actionId,
            new CompleteForumTopicPublicationDto(claimId, 12345));

        Assert.False(permanent.Result?.Data?.Created);
        Assert.Equal(actionId, permanent.Result?.Data?.AdminAction.AdminActionId);
        Assert.True(complete.IsSuccess);
        Assert.Equal(12345, permanent.Result?.Data?.AdminAction.ForumTopicId);
    }

    [Fact]
    public async Task AdminActions_V1_ForumTopicPublicationClaim_PreventsRepostAfterAmbiguousFailure()
    {
        var client = new FakeRepositoryApiClient();
        var action = await client.AdminActions.V1.EnsureAutomatedAction(new EnsureAutomatedActionDto(
            Guid.NewGuid(),
            AdminActionType.Ban,
            "Imported from server RCON dumpbanlist",
            AutomationFeature.RconBanImport,
            "cod4x:server:canonical-puid:permanent"));
        var adminActionId = action.Result!.Data!.AdminAction.AdminActionId;

        var firstClaim = await client.AdminActions.V1.ClaimForumTopicPublication(adminActionId);
        var repeatedClaim = await client.AdminActions.V1.ClaimForumTopicPublication(adminActionId);

        Assert.NotNull(firstClaim.Result?.Data?.ClaimId);
        Assert.False(firstClaim.Result?.Data?.RequiresManualRecovery);
        Assert.Null(repeatedClaim.Result?.Data?.ClaimId);
        Assert.True(repeatedClaim.Result?.Data?.RequiresManualRecovery);
    }

    [Fact]
    public async Task AdminActions_V1_CompleteForumTopicPublication_LinksClaimedTopic()
    {
        var client = new FakeRepositoryApiClient();
        var action = await client.AdminActions.V1.EnsureAutomatedAction(new EnsureAutomatedActionDto(
            Guid.NewGuid(),
            AdminActionType.Ban,
            "Imported from server RCON dumpbanlist",
            AutomationFeature.RconBanImport,
            "cod4x:server:canonical-puid:permanent"));
        var adminActionId = action.Result!.Data!.AdminAction.AdminActionId;
        var claim = await client.AdminActions.V1.ClaimForumTopicPublication(adminActionId);
        var claimId = Assert.IsType<Guid>(claim.Result?.Data?.ClaimId);

        var complete = await client.AdminActions.V1.CompleteForumTopicPublication(
            adminActionId,
            new CompleteForumTopicPublicationDto(claimId, 12345));
        var afterCompletion = await client.AdminActions.V1.ClaimForumTopicPublication(adminActionId);

        Assert.True(complete.IsSuccess);
        Assert.Equal(12345, afterCompletion.Result?.Data?.ForumTopicId);
        Assert.Null(afterCompletion.Result?.Data?.ClaimId);
        Assert.False(afterCompletion.Result?.Data?.RequiresManualRecovery);
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
