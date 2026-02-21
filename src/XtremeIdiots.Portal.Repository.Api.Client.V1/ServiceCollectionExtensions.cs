using Microsoft.Extensions.DependencyInjection;
using MX.Api.Client.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

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
            // Register V1 API implementations using the new typed pattern
            serviceCollection.AddTypedApiClient<IAdminActionsApi, AdminActionsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IBanFileMonitorsApi, BanFileMonitorsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IChatMessagesApi, ChatMessagesApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IDataMaintenanceApi, DataMaintenanceApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IDemosApi, DemosApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IGameServersApi, GameServersApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IGameServersEventsApi, GameServersEventsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IGameServersStatsApi, GameServersStatsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IGameTrackerBannerApi, GameTrackerBannerApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<ILivePlayersApi, LivePlayersApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IMapsApi, MapsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IMapPacksApi, MapPacksApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IPlayerAnalyticsApi, PlayerAnalyticsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IPlayersApi, PlayersApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IRecentPlayersApi, RecentPlayersApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IReportsApi, ReportsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<ITagsApi, TagsApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);
            serviceCollection.AddTypedApiClient<IUserProfileApi, UserProfileApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);

            // Register API info endpoint
            serviceCollection.AddTypedApiClient<IApiInfoApi, ApiInfoApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);

            // Register API health endpoint
            serviceCollection.AddTypedApiClient<IApiHealthApi, ApiHealthApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);

            // Register version selectors as scoped
            serviceCollection.AddScoped<IVersionedAdminActionsApi, VersionedAdminActionsApi>();
            serviceCollection.AddScoped<IVersionedBanFileMonitorsApi, VersionedBanFileMonitorsApi>();
            serviceCollection.AddScoped<IVersionedChatMessagesApi, VersionedChatMessagesApi>();
            serviceCollection.AddScoped<IVersionedDataMaintenanceApi, VersionedDataMaintenanceApi>();
            serviceCollection.AddScoped<IVersionedDemosApi, VersionedDemosApi>();
            serviceCollection.AddScoped<IVersionedGameServersApi, VersionedGameServersApi>();
            serviceCollection.AddScoped<IVersionedGameServersEventsApi, VersionedGameServersEventsApi>();
            serviceCollection.AddScoped<IVersionedGameServersStatsApi, VersionedGameServersStatsApi>();
            serviceCollection.AddScoped<IVersionedGameTrackerBannerApi, VersionedGameTrackerBannerApi>();
            serviceCollection.AddScoped<IVersionedLivePlayersApi, VersionedLivePlayersApi>();
            serviceCollection.AddScoped<IVersionedMapsApi, VersionedMapsApi>();
            serviceCollection.AddScoped<IVersionedMapPacksApi, VersionedMapPacksApi>();
            serviceCollection.AddScoped<IVersionedPlayerAnalyticsApi, VersionedPlayerAnalyticsApi>();
            serviceCollection.AddScoped<IVersionedPlayersApi, VersionedPlayersApi>();
            serviceCollection.AddScoped<IVersionedRecentPlayersApi, VersionedRecentPlayersApi>();
            serviceCollection.AddScoped<IVersionedReportsApi, VersionedReportsApi>();
            serviceCollection.AddScoped<IVersionedTagsApi, VersionedTagsApi>();
            serviceCollection.AddScoped<IVersionedUserProfileApi, VersionedUserProfileApi>();
            serviceCollection.AddScoped<IVersionedApiHealthApi, VersionedApiHealthApi>();
            serviceCollection.AddScoped<IVersionedApiInfoApi, VersionedApiInfoApi>();

            // Register the unified client as scoped
            serviceCollection.AddScoped<IRepositoryApiClient, RepositoryApiClient>();

            return serviceCollection;
        }
    }
}
