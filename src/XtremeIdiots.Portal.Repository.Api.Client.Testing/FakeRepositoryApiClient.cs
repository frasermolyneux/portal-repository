using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;
using XtremeIdiots.Portal.Repository.Api.Client.V1;

using V1_1Interfaces = XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1_1;

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

public class FakeVersionedLivePlayersApi : IVersionedLivePlayersApi
{
    public FakeVersionedLivePlayersApi(FakeLivePlayersApi v1) => V1 = v1;
    public ILivePlayersApi V1 { get; }
}

public class FakeVersionedMapsApi : IVersionedMapsApi
{
    public FakeVersionedMapsApi(FakeMapsApi v1) => V1 = v1;
    public IMapsApi V1 { get; }
}

public class FakeVersionedMapPacksApi : IVersionedMapPacksApi
{
    public FakeVersionedMapPacksApi(FakeMapPacksApi v1) => V1 = v1;
    public IMapPacksApi V1 { get; }
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

public class FakeVersionedRootApi : IVersionedRootApi
{
    public FakeVersionedRootApi(FakeRootApi rootApi)
    {
        V1 = rootApi;
        V1_1 = rootApi;
    }

    public IRootApi V1 { get; }
    public V1_1Interfaces.IRootApi V1_1 { get; }
}

/// <summary>
/// In-memory fake of <see cref="IRepositoryApiClient"/> for unit and integration tests.
/// Eliminates the need for nested mock hierarchies.
/// </summary>
public class FakeRepositoryApiClient : IRepositoryApiClient
{
    public FakeAdminActionsApi AdminActionsApi { get; } = new();
    public FakeBanFileMonitorsApi BanFileMonitorsApi { get; } = new();
    public FakeChatMessagesApi ChatMessagesApi { get; } = new();
    public FakeDataMaintenanceApi DataMaintenanceApi { get; } = new();
    public FakeDemosApi DemosApi { get; } = new();
    public FakeGameServersApi GameServersApi { get; } = new();
    public FakeGameServersEventsApi GameServersEventsApi { get; } = new();
    public FakeGameServersStatsApi GameServersStatsApi { get; } = new();
    public FakeGameTrackerBannerApi GameTrackerBannerApi { get; } = new();
    public FakeLivePlayersApi LivePlayersApi { get; } = new();
    public FakeMapsApi MapsApi { get; } = new();
    public FakeMapPacksApi MapPacksApi { get; } = new();
    public FakePlayerAnalyticsApi PlayerAnalyticsApi { get; } = new();
    public FakePlayersApi PlayersApi { get; } = new();
    public FakeRecentPlayersApi RecentPlayersApi { get; } = new();
    public FakeReportsApi ReportsApi { get; } = new();
    public FakeRootApi RootApi { get; } = new();
    public FakeUserProfileApi UserProfilesApi { get; } = new();
    public FakeTagsApi TagsApi { get; } = new();
    public FakeGameServersSecretsApi GameServersSecretsApi { get; } = new();

    private readonly Lazy<FakeVersionedAdminActionsApi> _adminActions;
    private readonly Lazy<FakeVersionedBanFileMonitorsApi> _banFileMonitors;
    private readonly Lazy<FakeVersionedChatMessagesApi> _chatMessages;
    private readonly Lazy<FakeVersionedDataMaintenanceApi> _dataMaintenance;
    private readonly Lazy<FakeVersionedDemosApi> _demos;
    private readonly Lazy<FakeVersionedGameServersApi> _gameServers;
    private readonly Lazy<FakeVersionedGameServersEventsApi> _gameServersEvents;
    private readonly Lazy<FakeVersionedGameServersStatsApi> _gameServersStats;
    private readonly Lazy<FakeVersionedGameTrackerBannerApi> _gameTrackerBanner;
    private readonly Lazy<FakeVersionedLivePlayersApi> _livePlayers;
    private readonly Lazy<FakeVersionedMapsApi> _maps;
    private readonly Lazy<FakeVersionedMapPacksApi> _mapPacks;
    private readonly Lazy<FakeVersionedPlayerAnalyticsApi> _playerAnalytics;
    private readonly Lazy<FakeVersionedPlayersApi> _players;
    private readonly Lazy<FakeVersionedRecentPlayersApi> _recentPlayers;
    private readonly Lazy<FakeVersionedReportsApi> _reports;
    private readonly Lazy<FakeVersionedRootApi> _root;
    private readonly Lazy<FakeVersionedUserProfileApi> _userProfiles;
    private readonly Lazy<FakeVersionedTagsApi> _tags;

    public FakeRepositoryApiClient()
    {
        _adminActions = new Lazy<FakeVersionedAdminActionsApi>(() => new FakeVersionedAdminActionsApi(AdminActionsApi));
        _banFileMonitors = new Lazy<FakeVersionedBanFileMonitorsApi>(() => new FakeVersionedBanFileMonitorsApi(BanFileMonitorsApi));
        _chatMessages = new Lazy<FakeVersionedChatMessagesApi>(() => new FakeVersionedChatMessagesApi(ChatMessagesApi));
        _dataMaintenance = new Lazy<FakeVersionedDataMaintenanceApi>(() => new FakeVersionedDataMaintenanceApi(DataMaintenanceApi));
        _demos = new Lazy<FakeVersionedDemosApi>(() => new FakeVersionedDemosApi(DemosApi));
        _gameServers = new Lazy<FakeVersionedGameServersApi>(() => new FakeVersionedGameServersApi(GameServersApi));
        _gameServersEvents = new Lazy<FakeVersionedGameServersEventsApi>(() => new FakeVersionedGameServersEventsApi(GameServersEventsApi));
        _gameServersStats = new Lazy<FakeVersionedGameServersStatsApi>(() => new FakeVersionedGameServersStatsApi(GameServersStatsApi));
        _gameTrackerBanner = new Lazy<FakeVersionedGameTrackerBannerApi>(() => new FakeVersionedGameTrackerBannerApi(GameTrackerBannerApi));
        _livePlayers = new Lazy<FakeVersionedLivePlayersApi>(() => new FakeVersionedLivePlayersApi(LivePlayersApi));
        _maps = new Lazy<FakeVersionedMapsApi>(() => new FakeVersionedMapsApi(MapsApi));
        _mapPacks = new Lazy<FakeVersionedMapPacksApi>(() => new FakeVersionedMapPacksApi(MapPacksApi));
        _playerAnalytics = new Lazy<FakeVersionedPlayerAnalyticsApi>(() => new FakeVersionedPlayerAnalyticsApi(PlayerAnalyticsApi));
        _players = new Lazy<FakeVersionedPlayersApi>(() => new FakeVersionedPlayersApi(PlayersApi));
        _recentPlayers = new Lazy<FakeVersionedRecentPlayersApi>(() => new FakeVersionedRecentPlayersApi(RecentPlayersApi));
        _reports = new Lazy<FakeVersionedReportsApi>(() => new FakeVersionedReportsApi(ReportsApi));
        _root = new Lazy<FakeVersionedRootApi>(() => new FakeVersionedRootApi(RootApi));
        _userProfiles = new Lazy<FakeVersionedUserProfileApi>(() => new FakeVersionedUserProfileApi(UserProfilesApi));
        _tags = new Lazy<FakeVersionedTagsApi>(() => new FakeVersionedTagsApi(TagsApi));
    }

    public IVersionedAdminActionsApi AdminActions => _adminActions.Value;
    public IVersionedBanFileMonitorsApi BanFileMonitors => _banFileMonitors.Value;
    public IVersionedChatMessagesApi ChatMessages => _chatMessages.Value;
    public IVersionedDataMaintenanceApi DataMaintenance => _dataMaintenance.Value;
    public IVersionedDemosApi Demos => _demos.Value;
    public IVersionedGameServersApi GameServers => _gameServers.Value;
    public IVersionedGameServersEventsApi GameServersEvents => _gameServersEvents.Value;
    public IVersionedGameServersStatsApi GameServersStats => _gameServersStats.Value;
    public IVersionedGameTrackerBannerApi GameTrackerBanner => _gameTrackerBanner.Value;
    public IVersionedLivePlayersApi LivePlayers => _livePlayers.Value;
    public IVersionedMapsApi Maps => _maps.Value;
    public IVersionedMapPacksApi MapPacks => _mapPacks.Value;
    public IVersionedPlayerAnalyticsApi PlayerAnalytics => _playerAnalytics.Value;
    public IVersionedPlayersApi Players => _players.Value;
    public IVersionedRecentPlayersApi RecentPlayers => _recentPlayers.Value;
    public IVersionedReportsApi Reports => _reports.Value;
    public IVersionedRootApi Root => _root.Value;
    public IVersionedUserProfileApi UserProfiles => _userProfiles.Value;
    public IVersionedTagsApi Tags => _tags.Value;

    /// <summary>
    /// Resets all fakes to their initial state, clearing configured responses,
    /// error responses, and tracking state.
    /// </summary>
    public FakeRepositoryApiClient Reset()
    {
        AdminActionsApi.Reset();
        BanFileMonitorsApi.Reset();
        ChatMessagesApi.Reset();
        DataMaintenanceApi.Reset();
        DemosApi.Reset();
        GameServersApi.Reset();
        GameServersEventsApi.Reset();
        GameServersStatsApi.Reset();
        GameTrackerBannerApi.Reset();
        LivePlayersApi.Reset();
        MapsApi.Reset();
        MapPacksApi.Reset();
        PlayerAnalyticsApi.Reset();
        PlayersApi.Reset();
        RecentPlayersApi.Reset();
        ReportsApi.Reset();
        RootApi.Reset();
        UserProfilesApi.Reset();
        TagsApi.Reset();
        GameServersSecretsApi.Reset();
        return this;
    }
}
