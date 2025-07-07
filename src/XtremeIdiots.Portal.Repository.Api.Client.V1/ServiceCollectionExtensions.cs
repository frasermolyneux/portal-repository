using Microsoft.Extensions.DependencyInjection;

using MxIO.ApiClient.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the unified repository API client with version selectors.
        /// This provides access to all APIs like: client.Players.V1 or client.Root.V1_1
        /// </summary>
        public static void AddRepositoryApiClient(this IServiceCollection serviceCollection, Action<RepositoryApiClientOptions> configure)
        {
            serviceCollection.AddApiClient();
            serviceCollection.Configure(configure);

            // Register V1 API implementations as scoped to match IRestClientService lifetime
            serviceCollection.AddScoped<AdminActionsApi>();
            serviceCollection.AddScoped<BanFileMonitorsApi>();
            serviceCollection.AddScoped<ChatMessagesApi>();
            serviceCollection.AddScoped<DataMaintenanceApi>();
            serviceCollection.AddScoped<DemosApi>();
            serviceCollection.AddScoped<GameServersApi>();
            serviceCollection.AddScoped<GameServersEventsApi>();
            serviceCollection.AddScoped<GameServersStatsApi>();
            serviceCollection.AddScoped<GameTrackerBannerApi>();
            serviceCollection.AddScoped<LivePlayersApi>();
            serviceCollection.AddScoped<MapsApi>();
            serviceCollection.AddScoped<MapPacksApi>();
            serviceCollection.AddScoped<PlayerAnalyticsApi>();
            serviceCollection.AddScoped<PlayersApi>();
            serviceCollection.AddScoped<RecentPlayersApi>();
            serviceCollection.AddScoped<ReportsApi>();
            serviceCollection.AddScoped<RootApi>();
            serviceCollection.AddScoped<TagsApi>();
            serviceCollection.AddScoped<UserProfileApi>();

            // Register V1.1 API implementations as scoped to match IRestClientService lifetime
            serviceCollection.AddScoped<XtremeIdiots.Portal.Repository.Api.Client.V1_1.RootApi>();

            // Register V1 API interfaces using concrete implementations as scoped
            serviceCollection.AddScoped<IAdminActionsApi>(provider => provider.GetRequiredService<AdminActionsApi>());
            serviceCollection.AddScoped<IBanFileMonitorsApi>(provider => provider.GetRequiredService<BanFileMonitorsApi>());
            serviceCollection.AddScoped<IChatMessagesApi>(provider => provider.GetRequiredService<ChatMessagesApi>());
            serviceCollection.AddScoped<IDataMaintenanceApi>(provider => provider.GetRequiredService<DataMaintenanceApi>());
            serviceCollection.AddScoped<IDemosApi>(provider => provider.GetRequiredService<DemosApi>());
            serviceCollection.AddScoped<IGameServersApi>(provider => provider.GetRequiredService<GameServersApi>());
            serviceCollection.AddScoped<IGameServersEventsApi>(provider => provider.GetRequiredService<GameServersEventsApi>());
            serviceCollection.AddScoped<IGameServersStatsApi>(provider => provider.GetRequiredService<GameServersStatsApi>());
            serviceCollection.AddScoped<IGameTrackerBannerApi>(provider => provider.GetRequiredService<GameTrackerBannerApi>());
            serviceCollection.AddScoped<ILivePlayersApi>(provider => provider.GetRequiredService<LivePlayersApi>());
            serviceCollection.AddScoped<IMapsApi>(provider => provider.GetRequiredService<MapsApi>());
            serviceCollection.AddScoped<IMapPacksApi>(provider => provider.GetRequiredService<MapPacksApi>());
            serviceCollection.AddScoped<IPlayerAnalyticsApi>(provider => provider.GetRequiredService<PlayerAnalyticsApi>());
            serviceCollection.AddScoped<IPlayersApi>(provider => provider.GetRequiredService<PlayersApi>());
            serviceCollection.AddScoped<IRecentPlayersApi>(provider => provider.GetRequiredService<RecentPlayersApi>());
            serviceCollection.AddScoped<IReportsApi>(provider => provider.GetRequiredService<ReportsApi>());
            serviceCollection.AddScoped<IRootApi>(provider => provider.GetRequiredService<RootApi>());
            serviceCollection.AddScoped<ITagsApi>(provider => provider.GetRequiredService<TagsApi>());
            serviceCollection.AddScoped<IUserProfileApi>(provider => provider.GetRequiredService<UserProfileApi>());

            // Register version selectors as scoped to match V1 API lifetime
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
            serviceCollection.AddScoped<IVersionedRootApi>(provider =>
                new VersionedRootApi(
                    provider.GetRequiredService<RootApi>(),
                    provider.GetRequiredService<XtremeIdiots.Portal.Repository.Api.Client.V1_1.RootApi>()
                ));

            // Register the unified client as scoped to match versioned API lifetime
            serviceCollection.AddScoped<IRepositoryApiClient, RepositoryApiClient>();
        }
    }
}
