using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using MX.Api.Abstractions;
using MX.Api.Client.Caching;
using MX.Api.Client.Configuration;
using MX.Api.Client.Extensions;
using MX.Caching.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;
using XtremeIdiots.Portal.Repository.Api.Client.V1.Caching;

using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1
{
    /// <summary>
    /// Verifies the V1 API client's default cache policy set: exactly the methods declared as
    /// cacheable are registered as in-memory with the expected TTL, and every never-cache guard
    /// is present. Uses the real <c>AddDefaultCachePolicies</c> DI path so the tests exercise
    /// the same code that runs in production.
    /// </summary>
    public class RepositoryApiCacheDefaultsTests
    {
        private static IReadOnlyDictionary<MethodInfo, CachePolicy> PoliciesFor<TApi>(Action<CacheBuilder> configure)
            where TApi : class
        {
            var services = new ServiceCollection();
            services.AddDefaultCachePolicies<TApi>(configure);
            using var sp = services.BuildServiceProvider();
            var defaults = sp.GetRequiredService<DefaultCachePolicies<TApi>>();
            return defaults.Policies;
        }

        private static CachePolicy PolicyFor(IReadOnlyDictionary<MethodInfo, CachePolicy> policies, string methodName, int parameterCount)
        {
            var entry = policies.FirstOrDefault(kvp =>
                kvp.Key.Name == methodName &&
                kvp.Key.GetParameters().Length == parameterCount);
            Assert.NotNull(entry.Key);
            return entry.Value;
        }

        private static IEnumerable<KeyValuePair<MethodInfo, CachePolicy>> AllPoliciesForMethod(
            IReadOnlyDictionary<MethodInfo, CachePolicy> policies,
            string methodName)
        {
            return policies.Where(kvp => kvp.Key.Name == methodName);
        }

        [Fact]
        public void ConfigureGameServers_CachesGetGameServer_At60Seconds()
        {
            var policies = PoliciesFor<IGameServersApi>(RepositoryApiCacheDefaults.ConfigureGameServers);

            var policy = PolicyFor(policies, nameof(IGameServersApi.GetGameServer), parameterCount: 2);

            Assert.True(policy.Enabled);
            Assert.Equal(CacheTier.InProcess, policy.Tier);
            Assert.Equal(RepositoryApiCacheDefaults.GameServerTtl, policy.Ttl);
        }

        [Fact]
        public void ConfigureGameServers_CachesGetGameServers_At60Seconds()
        {
            var policies = PoliciesFor<IGameServersApi>(RepositoryApiCacheDefaults.ConfigureGameServers);

            var policy = PolicyFor(policies, nameof(IGameServersApi.GetGameServers), parameterCount: 7);

            Assert.True(policy.Enabled);
            Assert.Equal(CacheTier.InProcess, policy.Tier);
            Assert.Equal(RepositoryApiCacheDefaults.GameServerTtl, policy.Ttl);
        }

        [Fact]
        public void ConfigureGameServers_MutationsAreNotCached()
        {
            var policies = PoliciesFor<IGameServersApi>(RepositoryApiCacheDefaults.ConfigureGameServers);

            AssertNotCached(policies, nameof(IGameServersApi.CreateGameServer));
            AssertNotCached(policies, nameof(IGameServersApi.CreateGameServers));
            AssertNotCached(policies, nameof(IGameServersApi.UpdateGameServer));
            AssertNotCached(policies, nameof(IGameServersApi.UpdateGameServerOrder));
            AssertNotCached(policies, nameof(IGameServersApi.DeleteGameServer));
        }

        [Fact]
        public void ConfigureMaps_CachesBothGetMapOverloads_At10Minutes()
        {
            var policies = PoliciesFor<IMapsApi>(RepositoryApiCacheDefaults.ConfigureMaps);

            var getMapOverloads = AllPoliciesForMethod(policies, nameof(IMapsApi.GetMap)).ToList();
            Assert.Equal(2, getMapOverloads.Count);
            Assert.All(getMapOverloads, kvp =>
            {
                Assert.True(kvp.Value.Enabled);
                Assert.Equal(CacheTier.InProcess, kvp.Value.Tier);
                Assert.Equal(RepositoryApiCacheDefaults.MapTtl, kvp.Value.Ttl);
            });

            // Disambiguated overload signatures: (Guid, CancellationToken) and (GameType, string, CancellationToken).
            var idOverload = getMapOverloads.Single(kvp =>
                kvp.Key.GetParameters()[0].ParameterType == typeof(Guid));
            var nameOverload = getMapOverloads.Single(kvp =>
                kvp.Key.GetParameters()[0].ParameterType == typeof(GameType));
            Assert.NotSame(idOverload.Key, nameOverload.Key);
        }

        [Fact]
        public void ConfigureMaps_CachesGetMaps_At10Minutes()
        {
            var policies = PoliciesFor<IMapsApi>(RepositoryApiCacheDefaults.ConfigureMaps);

            var policy = PolicyFor(policies, nameof(IMapsApi.GetMaps), parameterCount: 8);

            Assert.True(policy.Enabled);
            Assert.Equal(CacheTier.InProcess, policy.Tier);
            Assert.Equal(RepositoryApiCacheDefaults.MapTtl, policy.Ttl);
        }

        [Fact]
        public void ConfigureMaps_MutationsAreNotCached()
        {
            var policies = PoliciesFor<IMapsApi>(RepositoryApiCacheDefaults.ConfigureMaps);

            AssertNotCached(policies, nameof(IMapsApi.CreateMap));
            AssertNotCached(policies, nameof(IMapsApi.CreateMaps));
            AssertNotCached(policies, nameof(IMapsApi.UpdateMap));
            AssertNotCached(policies, nameof(IMapsApi.UpdateMaps));
            AssertNotCached(policies, nameof(IMapsApi.DeleteMap));
            AssertNotCached(policies, nameof(IMapsApi.RebuildMapPopularity));
            AssertNotCached(policies, nameof(IMapsApi.UpsertMapVote));
            AssertNotCached(policies, nameof(IMapsApi.UpsertMapVotes));
            AssertNotCached(policies, nameof(IMapsApi.UpdateMapImage));
            AssertNotCached(policies, nameof(IMapsApi.ClearMapImage));
        }

        [Fact]
        public void ConfigureUserProfile_AllRegisteredMethodsAreNotCached()
        {
            var policies = PoliciesFor<IUserProfileApi>(RepositoryApiCacheDefaults.ConfigureUserProfile);

            Assert.NotEmpty(policies);
            Assert.All(policies, kvp => Assert.Same(CachePolicy.NotCached, kvp.Value));
        }

        [Fact]
        public void ConfigureUserProfile_CoversReadsWritesAndClaimSurfaces()
        {
            var policies = PoliciesFor<IUserProfileApi>(RepositoryApiCacheDefaults.ConfigureUserProfile);

            var methodNames = policies.Keys.Select(m => m.Name).ToHashSet();
            Assert.Contains(nameof(IUserProfileApi.GetUserProfile), methodNames);
            Assert.Contains(nameof(IUserProfileApi.GetUserProfileByIdentityId), methodNames);
            Assert.Contains(nameof(IUserProfileApi.GetUserProfileByXtremeIdiotsId), methodNames);
            Assert.Contains(nameof(IUserProfileApi.GetUserProfileByDemoAuthKey), methodNames);
            Assert.Contains(nameof(IUserProfileApi.GetUserProfiles), methodNames);
            Assert.Contains(nameof(IUserProfileApi.GetPermissionsReport), methodNames);
            Assert.Contains(nameof(IUserProfileApi.CreateUserProfile), methodNames);
            Assert.Contains(nameof(IUserProfileApi.CreateUserProfiles), methodNames);
            Assert.Contains(nameof(IUserProfileApi.UpdateUserProfile), methodNames);
            Assert.Contains(nameof(IUserProfileApi.UpdateUserProfiles), methodNames);
            Assert.Contains(nameof(IUserProfileApi.CreateUserProfileClaim), methodNames);
            Assert.Contains(nameof(IUserProfileApi.SetUserProfileClaims), methodNames);
            Assert.Contains(nameof(IUserProfileApi.DeleteUserProfileClaim), methodNames);
        }

        [Fact]
        public void ConfigureApiInfo_GetApiInfoIsNotCached()
        {
            var policies = PoliciesFor<IApiInfoApi>(RepositoryApiCacheDefaults.ConfigureApiInfo);

            var policy = PolicyFor(policies, nameof(IApiInfoApi.GetApiInfo), parameterCount: 1);
            Assert.Same(CachePolicy.NotCached, policy);
        }

        [Fact]
        public void ConfigureApiHealth_CheckHealthIsNotCached()
        {
            var policies = PoliciesFor<IApiHealthApi>(RepositoryApiCacheDefaults.ConfigureApiHealth);

            var policy = PolicyFor(policies, nameof(IApiHealthApi.CheckHealth), parameterCount: 1);
            Assert.Same(CachePolicy.NotCached, policy);
        }

        [Fact]
        public void GameServerAndMapDefaultsHaveNoStaticTags()
        {
            // MX.Api.Client 2.3.76 does not support dynamic tag expansion. Positive defaults must
            // not encode literal placeholder tags such as "gameserver:{id}" — see decorator layer.
            var gameServerPolicies = PoliciesFor<IGameServersApi>(RepositoryApiCacheDefaults.ConfigureGameServers);
            foreach (var (_, policy) in gameServerPolicies.Where(kvp => kvp.Value.Enabled))
            {
                Assert.Empty(policy.Tags);
            }

            var mapPolicies = PoliciesFor<IMapsApi>(RepositoryApiCacheDefaults.ConfigureMaps);
            foreach (var (_, policy) in mapPolicies.Where(kvp => kvp.Value.Enabled))
            {
                Assert.Empty(policy.Tags);
            }
        }

        private static void AssertNotCached(IReadOnlyDictionary<MethodInfo, CachePolicy> policies, string methodName)
        {
            var overloads = AllPoliciesForMethod(policies, methodName).ToList();
            Assert.NotEmpty(overloads);
            Assert.All(overloads, kvp => Assert.Same(CachePolicy.NotCached, kvp.Value));
        }
    }
}
