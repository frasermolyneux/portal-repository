using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;

namespace XtremeIdiots.Portal.RepositoryApiClient.V1
{
    // Implementation classes for version selectors
    public class VersionedAdminActionsApi : IVersionedAdminActionsApi
    {
        public VersionedAdminActionsApi(IAdminActionsApi v1Api)
        {
            V1 = v1Api;
        }

        public IAdminActionsApi V1 { get; }
    }

    public class VersionedBanFileMonitorsApi : IVersionedBanFileMonitorsApi
    {
        public VersionedBanFileMonitorsApi(IBanFileMonitorsApi v1Api)
        {
            V1 = v1Api;
        }

        public IBanFileMonitorsApi V1 { get; }
    }

    public class VersionedChatMessagesApi : IVersionedChatMessagesApi
    {
        public VersionedChatMessagesApi(IChatMessagesApi v1Api)
        {
            V1 = v1Api;
        }

        public IChatMessagesApi V1 { get; }
    }

    public class VersionedDataMaintenanceApi : IVersionedDataMaintenanceApi
    {
        public VersionedDataMaintenanceApi(IDataMaintenanceApi v1Api)
        {
            V1 = v1Api;
        }

        public IDataMaintenanceApi V1 { get; }
    }

    public class VersionedDemosApi : IVersionedDemosApi
    {
        public VersionedDemosApi(IDemosApi v1Api)
        {
            V1 = v1Api;
        }

        public IDemosApi V1 { get; }
    }

    public class VersionedGameServersApi : IVersionedGameServersApi
    {
        public VersionedGameServersApi(IGameServersApi v1Api)
        {
            V1 = v1Api;
        }

        public IGameServersApi V1 { get; }
    }

    public class VersionedGameServersEventsApi : IVersionedGameServersEventsApi
    {
        public VersionedGameServersEventsApi(IGameServersEventsApi v1Api)
        {
            V1 = v1Api;
        }

        public IGameServersEventsApi V1 { get; }
    }

    public class VersionedGameServersStatsApi : IVersionedGameServersStatsApi
    {
        public VersionedGameServersStatsApi(IGameServersStatsApi v1Api)
        {
            V1 = v1Api;
        }

        public IGameServersStatsApi V1 { get; }
    }

    public class VersionedGameTrackerBannerApi : IVersionedGameTrackerBannerApi
    {
        public VersionedGameTrackerBannerApi(IGameTrackerBannerApi v1Api)
        {
            V1 = v1Api;
        }

        public IGameTrackerBannerApi V1 { get; }
    }

    public class VersionedLivePlayersApi : IVersionedLivePlayersApi
    {
        public VersionedLivePlayersApi(ILivePlayersApi v1Api)
        {
            V1 = v1Api;
        }

        public ILivePlayersApi V1 { get; }
    }

    public class VersionedMapsApi : IVersionedMapsApi
    {
        public VersionedMapsApi(IMapsApi v1Api)
        {
            V1 = v1Api;
        }

        public IMapsApi V1 { get; }
    }

    public class VersionedMapPacksApi : IVersionedMapPacksApi
    {
        public VersionedMapPacksApi(IMapPacksApi v1Api)
        {
            V1 = v1Api;
        }

        public IMapPacksApi V1 { get; }
    }

    public class VersionedPlayerAnalyticsApi : IVersionedPlayerAnalyticsApi
    {
        public VersionedPlayerAnalyticsApi(IPlayerAnalyticsApi v1Api)
        {
            V1 = v1Api;
        }

        public IPlayerAnalyticsApi V1 { get; }
    }

    public class VersionedPlayersApi : IVersionedPlayersApi
    {
        public VersionedPlayersApi(IPlayersApi v1Api)
        {
            V1 = v1Api;
        }

        public IPlayersApi V1 { get; }
    }

    public class VersionedRecentPlayersApi : IVersionedRecentPlayersApi
    {
        public VersionedRecentPlayersApi(IRecentPlayersApi v1Api)
        {
            V1 = v1Api;
        }

        public IRecentPlayersApi V1 { get; }
    }

    public class VersionedReportsApi : IVersionedReportsApi
    {
        public VersionedReportsApi(IReportsApi v1Api)
        {
            V1 = v1Api;
        }

        public IReportsApi V1 { get; }
    }

    public class VersionedTagsApi : IVersionedTagsApi
    {
        public VersionedTagsApi(ITagsApi v1Api)
        {
            V1 = v1Api;
        }

        public ITagsApi V1 { get; }
    }

    public class VersionedUserProfileApi : IVersionedUserProfileApi
    {
        public VersionedUserProfileApi(IUserProfileApi v1Api)
        {
            V1 = v1Api;
        }

        public IUserProfileApi V1 { get; }
    }

    public class VersionedRootApi : IVersionedRootApi
    {
        public VersionedRootApi(IRootApi v1Api, XtremeIdiots.Portal.RepositoryApiClient.V1_1.RootApi v1_1Api)
        {
            V1 = v1Api;
            V1_1 = v1_1Api;
        }

        public IRootApi V1 { get; }
        public IRootApi V1_1 { get; }
    }
}
