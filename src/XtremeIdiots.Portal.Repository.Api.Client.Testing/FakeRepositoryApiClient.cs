using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;
using XtremeIdiots.Portal.Repository.Api.Client.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing;

public class FakeVersionedAdminActionsApi : IVersionedAdminActionsApi
{
    public FakeVersionedAdminActionsApi(FakeAdminActionsApi v1) => V1 = v1;
    public IAdminActionsApi V1 { get; }
}

public class FakeVersionedBanFileMonitorsApi : IVersionedBanFileMonitorsApi
{
    public FakeVersionedBanFileMonitorsApi(FakeBanFileMonitorsApi v1) => V1 = v1;
    public IBanFileMonitorsApi V1 { get; }
}

public class FakeVersionedCentralBanFileStatusApi : IVersionedCentralBanFileStatusApi
{
    public FakeVersionedCentralBanFileStatusApi(FakeCentralBanFileStatusApi v1) => V1 = v1;
    public ICentralBanFileStatusApi V1 { get; }
}

public class FakeVersionedChatMessagesApi : IVersionedChatMessagesApi
{
    public FakeVersionedChatMessagesApi(FakeChatMessagesApi v1) => V1 = v1;
    public IChatMessagesApi V1 { get; }
}

public class FakeVersionedDataMaintenanceApi : IVersionedDataMaintenanceApi
{
    public FakeVersionedDataMaintenanceApi(FakeDataMaintenanceApi v1) => V1 = v1;
    public IDataMaintenanceApi V1 { get; }
}

public class FakeVersionedDemosApi : IVersionedDemosApi
{
    public FakeVersionedDemosApi(FakeDemosApi v1) => V1 = v1;
    public IDemosApi V1 { get; }
}

public class FakeVersionedGameServersApi : IVersionedGameServersApi
{
    public FakeVersionedGameServersApi(FakeGameServersApi v1) => V1 = v1;
    public IGameServersApi V1 { get; }
}

public class FakeVersionedGameServersEventsApi : IVersionedGameServersEventsApi
{
    public FakeVersionedGameServersEventsApi(FakeGameServersEventsApi v1) => V1 = v1;
    public IGameServersEventsApi V1 { get; }
}

public class FakeVersionedGameServersStatsApi : IVersionedGameServersStatsApi
{
    public FakeVersionedGameServersStatsApi(FakeGameServersStatsApi v1) => V1 = v1;
    public IGameServersStatsApi V1 { get; }
}

public class FakeVersionedGameTrackerBannerApi : IVersionedGameTrackerBannerApi
{
    public FakeVersionedGameTrackerBannerApi(FakeGameTrackerBannerApi v1) => V1 = v1;
    public IGameTrackerBannerApi V1 { get; }
}

public class FakeVersionedMapsApi : IVersionedMapsApi
{
    public FakeVersionedMapsApi(FakeMapsApi v1) => V1 = v1;
    public IMapsApi V1 { get; }
}

public class FakeVersionedConnectedPlayersApi : IVersionedConnectedPlayersApi
{
    public FakeVersionedConnectedPlayersApi(FakeConnectedPlayersApi v1) => V1 = v1;
    public IConnectedPlayersApi V1 { get; }
}

public class FakeVersionedScreenshotsApi : IVersionedScreenshotsApi
{
    public FakeVersionedScreenshotsApi(FakeScreenshotsApi v1) => V1 = v1;
    public IScreenshotsApi V1 { get; }
}


public class FakeVersionedPlayerAnalyticsApi : IVersionedPlayerAnalyticsApi
{
    public FakeVersionedPlayerAnalyticsApi(FakePlayerAnalyticsApi v1) => V1 = v1;
    public IPlayerAnalyticsApi V1 { get; }
}

public class FakeVersionedPlayersApi : IVersionedPlayersApi
{
    public FakeVersionedPlayersApi(FakePlayersApi v1) => V1 = v1;
    public IPlayersApi V1 { get; }
}

public class FakeVersionedRecentPlayersApi : IVersionedRecentPlayersApi
{
    public FakeVersionedRecentPlayersApi(FakeRecentPlayersApi v1) => V1 = v1;
    public IRecentPlayersApi V1 { get; }
}

public class FakeVersionedReportsApi : IVersionedReportsApi
{
    public FakeVersionedReportsApi(FakeReportsApi v1) => V1 = v1;
    public IReportsApi V1 { get; }
}

public class FakeVersionedTagsApi : IVersionedTagsApi
{
    public FakeVersionedTagsApi(FakeTagsApi v1) => V1 = v1;
    public ITagsApi V1 { get; }
}

public class FakeVersionedUserProfileApi : IVersionedUserProfileApi
{
    public FakeVersionedUserProfileApi(FakeUserProfileApi v1) => V1 = v1;
    public IUserProfileApi V1 { get; }
}

public class FakeVersionedApiHealthApi : IVersionedApiHealthApi
{
    public FakeVersionedApiHealthApi(FakeApiHealthApi v1) => V1 = v1;
    public IApiHealthApi V1 { get; }
}

public class FakeVersionedApiInfoApi : IVersionedApiInfoApi
{
    public FakeVersionedApiInfoApi(FakeApiInfoApi v1) => V1 = v1;
    public IApiInfoApi V1 { get; }
}

public class FakeVersionedNotificationTypesApi : IVersionedNotificationTypesApi
{
    public FakeVersionedNotificationTypesApi(FakeNotificationTypesApi v1) => V1 = v1;
    public INotificationTypesApi V1 { get; }
}

public class FakeVersionedNotificationPreferencesApi : IVersionedNotificationPreferencesApi
{
    public FakeVersionedNotificationPreferencesApi(FakeNotificationPreferencesApi v1) => V1 = v1;
    public INotificationPreferencesApi V1 { get; }
}

public class FakeVersionedNotificationsApi : IVersionedNotificationsApi
{
    public FakeVersionedNotificationsApi(FakeNotificationsApi v1) => V1 = v1;
    public INotificationsApi V1 { get; }
}

public class FakeVersionedMapRotationsApi : IVersionedMapRotationsApi
{
    public FakeVersionedMapRotationsApi(FakeMapRotationsApi v1) => V1 = v1;
    public IMapRotationsApi V1 { get; }
}

public class FakeVersionedDashboardApi : IVersionedDashboardApi
{
    public FakeVersionedDashboardApi(FakeDashboardApi v1) => V1 = v1;
    public IDashboardApi V1 { get; }
}

public class FakeVersionedGlobalConfigurationsApi : IVersionedGlobalConfigurationsApi
{
    public FakeVersionedGlobalConfigurationsApi(FakeGlobalConfigurationsApi v1) => V1 = v1;
    public IGlobalConfigurationsApi V1 { get; }
}

public class FakeVersionedGameServerConfigurationsApi : IVersionedGameServerConfigurationsApi
{
    public FakeVersionedGameServerConfigurationsApi(FakeGameServerConfigurationsApi v1) => V1 = v1;
    public IGameServerConfigurationsApi V1 { get; }
}

public class FakeVersionedLiveStatusApi : IVersionedLiveStatusApi
{
    public FakeVersionedLiveStatusApi(FakeLiveStatusApi v1) => V1 = v1;
    public ILiveStatusApi V1 { get; }
}

public class FakeVersionedGlobalAnalyticsApi : IVersionedGlobalAnalyticsApi
{
    public FakeVersionedGlobalAnalyticsApi(FakeGlobalAnalyticsApi v1) => V1 = v1;
    public IGlobalAnalyticsApi V1 { get; }
}

public class FakeVersionedGameAnalyticsApi : IVersionedGameAnalyticsApi
{
    public FakeVersionedGameAnalyticsApi(FakeGameAnalyticsApi v1) => V1 = v1;
    public IGameAnalyticsApi V1 { get; }
}

public class FakeVersionedServerAnalyticsApi : IVersionedServerAnalyticsApi
{
    public FakeVersionedServerAnalyticsApi(FakeServerAnalyticsApi v1) => V1 = v1;
    public IServerAnalyticsApi V1 { get; }
}

public class FakeVersionedDashboardAnalyticsApi : IVersionedDashboardAnalyticsApi
{
    public FakeVersionedDashboardAnalyticsApi(FakeDashboardAnalyticsApi v1) => V1 = v1;
    public IDashboardAnalyticsApi V1 { get; }
}

public class FakeVersionedMapAnalyticsApi : IVersionedMapAnalyticsApi
{
    public FakeVersionedMapAnalyticsApi(FakeMapAnalyticsApi v1) => V1 = v1;
    public IMapAnalyticsApi V1 { get; }
}

public class FakeVersionedPlayerAnalyticsV2Api : IVersionedPlayerAnalyticsV2Api
{
    public FakeVersionedPlayerAnalyticsV2Api(FakePlayerAnalyticsV2Api v1) => V1 = v1;
    public IPlayerAnalyticsV2Api V1 { get; }
}

/// <summary>
/// In-memory fake of <see cref="IRepositoryApiClient"/> for unit and integration tests.
/// Eliminates the need for nested mock hierarchies.
/// </summary>
public class FakeRepositoryApiClient : IRepositoryApiClient
{
    public FakeAdminActionsApi AdminActionsApi { get; } = new();
    public FakeBanFileMonitorsApi BanFileMonitorsApi { get; } = new();
    public FakeCentralBanFileStatusApi CentralBanFileStatusApi { get; } = new();
    public FakeChatMessagesApi ChatMessagesApi { get; } = new();
    public FakeDataMaintenanceApi DataMaintenanceApi { get; } = new();
    public FakeDemosApi DemosApi { get; } = new();
    public FakeGameServersApi GameServersApi { get; } = new();
    public FakeGameServersEventsApi GameServersEventsApi { get; } = new();
    public FakeGameServersStatsApi GameServersStatsApi { get; } = new();
    public FakeGameTrackerBannerApi GameTrackerBannerApi { get; } = new();
    public FakeMapsApi MapsApi { get; } = new();
    public FakeConnectedPlayersApi ConnectedPlayersApi { get; } = new();
    public FakeScreenshotsApi ScreenshotsApi { get; } = new();

    public FakePlayerAnalyticsApi PlayerAnalyticsApi { get; } = new();
    public FakePlayersApi PlayersApi { get; } = new();
    public FakeRecentPlayersApi RecentPlayersApi { get; } = new();
    public FakeReportsApi ReportsApi { get; } = new();
    public FakeUserProfileApi UserProfilesApi { get; } = new();
    public FakeTagsApi TagsApi { get; } = new();
    public FakeGameServersSecretsApi GameServersSecretsApi { get; } = new();
    public FakeApiHealthApi HealthApi { get; } = new();
    public FakeApiInfoApi InfoApi { get; } = new();
    public FakeNotificationTypesApi NotificationTypesApi { get; } = new();
    public FakeNotificationPreferencesApi NotificationPreferencesApi { get; } = new();
    public FakeNotificationsApi NotificationsApi { get; } = new();
    public FakeMapRotationsApi MapRotationsApi { get; } = new();
    public FakeDashboardApi DashboardApi { get; } = new();
    public FakeGlobalConfigurationsApi GlobalConfigurationsApi { get; } = new();
    public FakeGameServerConfigurationsApi GameServerConfigurationsApi { get; } = new();
    public FakeLiveStatusApi LiveStatusApi { get; } = new();
    public FakeGlobalAnalyticsApi GlobalAnalyticsApi { get; } = new();
    public FakeGameAnalyticsApi GameAnalyticsApi { get; } = new();
    public FakeServerAnalyticsApi ServerAnalyticsApi { get; } = new();
    public FakeDashboardAnalyticsApi DashboardAnalyticsApi { get; } = new();
    public FakeMapAnalyticsApi MapAnalyticsApi { get; } = new();
    public FakePlayerAnalyticsV2Api PlayerAnalyticsV2Api { get; } = new();

    private readonly Lazy<FakeVersionedAdminActionsApi> _adminActions;
    private readonly Lazy<FakeVersionedBanFileMonitorsApi> _banFileMonitors;
    private readonly Lazy<FakeVersionedCentralBanFileStatusApi> _centralBanFileStatus;
    private readonly Lazy<FakeVersionedChatMessagesApi> _chatMessages;
    private readonly Lazy<FakeVersionedDataMaintenanceApi> _dataMaintenance;
    private readonly Lazy<FakeVersionedDemosApi> _demos;
    private readonly Lazy<FakeVersionedGameServersApi> _gameServers;
    private readonly Lazy<FakeVersionedGameServersEventsApi> _gameServersEvents;
    private readonly Lazy<FakeVersionedGameServersStatsApi> _gameServersStats;
    private readonly Lazy<FakeVersionedGameTrackerBannerApi> _gameTrackerBanner;
    private readonly Lazy<FakeVersionedMapsApi> _maps;
    private readonly Lazy<FakeVersionedConnectedPlayersApi> _connectedPlayers;
    private readonly Lazy<FakeVersionedScreenshotsApi> _screenshots;

    private readonly Lazy<FakeVersionedPlayerAnalyticsApi> _playerAnalytics;
    private readonly Lazy<FakeVersionedPlayersApi> _players;
    private readonly Lazy<FakeVersionedRecentPlayersApi> _recentPlayers;
    private readonly Lazy<FakeVersionedReportsApi> _reports;
    private readonly Lazy<FakeVersionedUserProfileApi> _userProfiles;
    private readonly Lazy<FakeVersionedTagsApi> _tags;
    private readonly Lazy<FakeVersionedApiHealthApi> _apiHealth;
    private readonly Lazy<FakeVersionedApiInfoApi> _apiInfo;
    private readonly Lazy<FakeVersionedNotificationTypesApi> _notificationTypes;
    private readonly Lazy<FakeVersionedNotificationPreferencesApi> _notificationPreferences;
    private readonly Lazy<FakeVersionedNotificationsApi> _notifications;
    private readonly Lazy<FakeVersionedMapRotationsApi> _mapRotations;
    private readonly Lazy<FakeVersionedDashboardApi> _dashboard;
    private readonly Lazy<FakeVersionedGlobalConfigurationsApi> _globalConfigurations;
    private readonly Lazy<FakeVersionedGameServerConfigurationsApi> _gameServerConfigurations;
    private readonly Lazy<FakeVersionedLiveStatusApi> _liveStatus;
    private readonly Lazy<FakeVersionedGlobalAnalyticsApi> _globalAnalytics;
    private readonly Lazy<FakeVersionedGameAnalyticsApi> _gameAnalytics;
    private readonly Lazy<FakeVersionedServerAnalyticsApi> _serverAnalytics;
    private readonly Lazy<FakeVersionedDashboardAnalyticsApi> _dashboardAnalytics;
    private readonly Lazy<FakeVersionedMapAnalyticsApi> _mapAnalytics;
    private readonly Lazy<FakeVersionedPlayerAnalyticsV2Api> _playerAnalyticsV2;

    public FakeRepositoryApiClient()
    {
        _adminActions = new Lazy<FakeVersionedAdminActionsApi>(() => new FakeVersionedAdminActionsApi(AdminActionsApi));
        _banFileMonitors = new Lazy<FakeVersionedBanFileMonitorsApi>(() => new FakeVersionedBanFileMonitorsApi(BanFileMonitorsApi));
        _centralBanFileStatus = new Lazy<FakeVersionedCentralBanFileStatusApi>(() => new FakeVersionedCentralBanFileStatusApi(CentralBanFileStatusApi));
        _chatMessages = new Lazy<FakeVersionedChatMessagesApi>(() => new FakeVersionedChatMessagesApi(ChatMessagesApi));
        _dataMaintenance = new Lazy<FakeVersionedDataMaintenanceApi>(() => new FakeVersionedDataMaintenanceApi(DataMaintenanceApi));
        _demos = new Lazy<FakeVersionedDemosApi>(() => new FakeVersionedDemosApi(DemosApi));
        _gameServers = new Lazy<FakeVersionedGameServersApi>(() => new FakeVersionedGameServersApi(GameServersApi));
        _gameServersEvents = new Lazy<FakeVersionedGameServersEventsApi>(() => new FakeVersionedGameServersEventsApi(GameServersEventsApi));
        _gameServersStats = new Lazy<FakeVersionedGameServersStatsApi>(() => new FakeVersionedGameServersStatsApi(GameServersStatsApi));
        _gameTrackerBanner = new Lazy<FakeVersionedGameTrackerBannerApi>(() => new FakeVersionedGameTrackerBannerApi(GameTrackerBannerApi));
        _maps = new Lazy<FakeVersionedMapsApi>(() => new FakeVersionedMapsApi(MapsApi));
        _connectedPlayers = new Lazy<FakeVersionedConnectedPlayersApi>(() => new FakeVersionedConnectedPlayersApi(ConnectedPlayersApi));
        _screenshots = new Lazy<FakeVersionedScreenshotsApi>(() => new FakeVersionedScreenshotsApi(ScreenshotsApi));

        _playerAnalytics = new Lazy<FakeVersionedPlayerAnalyticsApi>(() => new FakeVersionedPlayerAnalyticsApi(PlayerAnalyticsApi));
        _players = new Lazy<FakeVersionedPlayersApi>(() => new FakeVersionedPlayersApi(PlayersApi));
        _recentPlayers = new Lazy<FakeVersionedRecentPlayersApi>(() => new FakeVersionedRecentPlayersApi(RecentPlayersApi));
        _reports = new Lazy<FakeVersionedReportsApi>(() => new FakeVersionedReportsApi(ReportsApi));
        _userProfiles = new Lazy<FakeVersionedUserProfileApi>(() => new FakeVersionedUserProfileApi(UserProfilesApi));
        _tags = new Lazy<FakeVersionedTagsApi>(() => new FakeVersionedTagsApi(TagsApi));
        _apiHealth = new Lazy<FakeVersionedApiHealthApi>(() => new FakeVersionedApiHealthApi(HealthApi));
        _apiInfo = new Lazy<FakeVersionedApiInfoApi>(() => new FakeVersionedApiInfoApi(InfoApi));
        _notificationTypes = new Lazy<FakeVersionedNotificationTypesApi>(() => new FakeVersionedNotificationTypesApi(NotificationTypesApi));
        _notificationPreferences = new Lazy<FakeVersionedNotificationPreferencesApi>(() => new FakeVersionedNotificationPreferencesApi(NotificationPreferencesApi));
        _notifications = new Lazy<FakeVersionedNotificationsApi>(() => new FakeVersionedNotificationsApi(NotificationsApi));
        _mapRotations = new Lazy<FakeVersionedMapRotationsApi>(() => new FakeVersionedMapRotationsApi(MapRotationsApi));
        _dashboard = new Lazy<FakeVersionedDashboardApi>(() => new FakeVersionedDashboardApi(DashboardApi));
        _globalConfigurations = new Lazy<FakeVersionedGlobalConfigurationsApi>(() => new FakeVersionedGlobalConfigurationsApi(GlobalConfigurationsApi));
        _gameServerConfigurations = new Lazy<FakeVersionedGameServerConfigurationsApi>(() => new FakeVersionedGameServerConfigurationsApi(GameServerConfigurationsApi));
        _liveStatus = new Lazy<FakeVersionedLiveStatusApi>(() => new FakeVersionedLiveStatusApi(LiveStatusApi));
        _globalAnalytics = new Lazy<FakeVersionedGlobalAnalyticsApi>(() => new FakeVersionedGlobalAnalyticsApi(GlobalAnalyticsApi));
        _gameAnalytics = new Lazy<FakeVersionedGameAnalyticsApi>(() => new FakeVersionedGameAnalyticsApi(GameAnalyticsApi));
        _serverAnalytics = new Lazy<FakeVersionedServerAnalyticsApi>(() => new FakeVersionedServerAnalyticsApi(ServerAnalyticsApi));
        _dashboardAnalytics = new Lazy<FakeVersionedDashboardAnalyticsApi>(() => new FakeVersionedDashboardAnalyticsApi(DashboardAnalyticsApi));
        _mapAnalytics = new Lazy<FakeVersionedMapAnalyticsApi>(() => new FakeVersionedMapAnalyticsApi(MapAnalyticsApi));
        _playerAnalyticsV2 = new Lazy<FakeVersionedPlayerAnalyticsV2Api>(() => new FakeVersionedPlayerAnalyticsV2Api(PlayerAnalyticsV2Api));
    }

    public IVersionedAdminActionsApi AdminActions => _adminActions.Value;
    public IVersionedBanFileMonitorsApi BanFileMonitors => _banFileMonitors.Value;
    public IVersionedCentralBanFileStatusApi CentralBanFileStatus => _centralBanFileStatus.Value;
    public IVersionedChatMessagesApi ChatMessages => _chatMessages.Value;
    public IVersionedDataMaintenanceApi DataMaintenance => _dataMaintenance.Value;
    public IVersionedDemosApi Demos => _demos.Value;
    public IVersionedGameServersApi GameServers => _gameServers.Value;
    public IVersionedGameServersEventsApi GameServersEvents => _gameServersEvents.Value;
    public IVersionedGameServersStatsApi GameServersStats => _gameServersStats.Value;
    public IVersionedGameTrackerBannerApi GameTrackerBanner => _gameTrackerBanner.Value;
    public IVersionedMapsApi Maps => _maps.Value;
    public IVersionedConnectedPlayersApi ConnectedPlayers => _connectedPlayers.Value;
    public IVersionedScreenshotsApi Screenshots => _screenshots.Value;

    public IVersionedPlayerAnalyticsApi PlayerAnalytics => _playerAnalytics.Value;
    public IVersionedPlayersApi Players => _players.Value;
    public IVersionedRecentPlayersApi RecentPlayers => _recentPlayers.Value;
    public IVersionedReportsApi Reports => _reports.Value;
    public IVersionedUserProfileApi UserProfiles => _userProfiles.Value;
    public IVersionedTagsApi Tags => _tags.Value;
    public IVersionedApiHealthApi ApiHealth => _apiHealth.Value;
    public IVersionedApiInfoApi ApiInfo => _apiInfo.Value;
    public IVersionedNotificationTypesApi NotificationTypes => _notificationTypes.Value;
    public IVersionedNotificationPreferencesApi NotificationPreferences => _notificationPreferences.Value;
    public IVersionedNotificationsApi Notifications => _notifications.Value;
    public IVersionedMapRotationsApi MapRotations => _mapRotations.Value;
    public IVersionedDashboardApi Dashboard => _dashboard.Value;
    public IVersionedGlobalConfigurationsApi GlobalConfigurations => _globalConfigurations.Value;
    public IVersionedGameServerConfigurationsApi GameServerConfigurations => _gameServerConfigurations.Value;
    public IVersionedLiveStatusApi LiveStatus => _liveStatus.Value;
    public IVersionedGlobalAnalyticsApi GlobalAnalytics => _globalAnalytics.Value;
    public IVersionedGameAnalyticsApi GameAnalytics => _gameAnalytics.Value;
    public IVersionedServerAnalyticsApi ServerAnalytics => _serverAnalytics.Value;
    public IVersionedDashboardAnalyticsApi DashboardAnalytics => _dashboardAnalytics.Value;
    public IVersionedMapAnalyticsApi MapAnalytics => _mapAnalytics.Value;
    public IVersionedPlayerAnalyticsV2Api PlayerAnalyticsV2 => _playerAnalyticsV2.Value;

    /// <summary>
    /// Resets all fakes to their initial state, clearing configured responses,
    /// error responses, and tracking state.
    /// </summary>
    public FakeRepositoryApiClient Reset()
    {
        AdminActionsApi.Reset();
        BanFileMonitorsApi.Reset();
        CentralBanFileStatusApi.Reset();
        ChatMessagesApi.Reset();
        DataMaintenanceApi.Reset();
        DemosApi.Reset();
        GameServersApi.Reset();
        GameServersEventsApi.Reset();
        GameServersStatsApi.Reset();
        GameTrackerBannerApi.Reset();
        MapsApi.Reset();
        ConnectedPlayersApi.Reset();
        ScreenshotsApi.Reset();

        PlayerAnalyticsApi.Reset();
        PlayersApi.Reset();
        RecentPlayersApi.Reset();
        ReportsApi.Reset();
        UserProfilesApi.Reset();
        TagsApi.Reset();
        GameServersSecretsApi.Reset();
        NotificationTypesApi.Reset();
        NotificationPreferencesApi.Reset();
        NotificationsApi.Reset();
        MapRotationsApi.Reset();
        DashboardApi.Reset();
        GlobalConfigurationsApi.Reset();
        GameServerConfigurationsApi.Reset();
        LiveStatusApi.Reset();
        GlobalAnalyticsApi.Reset();
        GameAnalyticsApi.Reset();
        ServerAnalyticsApi.Reset();
        DashboardAnalyticsApi.Reset();
        MapAnalyticsApi.Reset();
        PlayerAnalyticsV2Api.Reset();
        return this;
    }
}
