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
            IVersionedCentralBanFileStatusApi centralBanFileStatus,
            IVersionedChatMessagesApi chatMessages,
            IVersionedDataMaintenanceApi dataMaintenance,
            IVersionedDemosApi demos,
            IVersionedGameServersApi gameServers,
            IVersionedGameServersEventsApi gameServersEvents,
            IVersionedGameServersStatsApi gameServersStats,
            IVersionedGameTrackerBannerApi gameTrackerBanner,
            IVersionedMapsApi maps,
            IVersionedConnectedPlayersApi connectedPlayers,

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
            IVersionedLiveStatusApi liveStatus,
            IVersionedGlobalAnalyticsApi globalAnalytics,
            IVersionedGameAnalyticsApi gameAnalytics,
            IVersionedServerAnalyticsApi serverAnalytics,
            IVersionedDashboardAnalyticsApi dashboardAnalytics,
            IVersionedMapAnalyticsApi mapAnalytics,
            IVersionedPlayerAnalyticsV2Api playerAnalyticsV2)
        {
            AdminActions = adminActions;
            BanFileMonitors = banFileMonitors;
            CentralBanFileStatus = centralBanFileStatus;
            ChatMessages = chatMessages;
            DataMaintenance = dataMaintenance;
            Demos = demos;
            GameServers = gameServers;
            GameServersEvents = gameServersEvents;
            GameServersStats = gameServersStats;
            GameTrackerBanner = gameTrackerBanner;
            Maps = maps;
            ConnectedPlayers = connectedPlayers;

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
            GlobalAnalytics = globalAnalytics;
            GameAnalytics = gameAnalytics;
            ServerAnalytics = serverAnalytics;
            DashboardAnalytics = dashboardAnalytics;
            MapAnalytics = mapAnalytics;
            PlayerAnalyticsV2 = playerAnalyticsV2;
        }

        public IVersionedAdminActionsApi AdminActions { get; }
        public IVersionedBanFileMonitorsApi BanFileMonitors { get; }
        public IVersionedCentralBanFileStatusApi CentralBanFileStatus { get; }
        public IVersionedChatMessagesApi ChatMessages { get; }
        public IVersionedDataMaintenanceApi DataMaintenance { get; }
        public IVersionedDemosApi Demos { get; }
        public IVersionedGameServersApi GameServers { get; }
        public IVersionedGameServersEventsApi GameServersEvents { get; }
        public IVersionedGameServersStatsApi GameServersStats { get; }
        public IVersionedGameTrackerBannerApi GameTrackerBanner { get; }
        public IVersionedMapsApi Maps { get; }
        public IVersionedConnectedPlayersApi ConnectedPlayers { get; }

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
        public IVersionedGlobalAnalyticsApi GlobalAnalytics { get; }
        public IVersionedGameAnalyticsApi GameAnalytics { get; }
        public IVersionedServerAnalyticsApi ServerAnalytics { get; }
        public IVersionedDashboardAnalyticsApi DashboardAnalytics { get; }
        public IVersionedMapAnalyticsApi MapAnalytics { get; }
        public IVersionedPlayerAnalyticsV2Api PlayerAnalyticsV2 { get; }
    }
}
