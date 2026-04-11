namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    /// <summary>
    /// Unified repository API client providing access to all APIs with version selectors.
    /// Use like: client.Players.V1
    /// </summary>
    public class RepositoryApiClient : IRepositoryApiClient
    {
        public RepositoryApiClient(
            IVersionedAdminActionsApi adminActions,
            IVersionedBanFileMonitorsApi banFileMonitors,
            IVersionedChatMessagesApi chatMessages,
            IVersionedDataMaintenanceApi dataMaintenance,
            IVersionedDemosApi demos,
            IVersionedGameServersApi gameServers,
            IVersionedGameServersEventsApi gameServersEvents,
            IVersionedGameServersStatsApi gameServersStats,
            IVersionedGameTrackerBannerApi gameTrackerBanner,
            IVersionedLivePlayersApi livePlayers,
            IVersionedMapsApi maps,
            IVersionedMapPacksApi mapPacks,
            IVersionedPlayerAnalyticsApi playerAnalytics,
            IVersionedPlayersApi players,
            IVersionedRecentPlayersApi recentPlayers,
            IVersionedReportsApi reports,
            IVersionedUserProfileApi userProfiles,
            IVersionedTagsApi tags,
            IVersionedApiHealthApi apiHealth,
            IVersionedApiInfoApi apiInfo,
            IVersionedNotificationTypesApi notificationTypes,
            IVersionedNotificationPreferencesApi notificationPreferences,
            IVersionedNotificationsApi notifications,
            IVersionedMapRotationsApi mapRotations,
            IVersionedDashboardApi dashboard,
            IVersionedGlobalConfigurationsApi globalConfigurations,
            IVersionedGameServerConfigurationsApi gameServerConfigurations,
            IVersionedLiveStatusApi liveStatus)
        {
            AdminActions = adminActions;
            BanFileMonitors = banFileMonitors;
            ChatMessages = chatMessages;
            DataMaintenance = dataMaintenance;
            Demos = demos;
            GameServers = gameServers;
            GameServersEvents = gameServersEvents;
            GameServersStats = gameServersStats;
            GameTrackerBanner = gameTrackerBanner;
            LivePlayers = livePlayers;
            Maps = maps;
            MapPacks = mapPacks;
            PlayerAnalytics = playerAnalytics;
            Players = players;
            RecentPlayers = recentPlayers;
            Reports = reports;
            UserProfiles = userProfiles;
            Tags = tags;
            ApiHealth = apiHealth;
            ApiInfo = apiInfo;
            NotificationTypes = notificationTypes;
            NotificationPreferences = notificationPreferences;
            Notifications = notifications;
            MapRotations = mapRotations;
            Dashboard = dashboard;
            GlobalConfigurations = globalConfigurations;
            GameServerConfigurations = gameServerConfigurations;
            LiveStatus = liveStatus;
        }

        public IVersionedAdminActionsApi AdminActions { get; }
        public IVersionedBanFileMonitorsApi BanFileMonitors { get; }
        public IVersionedChatMessagesApi ChatMessages { get; }
        public IVersionedDataMaintenanceApi DataMaintenance { get; }
        public IVersionedDemosApi Demos { get; }
        public IVersionedGameServersApi GameServers { get; }
        public IVersionedGameServersEventsApi GameServersEvents { get; }
        public IVersionedGameServersStatsApi GameServersStats { get; }
        public IVersionedGameTrackerBannerApi GameTrackerBanner { get; }
        public IVersionedLivePlayersApi LivePlayers { get; }
        public IVersionedMapsApi Maps { get; }
        public IVersionedMapPacksApi MapPacks { get; }
        public IVersionedPlayerAnalyticsApi PlayerAnalytics { get; }
        public IVersionedPlayersApi Players { get; }
        public IVersionedRecentPlayersApi RecentPlayers { get; }
        public IVersionedReportsApi Reports { get; }
        public IVersionedUserProfileApi UserProfiles { get; }
        public IVersionedTagsApi Tags { get; }
        public IVersionedApiHealthApi ApiHealth { get; }
        public IVersionedApiInfoApi ApiInfo { get; }
        public IVersionedNotificationTypesApi NotificationTypes { get; }
        public IVersionedNotificationPreferencesApi NotificationPreferences { get; }
        public IVersionedNotificationsApi Notifications { get; }
        public IVersionedMapRotationsApi MapRotations { get; }
        public IVersionedDashboardApi Dashboard { get; }
        public IVersionedGlobalConfigurationsApi GlobalConfigurations { get; }
        public IVersionedGameServerConfigurationsApi GameServerConfigurations { get; }
        public IVersionedLiveStatusApi LiveStatus { get; }
    }
}
