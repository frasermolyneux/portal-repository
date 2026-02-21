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
            IVersionedApiInfoApi apiInfo)
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
    }
}
