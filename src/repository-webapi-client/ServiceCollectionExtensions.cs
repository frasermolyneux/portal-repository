﻿using Microsoft.Extensions.DependencyInjection;

using MxIO.ApiClient.Extensions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApiClient.Api;

namespace XtremeIdiots.Portal.RepositoryApiClient
{
    public static class ServiceCollectionExtensions
    {
        public static void AddRepositoryApiClient(this IServiceCollection serviceCollection, Action<RepositoryApiClientOptions> configure)
        {
            serviceCollection.AddApiClient();

            serviceCollection.Configure(configure);

            serviceCollection.AddSingleton<IAdminActionsApi, AdminActionsApi>();
            serviceCollection.AddSingleton<IBanFileMonitorsApi, BanFileMonitorsApi>();
            serviceCollection.AddSingleton<IChatMessagesApi, ChatMessagesApi>();
            serviceCollection.AddSingleton<IDataMaintenanceApi, DataMaintenanceApi>();
            serviceCollection.AddSingleton<IDemosApi, DemosApi>();
            serviceCollection.AddSingleton<IGameServersApi, GameServersApi>();
            serviceCollection.AddSingleton<IGameServersEventsApi, GameServersEventsApi>();
            serviceCollection.AddSingleton<IGameServersStatsApi, GameServersStatsApi>();
            serviceCollection.AddSingleton<IGameTrackerBannerApi, GameTrackerBannerApi>();
            serviceCollection.AddSingleton<ILivePlayersApi, LivePlayersApi>();
            serviceCollection.AddSingleton<IMapsApi, MapsApi>();
            serviceCollection.AddSingleton<IMapPacksApi, MapPacksApi>();
            serviceCollection.AddSingleton<IPlayerAnalyticsApi, PlayerAnalyticsApi>();
            serviceCollection.AddSingleton<IPlayersApi, PlayersApi>();
            serviceCollection.AddSingleton<IRecentPlayersApi, RecentPlayersApi>();
            serviceCollection.AddSingleton<IReportsApi, ReportsApi>();
            serviceCollection.AddSingleton<IRootApi, RootApi>();
            serviceCollection.AddSingleton<IUserProfileApi, UserProfileApi>();

            serviceCollection.AddSingleton<IRepositoryApiClient, RepositoryApiClient>();
        }
    }
}