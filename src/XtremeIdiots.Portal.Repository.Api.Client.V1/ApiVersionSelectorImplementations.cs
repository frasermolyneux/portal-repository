using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
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

    public class VersionedCentralBanFileStatusApi : IVersionedCentralBanFileStatusApi
    {
        public VersionedCentralBanFileStatusApi(ICentralBanFileStatusApi v1Api)
        {
            V1 = v1Api;
        }

        public ICentralBanFileStatusApi V1 { get; }
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

    public class VersionedMapsApi : IVersionedMapsApi
    {
        public VersionedMapsApi(IMapsApi v1Api)
        {
            V1 = v1Api;
        }

        public IMapsApi V1 { get; }
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

    public class VersionedApiHealthApi : IVersionedApiHealthApi
    {
        public VersionedApiHealthApi(IApiHealthApi v1Api)
        {
            V1 = v1Api;
        }

        public IApiHealthApi V1 { get; }
    }

    public class VersionedApiInfoApi : IVersionedApiInfoApi
    {
        public VersionedApiInfoApi(IApiInfoApi v1Api)
        {
            V1 = v1Api;
        }

        public IApiInfoApi V1 { get; }
    }

    public class VersionedNotificationTypesApi : IVersionedNotificationTypesApi
    {
        public VersionedNotificationTypesApi(INotificationTypesApi v1Api)
        {
            V1 = v1Api;
        }

        public INotificationTypesApi V1 { get; }
    }

    public class VersionedNotificationPreferencesApi : IVersionedNotificationPreferencesApi
    {
        public VersionedNotificationPreferencesApi(INotificationPreferencesApi v1Api)
        {
            V1 = v1Api;
        }

        public INotificationPreferencesApi V1 { get; }
    }

    public class VersionedNotificationsApi : IVersionedNotificationsApi
    {
        public VersionedNotificationsApi(INotificationsApi v1Api)
        {
            V1 = v1Api;
        }

        public INotificationsApi V1 { get; }
    }

    public class VersionedMapRotationsApi : IVersionedMapRotationsApi
    {
        public VersionedMapRotationsApi(IMapRotationsApi v1Api)
        {
            V1 = v1Api;
        }

        public IMapRotationsApi V1 { get; }
    }

    public class VersionedDashboardApi : IVersionedDashboardApi
    {
        public VersionedDashboardApi(IDashboardApi v1Api)
        {
            V1 = v1Api;
        }

        public IDashboardApi V1 { get; }
    }

    public class VersionedGlobalConfigurationsApi : IVersionedGlobalConfigurationsApi
    {
        public VersionedGlobalConfigurationsApi(IGlobalConfigurationsApi v1Api)
        {
            V1 = v1Api;
        }

        public IGlobalConfigurationsApi V1 { get; }
    }

    public class VersionedGameServerConfigurationsApi : IVersionedGameServerConfigurationsApi
    {
        public VersionedGameServerConfigurationsApi(IGameServerConfigurationsApi v1Api)
        {
            V1 = v1Api;
        }

        public IGameServerConfigurationsApi V1 { get; }
    }

    public class VersionedLiveStatusApi : IVersionedLiveStatusApi
    {
        public VersionedLiveStatusApi(ILiveStatusApi v1Api)
        {
            V1 = v1Api;
        }

        public ILiveStatusApi V1 { get; }
    }
}
