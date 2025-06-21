using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;

namespace XtremeIdiots.Portal.RepositoryApiClient
{
    public class RepositoryApiClient : IRepositoryApiClient
    {
        public RepositoryApiClient(
            IAdminActionsApi adminActionsApiClient,
            IBanFileMonitorsApi banFileMonitorsApiClient,
            IChatMessagesApi chatMessagesApiClient,
            IDataMaintenanceApi dataMaintenanceApiClient,
            IDemosApi demosApiClient,
            IGameServersApi gameServersApiClient,
            IGameServersEventsApi gameServersEventsApiClient,
            IGameServersStatsApi gameServersStatsApiClient,
            IGameTrackerBannerApi gameTrackerBannerApi,
            ILivePlayersApi livePlayersApiClient,
            IMapsApi mapsApiClient,
            IMapPacksApi mapPacksApiClient,
            IPlayerAnalyticsApi playerAnalyticsApiClient,
            IPlayersApi playersApiClient,
            IRecentPlayersApi recentPlayersApiClient,
            IReportsApi reportsApiClient,
            IRootApi rootApiClient,
            IUserProfileApi userProfileApiClient,
            ITagsApi tagsApiClient)
        {
            AdminActions = adminActionsApiClient;
            BanFileMonitors = banFileMonitorsApiClient;
            ChatMessages = chatMessagesApiClient;
            DataMaintenance = dataMaintenanceApiClient;
            Demos = demosApiClient;
            GameServers = gameServersApiClient;
            GameServersEvents = gameServersEventsApiClient;
            GameServersStats = gameServersStatsApiClient;
            GameTrackerBanner = gameTrackerBannerApi;
            LivePlayers = livePlayersApiClient;
            Maps = mapsApiClient;
            MapPacks = mapPacksApiClient;
            PlayerAnalytics = playerAnalyticsApiClient;
            Players = playersApiClient;
            RecentPlayers = recentPlayersApiClient;
            Reports = reportsApiClient;
            Root = rootApiClient;
            UserProfiles = userProfileApiClient;
            Tags = tagsApiClient; // initialize new property
        }

        public IAdminActionsApi AdminActions { get; }
        public IBanFileMonitorsApi BanFileMonitors { get; }
        public IChatMessagesApi ChatMessages { get; }
        public IDataMaintenanceApi DataMaintenance { get; }
        public IDemosApi Demos { get; }
        public IGameServersApi GameServers { get; }
        public IGameServersEventsApi GameServersEvents { get; }
        public IGameServersStatsApi GameServersStats { get; }
        public IGameTrackerBannerApi GameTrackerBanner { get; }
        public ILivePlayersApi LivePlayers { get; }
        public IMapsApi Maps { get; }
        public IMapPacksApi MapPacks { get; }
        public IPlayerAnalyticsApi PlayerAnalytics { get; }
        public IPlayersApi Players { get; }
        public IRecentPlayersApi RecentPlayers { get; }
        public IReportsApi Reports { get; }
        public IRootApi Root { get; }
        public IUserProfileApi UserProfiles { get; }
        public ITagsApi Tags { get; }
    }
}