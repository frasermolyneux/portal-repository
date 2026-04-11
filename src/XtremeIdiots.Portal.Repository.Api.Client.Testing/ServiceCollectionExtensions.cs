using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;
using XtremeIdiots.Portal.Repository.Api.Client.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing;

/// <summary>
/// DI extensions for registering fake repository services in integration tests.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the real <see cref="IRepositoryApiClient"/> and all related services
    /// with in-memory fakes. Use the optional <paramref name="configure"/> callback to
    /// set up canned responses.
    /// </summary>
    public static IServiceCollection AddFakeRepositoryApiClient(
        this IServiceCollection services,
        Action<FakeRepositoryApiClient>? configure = null)
    {
        var fakeClient = new FakeRepositoryApiClient();
        configure?.Invoke(fakeClient);

        // Remove all real registrations
        services.RemoveAll<IRepositoryApiClient>();

        // Remove all versioned API selectors
        services.RemoveAll<IVersionedAdminActionsApi>();
        services.RemoveAll<IVersionedBanFileMonitorsApi>();
        services.RemoveAll<IVersionedChatMessagesApi>();
        services.RemoveAll<IVersionedDataMaintenanceApi>();
        services.RemoveAll<IVersionedDemosApi>();
        services.RemoveAll<IVersionedGameServersApi>();
        services.RemoveAll<IVersionedGameServersEventsApi>();
        services.RemoveAll<IVersionedGameServersStatsApi>();
        services.RemoveAll<IVersionedGameTrackerBannerApi>();
        services.RemoveAll<IVersionedMapsApi>();

        services.RemoveAll<IVersionedMapRotationsApi>();
        services.RemoveAll<IVersionedDashboardApi>();
        services.RemoveAll<IVersionedPlayerAnalyticsApi>();
        services.RemoveAll<IVersionedPlayersApi>();
        services.RemoveAll<IVersionedRecentPlayersApi>();
        services.RemoveAll<IVersionedReportsApi>();
        services.RemoveAll<IVersionedTagsApi>();
        services.RemoveAll<IVersionedUserProfileApi>();
        services.RemoveAll<IVersionedApiHealthApi>();
        services.RemoveAll<IVersionedApiInfoApi>();
        services.RemoveAll<IApiHealthApi>();
        services.RemoveAll<IApiInfoApi>();
        services.RemoveAll<IVersionedLiveStatusApi>();

        // Remove all actual API implementations
        services.RemoveAll<IAdminActionsApi>();
        services.RemoveAll<IBanFileMonitorsApi>();
        services.RemoveAll<IChatMessagesApi>();
        services.RemoveAll<IDataMaintenanceApi>();
        services.RemoveAll<IDemosApi>();
        services.RemoveAll<IGameServersApi>();
        services.RemoveAll<IGameServersEventsApi>();
        services.RemoveAll<IGameServersSecretsApi>();
        services.RemoveAll<IGameServersStatsApi>();
        services.RemoveAll<IGameTrackerBannerApi>();
        services.RemoveAll<ILiveStatusApi>();

        services.RemoveAll<IMapRotationsApi>();
        services.RemoveAll<IDashboardApi>();
        services.RemoveAll<IMapsApi>();
        services.RemoveAll<IPlayerAnalyticsApi>();
        services.RemoveAll<IPlayersApi>();
        services.RemoveAll<IRecentPlayersApi>();
        services.RemoveAll<IReportsApi>();
        services.RemoveAll<ITagsApi>();
        services.RemoveAll<IUserProfileApi>();

        // Register fakes as singletons
        services.AddSingleton<IRepositoryApiClient>(fakeClient);

        services.AddSingleton<IVersionedAdminActionsApi>(fakeClient.AdminActions);
        services.AddSingleton<IVersionedBanFileMonitorsApi>(fakeClient.BanFileMonitors);
        services.AddSingleton<IVersionedChatMessagesApi>(fakeClient.ChatMessages);
        services.AddSingleton<IVersionedDataMaintenanceApi>(fakeClient.DataMaintenance);
        services.AddSingleton<IVersionedDemosApi>(fakeClient.Demos);
        services.AddSingleton<IVersionedGameServersApi>(fakeClient.GameServers);
        services.AddSingleton<IVersionedGameServersEventsApi>(fakeClient.GameServersEvents);
        services.AddSingleton<IVersionedGameServersStatsApi>(fakeClient.GameServersStats);
        services.AddSingleton<IVersionedGameTrackerBannerApi>(fakeClient.GameTrackerBanner);
        services.AddSingleton<IVersionedMapsApi>(fakeClient.Maps);

        services.AddSingleton<IVersionedMapRotationsApi>(fakeClient.MapRotations);
        services.AddSingleton<IVersionedDashboardApi>(fakeClient.Dashboard);
        services.AddSingleton<IVersionedPlayerAnalyticsApi>(fakeClient.PlayerAnalytics);
        services.AddSingleton<IVersionedPlayersApi>(fakeClient.Players);
        services.AddSingleton<IVersionedRecentPlayersApi>(fakeClient.RecentPlayers);
        services.AddSingleton<IVersionedReportsApi>(fakeClient.Reports);
        services.AddSingleton<IVersionedTagsApi>(fakeClient.Tags);
        services.AddSingleton<IVersionedUserProfileApi>(fakeClient.UserProfiles);
        services.AddSingleton<IVersionedApiHealthApi>(fakeClient.ApiHealth);
        services.AddSingleton<IVersionedApiInfoApi>(fakeClient.ApiInfo);
        services.AddSingleton<IVersionedLiveStatusApi>(fakeClient.LiveStatus);
        services.AddSingleton<IApiHealthApi>(fakeClient.HealthApi);
        services.AddSingleton<IApiInfoApi>(fakeClient.InfoApi);

        services.AddSingleton<IAdminActionsApi>(fakeClient.AdminActionsApi);
        services.AddSingleton<IBanFileMonitorsApi>(fakeClient.BanFileMonitorsApi);
        services.AddSingleton<IChatMessagesApi>(fakeClient.ChatMessagesApi);
        services.AddSingleton<IDataMaintenanceApi>(fakeClient.DataMaintenanceApi);
        services.AddSingleton<IDemosApi>(fakeClient.DemosApi);
        services.AddSingleton<IGameServersApi>(fakeClient.GameServersApi);
        services.AddSingleton<IGameServersEventsApi>(fakeClient.GameServersEventsApi);
        services.AddSingleton<IGameServersSecretsApi>(fakeClient.GameServersSecretsApi);
        services.AddSingleton<IGameServersStatsApi>(fakeClient.GameServersStatsApi);
        services.AddSingleton<IGameTrackerBannerApi>(fakeClient.GameTrackerBannerApi);
        services.AddSingleton<ILiveStatusApi>(fakeClient.LiveStatusApi);

        services.AddSingleton<IMapRotationsApi>(fakeClient.MapRotationsApi);
        services.AddSingleton<IDashboardApi>(fakeClient.DashboardApi);
        services.AddSingleton<IMapsApi>(fakeClient.MapsApi);
        services.AddSingleton<IPlayerAnalyticsApi>(fakeClient.PlayerAnalyticsApi);
        services.AddSingleton<IPlayersApi>(fakeClient.PlayersApi);
        services.AddSingleton<IRecentPlayersApi>(fakeClient.RecentPlayersApi);
        services.AddSingleton<IReportsApi>(fakeClient.ReportsApi);
        services.AddSingleton<ITagsApi>(fakeClient.TagsApi);
        services.AddSingleton<IUserProfileApi>(fakeClient.UserProfilesApi);

        return services;
    }
}
