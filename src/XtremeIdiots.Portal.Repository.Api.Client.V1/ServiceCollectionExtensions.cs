using Microsoft.Extensions.DependencyInjection;
using MX.Api.Client.Configuration;
using MX.Api.Client.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.Client.V1.Caching;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Repository API client services with custom configuration
        /// </summary>
        /// <param name="serviceCollection">The service collection</param>
        /// <param name="configureOptions">Action to configure the Repository API options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddRepositoryApiClient(
            this IServiceCollection serviceCollection,
            Action<RepositoryApiOptionsBuilder> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(serviceCollection);
            ArgumentNullException.ThrowIfNull(configureOptions);

            // Run configureOptions once against a throwaway probe to extract any caching delegate the
            // consumer captured through RepositoryApiOptionsBuilder.WithCaching(Action<CacheBuilder>).
            // The probe's remaining state is discarded - only the captured cache delegate is reused.
            var probe = new RepositoryApiOptionsBuilder();
            configureOptions(probe);
            var capturedCache = probe.CapturedCacheConfigure;

            // One SharedCacheConfiguration per AddRepositoryApiClient invocation, shared across every
            // typed sub-API registration. The library scopes operations per typed client at apply time
            // and skips non-matching siblings; ValidateAllOperationsMatched() below guards against typos
            // (an expression that never matched any registered typed client).
            var sharedCache = capturedCache is null
                ? null
                : new SharedCacheConfiguration(capturedCache);

            Action<RepositoryApiOptionsBuilder> perClient = sharedCache is null
                ? configureOptions
                : builder =>
                {
                    configureOptions(builder);
                    builder.WithSharedCaching(sharedCache);
                };

            // Register library default cache policies per sub-API interface (the same interface
            // supplied to AddTypedApiClient below). Consumers opt in per client with
            // UseLibraryDefaults() on their CacheBuilder.
            serviceCollection.AddDefaultCachePolicies<IGameServersApi>(RepositoryApiCacheDefaults.ConfigureGameServers);
            serviceCollection.AddDefaultCachePolicies<IMapsApi>(RepositoryApiCacheDefaults.ConfigureMaps);
            serviceCollection.AddDefaultCachePolicies<IUserProfileApi>(RepositoryApiCacheDefaults.ConfigureUserProfile);
            serviceCollection.AddDefaultCachePolicies<IApiInfoApi>(RepositoryApiCacheDefaults.ConfigureApiInfo);
            serviceCollection.AddDefaultCachePolicies<IApiHealthApi>(RepositoryApiCacheDefaults.ConfigureApiHealth);

            // Register V1 API implementations using the new typed pattern
            serviceCollection.AddTypedApiClient<IAdminActionsApi, AdminActionsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IBanFileMonitorsApi, BanFileMonitorsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<ICentralBanFileStatusApi, CentralBanFileStatusApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IChatMessagesApi, ChatMessagesApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IDataMaintenanceApi, DataMaintenanceApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IDemosApi, DemosApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IGameServersApi, GameServersApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IGameServersEventsApi, GameServersEventsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IGameServersStatsApi, GameServersStatsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IGameTrackerBannerApi, GameTrackerBannerApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IMapsApi, MapsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IConnectedPlayersApi, ConnectedPlayersApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);

            serviceCollection.AddTypedApiClient<IPlayerAnalyticsApi, PlayerAnalyticsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IPlayersApi, PlayersApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IRecentPlayersApi, RecentPlayersApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IReportsApi, ReportsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<ITagsApi, TagsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IUserProfileApi, UserProfileApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);

            // Register API info endpoint
            serviceCollection.AddTypedApiClient<IApiInfoApi, ApiInfoApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);

            // Register API health endpoint
            serviceCollection.AddTypedApiClient<IApiHealthApi, ApiHealthApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);

            // Register Notification API implementations
            serviceCollection.AddTypedApiClient<INotificationTypesApi, NotificationTypesApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<INotificationPreferencesApi, NotificationPreferencesApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<INotificationsApi, NotificationsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IMapRotationsApi, MapRotationsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IDashboardApi, DashboardApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IGlobalConfigurationsApi, GlobalConfigurationsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IGameServerConfigurationsApi, GameServerConfigurationsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<ILiveStatusApi, LiveStatusApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IGlobalAnalyticsApi, GlobalAnalyticsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IGameAnalyticsApi, GameAnalyticsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IServerAnalyticsApi, ServerAnalyticsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IDashboardAnalyticsApi, DashboardAnalyticsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IMapAnalyticsApi, MapAnalyticsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);
            serviceCollection.AddTypedApiClient<IPlayerAnalyticsV2Api, PlayerAnalyticsV2Api, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(perClient);

            // Once every typed sub-API has been registered, verify every operation captured on the
            // shared cache configuration matched at least one registered typed client. Throws
            // InvalidOperationException on orphaned operations (e.g. a consumer expression that
            // targets an interface which is not a registered Repository sub-API).
            sharedCache?.ValidateAllOperationsMatched();

            // Register version selectors as scoped
            serviceCollection.AddScoped<IVersionedAdminActionsApi, VersionedAdminActionsApi>();
            serviceCollection.AddScoped<IVersionedBanFileMonitorsApi, VersionedBanFileMonitorsApi>();
            serviceCollection.AddScoped<IVersionedCentralBanFileStatusApi, VersionedCentralBanFileStatusApi>();
            serviceCollection.AddScoped<IVersionedChatMessagesApi, VersionedChatMessagesApi>();
            serviceCollection.AddScoped<IVersionedDataMaintenanceApi, VersionedDataMaintenanceApi>();
            serviceCollection.AddScoped<IVersionedDemosApi, VersionedDemosApi>();
            serviceCollection.AddScoped<IVersionedGameServersApi, VersionedGameServersApi>();
            serviceCollection.AddScoped<IVersionedGameServersEventsApi, VersionedGameServersEventsApi>();
            serviceCollection.AddScoped<IVersionedGameServersStatsApi, VersionedGameServersStatsApi>();
            serviceCollection.AddScoped<IVersionedGameTrackerBannerApi, VersionedGameTrackerBannerApi>();
            serviceCollection.AddScoped<IVersionedMapsApi, VersionedMapsApi>();
            serviceCollection.AddScoped<IVersionedConnectedPlayersApi, VersionedConnectedPlayersApi>();

            serviceCollection.AddScoped<IVersionedPlayerAnalyticsApi, VersionedPlayerAnalyticsApi>();
            serviceCollection.AddScoped<IVersionedPlayersApi, VersionedPlayersApi>();
            serviceCollection.AddScoped<IVersionedRecentPlayersApi, VersionedRecentPlayersApi>();
            serviceCollection.AddScoped<IVersionedReportsApi, VersionedReportsApi>();
            serviceCollection.AddScoped<IVersionedTagsApi, VersionedTagsApi>();
            serviceCollection.AddScoped<IVersionedUserProfileApi, VersionedUserProfileApi>();
            serviceCollection.AddScoped<IVersionedApiHealthApi, VersionedApiHealthApi>();
            serviceCollection.AddScoped<IVersionedApiInfoApi, VersionedApiInfoApi>();
            serviceCollection.AddScoped<IVersionedNotificationTypesApi, VersionedNotificationTypesApi>();
            serviceCollection.AddScoped<IVersionedNotificationPreferencesApi, VersionedNotificationPreferencesApi>();
            serviceCollection.AddScoped<IVersionedNotificationsApi, VersionedNotificationsApi>();
            serviceCollection.AddScoped<IVersionedMapRotationsApi, VersionedMapRotationsApi>();
            serviceCollection.AddScoped<IVersionedDashboardApi, VersionedDashboardApi>();
            serviceCollection.AddScoped<IVersionedGlobalConfigurationsApi, VersionedGlobalConfigurationsApi>();
            serviceCollection.AddScoped<IVersionedGameServerConfigurationsApi, VersionedGameServerConfigurationsApi>();
            serviceCollection.AddScoped<IVersionedLiveStatusApi, VersionedLiveStatusApi>();
            serviceCollection.AddScoped<IVersionedGlobalAnalyticsApi, VersionedGlobalAnalyticsApi>();
            serviceCollection.AddScoped<IVersionedGameAnalyticsApi, VersionedGameAnalyticsApi>();
            serviceCollection.AddScoped<IVersionedServerAnalyticsApi, VersionedServerAnalyticsApi>();
            serviceCollection.AddScoped<IVersionedDashboardAnalyticsApi, VersionedDashboardAnalyticsApi>();
            serviceCollection.AddScoped<IVersionedMapAnalyticsApi, VersionedMapAnalyticsApi>();
            serviceCollection.AddScoped<IVersionedPlayerAnalyticsV2Api, VersionedPlayerAnalyticsV2Api>();

            // Register the unified client as scoped
            serviceCollection.AddScoped<IRepositoryApiClient, RepositoryApiClient>();

            return serviceCollection;
        }
    }
}
