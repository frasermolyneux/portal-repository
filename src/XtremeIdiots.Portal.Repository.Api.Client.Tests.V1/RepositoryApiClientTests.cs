using Moq;
using XtremeIdiots.Portal.Repository.Api.Client.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1
{
    public class RepositoryApiClientTests
    {
        private readonly Mock<IVersionedAdminActionsApi> _adminActions = new();
        private readonly Mock<IVersionedBanFileMonitorsApi> _banFileMonitors = new();
        private readonly Mock<IVersionedChatMessagesApi> _chatMessages = new();
        private readonly Mock<IVersionedDataMaintenanceApi> _dataMaintenance = new();
        private readonly Mock<IVersionedDemosApi> _demos = new();
        private readonly Mock<IVersionedGameServersApi> _gameServers = new();
        private readonly Mock<IVersionedGameServersEventsApi> _gameServersEvents = new();
        private readonly Mock<IVersionedGameServersStatsApi> _gameServersStats = new();
        private readonly Mock<IVersionedGameTrackerBannerApi> _gameTrackerBanner = new();
        private readonly Mock<IVersionedLivePlayersApi> _livePlayers = new();
        private readonly Mock<IVersionedMapsApi> _maps = new();
        private readonly Mock<IVersionedMapPacksApi> _mapPacks = new();
        private readonly Mock<IVersionedPlayerAnalyticsApi> _playerAnalytics = new();
        private readonly Mock<IVersionedPlayersApi> _players = new();
        private readonly Mock<IVersionedRecentPlayersApi> _recentPlayers = new();
        private readonly Mock<IVersionedReportsApi> _reports = new();
        private readonly Mock<IVersionedRootApi> _root = new();
        private readonly Mock<IVersionedUserProfileApi> _userProfiles = new();
        private readonly Mock<IVersionedTagsApi> _tags = new();

        private RepositoryApiClient CreateClient() => new(
            _adminActions.Object,
            _banFileMonitors.Object,
            _chatMessages.Object,
            _dataMaintenance.Object,
            _demos.Object,
            _gameServers.Object,
            _gameServersEvents.Object,
            _gameServersStats.Object,
            _gameTrackerBanner.Object,
            _livePlayers.Object,
            _maps.Object,
            _mapPacks.Object,
            _playerAnalytics.Object,
            _players.Object,
            _recentPlayers.Object,
            _reports.Object,
            _root.Object,
            _userProfiles.Object,
            _tags.Object);

        [Fact]
        public void Constructor_StoresAllProperties()
        {
            var client = CreateClient();

            Assert.Same(_adminActions.Object, client.AdminActions);
            Assert.Same(_banFileMonitors.Object, client.BanFileMonitors);
            Assert.Same(_chatMessages.Object, client.ChatMessages);
            Assert.Same(_dataMaintenance.Object, client.DataMaintenance);
            Assert.Same(_demos.Object, client.Demos);
            Assert.Same(_gameServers.Object, client.GameServers);
            Assert.Same(_gameServersEvents.Object, client.GameServersEvents);
            Assert.Same(_gameServersStats.Object, client.GameServersStats);
            Assert.Same(_gameTrackerBanner.Object, client.GameTrackerBanner);
            Assert.Same(_livePlayers.Object, client.LivePlayers);
            Assert.Same(_maps.Object, client.Maps);
            Assert.Same(_mapPacks.Object, client.MapPacks);
            Assert.Same(_playerAnalytics.Object, client.PlayerAnalytics);
            Assert.Same(_players.Object, client.Players);
            Assert.Same(_recentPlayers.Object, client.RecentPlayers);
            Assert.Same(_reports.Object, client.Reports);
            Assert.Same(_root.Object, client.Root);
            Assert.Same(_userProfiles.Object, client.UserProfiles);
            Assert.Same(_tags.Object, client.Tags);
        }

        [Fact]
        public void Constructor_NoPropertyIsNull()
        {
            var client = CreateClient();

            Assert.NotNull(client.AdminActions);
            Assert.NotNull(client.BanFileMonitors);
            Assert.NotNull(client.ChatMessages);
            Assert.NotNull(client.DataMaintenance);
            Assert.NotNull(client.Demos);
            Assert.NotNull(client.GameServers);
            Assert.NotNull(client.GameServersEvents);
            Assert.NotNull(client.GameServersStats);
            Assert.NotNull(client.GameTrackerBanner);
            Assert.NotNull(client.LivePlayers);
            Assert.NotNull(client.Maps);
            Assert.NotNull(client.MapPacks);
            Assert.NotNull(client.PlayerAnalytics);
            Assert.NotNull(client.Players);
            Assert.NotNull(client.RecentPlayers);
            Assert.NotNull(client.Reports);
            Assert.NotNull(client.Root);
            Assert.NotNull(client.UserProfiles);
            Assert.NotNull(client.Tags);
        }

        [Fact]
        public void Properties_ReturnCorrectTypes()
        {
            var client = CreateClient();

            Assert.IsAssignableFrom<IVersionedAdminActionsApi>(client.AdminActions);
            Assert.IsAssignableFrom<IVersionedBanFileMonitorsApi>(client.BanFileMonitors);
            Assert.IsAssignableFrom<IVersionedChatMessagesApi>(client.ChatMessages);
            Assert.IsAssignableFrom<IVersionedDataMaintenanceApi>(client.DataMaintenance);
            Assert.IsAssignableFrom<IVersionedDemosApi>(client.Demos);
            Assert.IsAssignableFrom<IVersionedGameServersApi>(client.GameServers);
            Assert.IsAssignableFrom<IVersionedGameServersEventsApi>(client.GameServersEvents);
            Assert.IsAssignableFrom<IVersionedGameServersStatsApi>(client.GameServersStats);
            Assert.IsAssignableFrom<IVersionedGameTrackerBannerApi>(client.GameTrackerBanner);
            Assert.IsAssignableFrom<IVersionedLivePlayersApi>(client.LivePlayers);
            Assert.IsAssignableFrom<IVersionedMapsApi>(client.Maps);
            Assert.IsAssignableFrom<IVersionedMapPacksApi>(client.MapPacks);
            Assert.IsAssignableFrom<IVersionedPlayerAnalyticsApi>(client.PlayerAnalytics);
            Assert.IsAssignableFrom<IVersionedPlayersApi>(client.Players);
            Assert.IsAssignableFrom<IVersionedRecentPlayersApi>(client.RecentPlayers);
            Assert.IsAssignableFrom<IVersionedReportsApi>(client.Reports);
            Assert.IsAssignableFrom<IVersionedRootApi>(client.Root);
            Assert.IsAssignableFrom<IVersionedUserProfileApi>(client.UserProfiles);
            Assert.IsAssignableFrom<IVersionedTagsApi>(client.Tags);
        }
    }
}
