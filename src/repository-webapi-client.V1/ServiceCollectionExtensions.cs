using Microsoft.Extensions.DependencyInjection;

using MxIO.ApiClient.Extensions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;

namespace XtremeIdiots.Portal.RepositoryApiClient.V1
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

            // Register V1 API implementations
            serviceCollection.AddSingleton<AdminActionsApi>();
            serviceCollection.AddSingleton<BanFileMonitorsApi>();
            serviceCollection.AddSingleton<ChatMessagesApi>();
            serviceCollection.AddSingleton<DataMaintenanceApi>();
            serviceCollection.AddSingleton<DemosApi>();
            serviceCollection.AddSingleton<GameServersApi>();
            serviceCollection.AddSingleton<GameServersEventsApi>();
            serviceCollection.AddSingleton<GameServersStatsApi>();
            serviceCollection.AddSingleton<GameTrackerBannerApi>();
            serviceCollection.AddSingleton<LivePlayersApi>();
            serviceCollection.AddSingleton<MapsApi>();
            serviceCollection.AddSingleton<MapPacksApi>();
            serviceCollection.AddSingleton<PlayerAnalyticsApi>();
            serviceCollection.AddSingleton<PlayersApi>();
            serviceCollection.AddSingleton<RecentPlayersApi>();
            serviceCollection.AddSingleton<ReportsApi>();
            serviceCollection.AddSingleton<RootApi>();
            serviceCollection.AddSingleton<TagsApi>();
            serviceCollection.AddSingleton<UserProfileApi>();

            // Register V1.1 API implementations
            serviceCollection.AddSingleton<XtremeIdiots.Portal.RepositoryApiClient.V1_1.RootApi>();

            // Register V1 API interfaces using concrete implementations
            serviceCollection.AddSingleton<IAdminActionsApi>(provider => provider.GetRequiredService<AdminActionsApi>());
            serviceCollection.AddSingleton<IBanFileMonitorsApi>(provider => provider.GetRequiredService<BanFileMonitorsApi>());
            serviceCollection.AddSingleton<IChatMessagesApi>(provider => provider.GetRequiredService<ChatMessagesApi>());
            serviceCollection.AddSingleton<IDataMaintenanceApi>(provider => provider.GetRequiredService<DataMaintenanceApi>());
            serviceCollection.AddSingleton<IDemosApi>(provider => provider.GetRequiredService<DemosApi>());
            serviceCollection.AddSingleton<IGameServersApi>(provider => provider.GetRequiredService<GameServersApi>());
            serviceCollection.AddSingleton<IGameServersEventsApi>(provider => provider.GetRequiredService<GameServersEventsApi>());
            serviceCollection.AddSingleton<IGameServersStatsApi>(provider => provider.GetRequiredService<GameServersStatsApi>());
            serviceCollection.AddSingleton<IGameTrackerBannerApi>(provider => provider.GetRequiredService<GameTrackerBannerApi>());
            serviceCollection.AddSingleton<ILivePlayersApi>(provider => provider.GetRequiredService<LivePlayersApi>());
            serviceCollection.AddSingleton<IMapsApi>(provider => provider.GetRequiredService<MapsApi>());
            serviceCollection.AddSingleton<IMapPacksApi>(provider => provider.GetRequiredService<MapPacksApi>());
            serviceCollection.AddSingleton<IPlayerAnalyticsApi>(provider => provider.GetRequiredService<PlayerAnalyticsApi>());
            serviceCollection.AddSingleton<IPlayersApi>(provider => provider.GetRequiredService<PlayersApi>());
            serviceCollection.AddSingleton<IRecentPlayersApi>(provider => provider.GetRequiredService<RecentPlayersApi>());
            serviceCollection.AddSingleton<IReportsApi>(provider => provider.GetRequiredService<ReportsApi>());
            serviceCollection.AddSingleton<IRootApi>(provider => provider.GetRequiredService<RootApi>());
            serviceCollection.AddSingleton<ITagsApi>(provider => provider.GetRequiredService<TagsApi>());
            serviceCollection.AddSingleton<IUserProfileApi>(provider => provider.GetRequiredService<UserProfileApi>());

            // Register version selectors
            serviceCollection.AddSingleton<IVersionedAdminActionsApi, VersionedAdminActionsApi>();
            serviceCollection.AddSingleton<IVersionedBanFileMonitorsApi, VersionedBanFileMonitorsApi>();
            serviceCollection.AddSingleton<IVersionedChatMessagesApi, VersionedChatMessagesApi>();
            serviceCollection.AddSingleton<IVersionedDataMaintenanceApi, VersionedDataMaintenanceApi>();
            serviceCollection.AddSingleton<IVersionedDemosApi, VersionedDemosApi>();
            serviceCollection.AddSingleton<IVersionedGameServersApi, VersionedGameServersApi>();
            serviceCollection.AddSingleton<IVersionedGameServersEventsApi, VersionedGameServersEventsApi>();
            serviceCollection.AddSingleton<IVersionedGameServersStatsApi, VersionedGameServersStatsApi>();
            serviceCollection.AddSingleton<IVersionedGameTrackerBannerApi, VersionedGameTrackerBannerApi>();
            serviceCollection.AddSingleton<IVersionedLivePlayersApi, VersionedLivePlayersApi>();
            serviceCollection.AddSingleton<IVersionedMapsApi, VersionedMapsApi>();
            serviceCollection.AddSingleton<IVersionedMapPacksApi, VersionedMapPacksApi>();
            serviceCollection.AddSingleton<IVersionedPlayerAnalyticsApi, VersionedPlayerAnalyticsApi>();
            serviceCollection.AddSingleton<IVersionedPlayersApi, VersionedPlayersApi>();
            serviceCollection.AddSingleton<IVersionedRecentPlayersApi, VersionedRecentPlayersApi>();
            serviceCollection.AddSingleton<IVersionedReportsApi, VersionedReportsApi>();
            serviceCollection.AddSingleton<IVersionedTagsApi, VersionedTagsApi>();
            serviceCollection.AddSingleton<IVersionedUserProfileApi, VersionedUserProfileApi>();
            serviceCollection.AddSingleton<IVersionedRootApi>(provider =>
                new VersionedRootApi(
                    provider.GetRequiredService<RootApi>(),
                    provider.GetRequiredService<XtremeIdiots.Portal.RepositoryApiClient.V1_1.RootApi>()
                ));

            // Register the unified client
            serviceCollection.AddSingleton<IRepositoryApiClient, RepositoryApiClient>();
        }
    }
}
