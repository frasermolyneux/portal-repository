using System;
using System.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using MX.Api.Abstractions;
using MX.Api.Client.Configuration;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.Client.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1
{
    /// <summary>
    /// Startup-time DI resolution tests for <see cref="ServiceCollectionExtensions.AddRepositoryApiClient"/>.
    /// These tests exercise the full registration path so any regression in cache-policy scoping
    /// or per-typed-client wiring surfaces at build-provider / resolve time - the same way it does
    /// in production consumers.
    /// </summary>
    public class RepositoryClientRegistrationTests
    {
        private const string BaseUrl = "https://repo.example.local";
        private const string CachePartition = "unit-tests";
        private const string SubscriptionKey = "test-subscription-key";

        [Fact]
        public void AddRepositoryApiClient_WithLibraryCacheDefaults_ResolvesUnifiedClient()
        {
            using var provider = BuildProvider(o => o
                .WithBaseUrl(BaseUrl)
                .WithApiKeyAuthentication(SubscriptionKey)
                .WithCachePartition(CachePartition)
                .WithCaching(c => c.UseLibraryDefaults()));

            using var scope = provider.CreateScope();

            var client = scope.ServiceProvider.GetRequiredService<IRepositoryApiClient>();
            Assert.NotNull(client);
        }

        /// <summary>
        /// Repro for the production crash: enabling <c>UseLibraryDefaults()</c> registers the same
        /// consumer <c>configureOptions</c> delegate against every typed sub-API. Any typed client
        /// whose <c>SubApiInterface</c> does not match the expressions in the delegate causes
        /// <see cref="ArgumentException"/> at registration time.
        /// Resolving <c>IAdminActionsApi</c> should not throw.
        /// </summary>
        [Theory]
        [MemberData(nameof(AllSubApiInterfaces))]
        public void AddRepositoryApiClient_ResolvesEverySubApi(Type subApiInterface)
        {
            using var provider = BuildProvider(o => o
                .WithBaseUrl(BaseUrl)
                .WithApiKeyAuthentication(SubscriptionKey)
                .WithCachePartition(CachePartition)
                .WithCaching(c => c.UseLibraryDefaults()));

            using var scope = provider.CreateScope();

            var resolved = scope.ServiceProvider.GetService(subApiInterface);
            Assert.NotNull(resolved);
        }

        /// <summary>
        /// The consumer must be able to add a scope-specific override (e.g. a longer TTL for a
        /// single game-servers list call) on top of <c>UseLibraryDefaults()</c>. That expression
        /// targets a specific sub-API - it must not fail because the same delegate is replayed
        /// for other typed clients.
        /// </summary>
        [Fact]
        public void AddRepositoryApiClient_ConsumerOverrideForOneSubApi_DoesNotBleedIntoOthers()
        {
            using var provider = BuildProvider(o => o
                .WithBaseUrl(BaseUrl)
                .WithApiKeyAuthentication(SubscriptionKey)
                .WithCachePartition(CachePartition)
                .WithCaching(c => c
                    .UseLibraryDefaults()
                    .NotCached<IGameServersApi, Task<ApiResult<GameServerDto>>>(
                        x => x.GetGameServer(default, default))));

            using var scope = provider.CreateScope();

            Assert.NotNull(scope.ServiceProvider.GetService<IGameServersApi>());
            Assert.NotNull(scope.ServiceProvider.GetService<IAdminActionsApi>());
            Assert.NotNull(scope.ServiceProvider.GetService<IMapsApi>());
            Assert.NotNull(scope.ServiceProvider.GetService<IUserProfileApi>());
            Assert.NotNull(scope.ServiceProvider.GetService<IApiInfoApi>());
            Assert.NotNull(scope.ServiceProvider.GetService<IApiHealthApi>());
        }

        /// <summary>
        /// The scoping fix must land the consumer's expression only in the matching typed
        /// client's Options. Every sibling sub-API's Options should carry
        /// <c>UseLibraryCacheDefaults</c> but no cross-client policy operations.
        /// </summary>
        [Fact]
        public void AddRepositoryApiClient_ConsumerOverride_AppliesOnlyToMatchingTypedClient()
        {
            var services = new ServiceCollection();
            services.AddRepositoryApiClient(o => o
                .WithBaseUrl(BaseUrl)
                .WithApiKeyAuthentication(SubscriptionKey)
                .WithCachePartition(CachePartition)
                .WithCaching(c => c
                    .UseLibraryDefaults()
                    .NotCached<IGameServersApi, Task<ApiResult<GameServerDto>>>(
                        x => x.GetGameServer(default, default))));

            using var provider = services.BuildServiceProvider();

            var gameServersOptions = provider.GetRequiredService<RepositoryApiClientOptions>();
            Assert.True(gameServersOptions.UseLibraryCacheDefaults);

            // All RepositoryApiClientOptions instances are registered as singletons - one per typed client.
            // Confirm every typed client got its own options and only *some* have the override.
            var allOptionsInstances = services
                .Where(sd => sd.ServiceType == typeof(RepositoryApiClientOptions) && sd.ImplementationInstance is not null)
                .Select(sd => (RepositoryApiClientOptions)sd.ImplementationInstance!)
                .ToList();

            Assert.True(allOptionsInstances.Count > 5, "Expected one RepositoryApiClientOptions per typed sub-API registration.");

            // Every typed client should honour library defaults.
            Assert.All(allOptionsInstances, o => Assert.True(o.UseLibraryCacheDefaults));

            // Exactly one of the options instances should carry the consumer override targeting IGameServersApi.GetGameServer.
            var instancesWithOverride = allOptionsInstances
                .Where(o => o.CachePolicyOperations.Keys.Any(m =>
                    m.DeclaringType == typeof(IGameServersApi) && m.Name == nameof(IGameServersApi.GetGameServer)))
                .ToList();

            Assert.Single(instancesWithOverride);

            // No options instance should carry expressions targeting an interface it isn't for
            // - this is the crash guard: the override must not have been replayed against other typed clients.
            Assert.All(allOptionsInstances, o =>
            {
                foreach (var method in o.CachePolicyOperations.Keys)
                {
                    Assert.Equal(typeof(IGameServersApi), method.DeclaringType);
                }
            });
        }

        [Fact]
        public void AddRepositoryApiClient_MultipleConsumerOverridesAcrossSubApis_AllApplyToMatchingClientsOnly()
        {
            var services = new ServiceCollection();
            services.AddRepositoryApiClient(o => o
                .WithBaseUrl(BaseUrl)
                .WithApiKeyAuthentication(SubscriptionKey)
                .WithCachePartition(CachePartition)
                .WithCaching(c => c
                    .UseLibraryDefaults()
                    .NotCached<IGameServersApi, Task<ApiResult<GameServerDto>>>(
                        x => x.GetGameServer(default, default))
                    .NotCached<IAdminActionsApi, Task<ApiResult>>(
                        x => x.CreateAdminAction(default!, default))));

            using var provider = services.BuildServiceProvider();

            var allOptionsInstances = services
                .Where(sd => sd.ServiceType == typeof(RepositoryApiClientOptions) && sd.ImplementationInstance is not null)
                .Select(sd => (RepositoryApiClientOptions)sd.ImplementationInstance!)
                .ToList();

            // Each expression must land in exactly one typed client's Options.
            var gameServersOverrideCount = allOptionsInstances.Count(o => o.CachePolicyOperations.Keys.Any(m =>
                m.DeclaringType == typeof(IGameServersApi) && m.Name == nameof(IGameServersApi.GetGameServer)));
            Assert.Equal(1, gameServersOverrideCount);

            var adminActionsOverrideCount = allOptionsInstances.Count(o => o.CachePolicyOperations.Keys.Any(m =>
                m.DeclaringType == typeof(IAdminActionsApi)));
            Assert.Equal(1, adminActionsOverrideCount);
        }

        [Fact]
        public void AddRepositoryApiClient_WithoutCaching_ResolvesEverySubApi()
        {
            using var provider = BuildProvider(o => o
                .WithBaseUrl(BaseUrl)
                .WithApiKeyAuthentication(SubscriptionKey));

            using var scope = provider.CreateScope();

            foreach (var subApi in SubApiInterfaces())
            {
                var resolved = scope.ServiceProvider.GetService(subApi);
                Assert.NotNull(resolved);
            }
        }

        public static TheoryData<Type> AllSubApiInterfaces()
        {
            var data = new TheoryData<Type>();
            foreach (var type in SubApiInterfaces())
            {
                data.Add(type);
            }
            return data;
        }

        private static IEnumerable<Type> SubApiInterfaces() => new[]
        {
            typeof(IAdminActionsApi),
            typeof(IBanFileMonitorsApi),
            typeof(ICentralBanFileStatusApi),
            typeof(IChatMessagesApi),
            typeof(IDataMaintenanceApi),
            typeof(IDemosApi),
            typeof(IGameServersApi),
            typeof(IGameServersEventsApi),
            typeof(IGameServersStatsApi),
            typeof(IGameTrackerBannerApi),
            typeof(IMapsApi),
            typeof(IConnectedPlayersApi),
            typeof(IPlayerAnalyticsApi),
            typeof(IPlayersApi),
            typeof(IRecentPlayersApi),
            typeof(IReportsApi),
            typeof(ITagsApi),
            typeof(IUserProfileApi),
            typeof(IApiInfoApi),
            typeof(IApiHealthApi),
            typeof(INotificationTypesApi),
            typeof(INotificationPreferencesApi),
            typeof(INotificationsApi),
            typeof(IMapRotationsApi),
            typeof(IDashboardApi),
            typeof(IGlobalConfigurationsApi),
            typeof(IGameServerConfigurationsApi),
            typeof(ILiveStatusApi),
            typeof(IGlobalAnalyticsApi),
            typeof(IGameAnalyticsApi),
            typeof(IServerAnalyticsApi),
            typeof(IDashboardAnalyticsApi),
            typeof(IMapAnalyticsApi),
            typeof(IPlayerAnalyticsV2Api),
        };

        private static ServiceProvider BuildProvider(Action<RepositoryApiOptionsBuilder> configureOptions)
        {
            var services = new ServiceCollection();
            services.AddRepositoryApiClient(configureOptions);
            return services.BuildServiceProvider();
        }
    }
}
