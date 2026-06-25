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
        services.RemoveAll<IVersionedConnectedPlayersApi>();
        services.RemoveAll<IVersionedScreenshotsApi>();

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
        services.RemoveAll<IConnectedPlayersApi>();
        services.RemoveAll<IScreenshotsApi>();

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

        services.AddSingleton(fakeClient.AdminActions);
        services.AddSingleton(fakeClient.BanFileMonitors);
        services.AddSingleton(fakeClient.ChatMessages);
        services.AddSingleton(fakeClient.DataMaintenance);
        services.AddSingleton(fakeClient.Demos);
        services.AddSingleton(fakeClient.GameServers);
        services.AddSingleton(fakeClient.GameServersEvents);
        services.AddSingleton(fakeClient.GameServersStats);
        services.AddSingleton(fakeClient.GameTrackerBanner);
        services.AddSingleton(fakeClient.Maps);
        services.AddSingleton(fakeClient.ConnectedPlayers);
        services.AddSingleton(fakeClient.Screenshots);

        services.AddSingleton(fakeClient.MapRotations);
        services.AddSingleton(fakeClient.Dashboard);
        services.AddSingleton(fakeClient.PlayerAnalytics);
        services.AddSingleton(fakeClient.Players);
        services.AddSingleton(fakeClient.RecentPlayers);
        services.AddSingleton(fakeClient.Reports);
        services.AddSingleton(fakeClient.Tags);
        services.AddSingleton(fakeClient.UserProfiles);
        services.AddSingleton(fakeClient.ApiHealth);
        services.AddSingleton(fakeClient.ApiInfo);
        services.AddSingleton(fakeClient.LiveStatus);
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
        services.AddSingleton<IConnectedPlayersApi>(fakeClient.ConnectedPlayersApi);
        services.AddSingleton<IScreenshotsApi>(fakeClient.ScreenshotsApi);

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
