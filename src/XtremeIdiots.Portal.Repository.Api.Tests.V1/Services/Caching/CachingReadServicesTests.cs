using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using System.Net;

using MX.Api.Abstractions;
using MX.Caching.Testing;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.Api.V1.Services.Caching;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;

using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Services.Caching
{
    /// <summary>
    /// Cache-aside behavior tests for the read-service decorators. Uses the
    /// <see cref="FakeMxCache"/> from MX.Caching.Testing to prove repeated-hit and precise
    /// tag-eviction semantics without exercising Table Storage.
    /// </summary>
    public class CachingReadServicesTests
    {
        private static RepositoryCacheMetrics CreateMetrics() => new();

        private static ApiResult<T> Ok<T>(T value) => new(HttpStatusCode.OK, new ApiResponse<T>(value));

        // --- GameServer ---

        private sealed class CountingGameServerReadService : IGameServerReadService
        {
            public int CallCount { get; private set; }
            public Func<Guid, ApiResult<GameServerDto>>? Factory { get; set; }

            public Task<ApiResult<GameServerDto>> GetGameServerAsync(Guid gameServerId, CancellationToken cancellationToken)
            {
                CallCount++;
                var result = Factory?.Invoke(gameServerId) ?? Ok(new GameServerDto());
                return Task.FromResult(result);
            }
        }

        [Fact]
        public async Task CachingGameServerReadService_ReturnsCachedValue_OnRepeatedHit()
        {
            var inner = new CountingGameServerReadService();
            var cache = new FakeMxCache();
            var subject = new CachingGameServerReadService(inner, cache, CreateMetrics());
            var id = Guid.NewGuid();

            var first = await subject.GetGameServerAsync(id, CancellationToken.None);
            var second = await subject.GetGameServerAsync(id, CancellationToken.None);

            Assert.Equal(1, inner.CallCount);
            Assert.True(first.IsSuccess);
            Assert.True(second.IsSuccess);
        }

        [Fact]
        public async Task CachingGameServerReadService_DoesNotCache_UnsuccessfulResult()
        {
            var inner = new CountingGameServerReadService { Factory = _ => new ApiResult<GameServerDto>(HttpStatusCode.NotFound) };
            var cache = new FakeMxCache();
            var subject = new CachingGameServerReadService(inner, cache, CreateMetrics());
            var id = Guid.NewGuid();

            _ = await subject.GetGameServerAsync(id, CancellationToken.None);
            _ = await subject.GetGameServerAsync(id, CancellationToken.None);

            Assert.Equal(2, inner.CallCount);
        }

        [Fact]
        public async Task InvalidateGameServer_EvictsCachedEntry_ForThatId()
        {
            var inner = new CountingGameServerReadService();
            var cache = new FakeMxCache();
            var metrics = CreateMetrics();
            var subject = new CachingGameServerReadService(inner, cache, metrics);
            var invalidator = new RepositoryCacheInvalidator(cache, metrics);
            var id = Guid.NewGuid();

            _ = await subject.GetGameServerAsync(id, CancellationToken.None);
            await invalidator.InvalidateGameServerAsync(id, CancellationToken.None);
            _ = await subject.GetGameServerAsync(id, CancellationToken.None);

            Assert.Equal(2, inner.CallCount);
        }

        [Fact]
        public async Task InvalidateGameServer_DoesNotEvict_OtherIds()
        {
            var inner = new CountingGameServerReadService();
            var cache = new FakeMxCache();
            var metrics = CreateMetrics();
            var subject = new CachingGameServerReadService(inner, cache, metrics);
            var invalidator = new RepositoryCacheInvalidator(cache, metrics);
            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();

            _ = await subject.GetGameServerAsync(idA, CancellationToken.None);
            _ = await subject.GetGameServerAsync(idB, CancellationToken.None);
            await invalidator.InvalidateGameServerAsync(idA, CancellationToken.None);
            _ = await subject.GetGameServerAsync(idB, CancellationToken.None); // still cached

            Assert.Equal(2, inner.CallCount);
        }

        // --- Dashboard ---

        private sealed class CountingDashboardService : IDashboardService
        {
            public int SummaryCalls { get; private set; }
            public int LeaderboardCalls { get; private set; }
            public int ModerationCalls { get; private set; }
            public int UtilizationCalls { get; private set; }

            public Task<ApiResult<DashboardSummaryDto>> GetDashboardSummaryAsync(CancellationToken cancellationToken)
            { SummaryCalls++; return Task.FromResult(Ok(new DashboardSummaryDto())); }

            public Task<ApiResult<CollectionModel<AdminLeaderboardEntryDto>>> GetAdminLeaderboardAsync(int days, CancellationToken cancellationToken)
            { LeaderboardCalls++; return Task.FromResult(Ok(new CollectionModel<AdminLeaderboardEntryDto>())); }

            public Task<ApiResult<CollectionModel<ModerationTrendDataPointDto>>> GetModerationTrendAsync(int days, CancellationToken cancellationToken)
            { ModerationCalls++; return Task.FromResult(Ok(new CollectionModel<ModerationTrendDataPointDto>())); }

            public Task<ApiResult<ServerUtilizationCollectionDto>> GetServerUtilizationAsync(CancellationToken cancellationToken)
            { UtilizationCalls++; return Task.FromResult(Ok(new ServerUtilizationCollectionDto())); }
        }

        [Fact]
        public async Task CachingDashboardService_CachesEachAggregation_PerMetricAndWindow()
        {
            var inner = new CountingDashboardService();
            var cache = new FakeMxCache();
            var subject = new CachingDashboardService(inner, cache, CreateMetrics());

            _ = await subject.GetDashboardSummaryAsync(CancellationToken.None);
            _ = await subject.GetDashboardSummaryAsync(CancellationToken.None);
            _ = await subject.GetAdminLeaderboardAsync(30, CancellationToken.None);
            _ = await subject.GetAdminLeaderboardAsync(30, CancellationToken.None);
            _ = await subject.GetModerationTrendAsync(30, CancellationToken.None);
            _ = await subject.GetModerationTrendAsync(30, CancellationToken.None);
            _ = await subject.GetServerUtilizationAsync(CancellationToken.None);
            _ = await subject.GetServerUtilizationAsync(CancellationToken.None);

            Assert.Equal(1, inner.SummaryCalls);
            Assert.Equal(1, inner.LeaderboardCalls);
            Assert.Equal(1, inner.ModerationCalls);
            Assert.Equal(1, inner.UtilizationCalls);
        }

        [Fact]
        public async Task CachingDashboardService_DifferentWindows_KeyIndependently()
        {
            var inner = new CountingDashboardService();
            var cache = new FakeMxCache();
            var subject = new CachingDashboardService(inner, cache, CreateMetrics());

            _ = await subject.GetAdminLeaderboardAsync(30, CancellationToken.None);
            _ = await subject.GetAdminLeaderboardAsync(7, CancellationToken.None);
            _ = await subject.GetAdminLeaderboardAsync(30, CancellationToken.None); // hit
            _ = await subject.GetAdminLeaderboardAsync(7, CancellationToken.None);  // hit

            Assert.Equal(2, inner.LeaderboardCalls);
        }

        [Fact]
        public async Task InvalidateDashboard_EvictsAllAggregations()
        {
            var inner = new CountingDashboardService();
            var cache = new FakeMxCache();
            var metrics = CreateMetrics();
            var subject = new CachingDashboardService(inner, cache, metrics);
            var invalidator = new RepositoryCacheInvalidator(cache, metrics);

            _ = await subject.GetDashboardSummaryAsync(CancellationToken.None);
            _ = await subject.GetAdminLeaderboardAsync(30, CancellationToken.None);

            await invalidator.InvalidateDashboardAsync(CancellationToken.None);

            _ = await subject.GetDashboardSummaryAsync(CancellationToken.None);
            _ = await subject.GetAdminLeaderboardAsync(30, CancellationToken.None);

            Assert.Equal(2, inner.SummaryCalls);
            Assert.Equal(2, inner.LeaderboardCalls);
        }

        // --- Configurations ---

        private sealed class CountingConfigurationReadService : IConfigurationReadService
        {
            public int ServerCalls { get; private set; }
            public int GlobalCalls { get; private set; }

            public Task<ApiResult<ConfigurationDto>> GetServerConfigurationAsync(Guid gameServerId, string ns, CancellationToken cancellationToken)
            { ServerCalls++; return Task.FromResult(Ok(new ConfigurationDto())); }

            public Task<ApiResult<ConfigurationDto>> GetGlobalConfigurationAsync(string ns, CancellationToken cancellationToken)
            { GlobalCalls++; return Task.FromResult(Ok(new ConfigurationDto())); }
        }

        [Fact]
        public async Task CachingConfigurationReadService_CachesServer_ByIdAndNamespace()
        {
            var inner = new CountingConfigurationReadService();
            var cache = new FakeMxCache();
            var subject = new CachingConfigurationReadService(inner, cache, CreateMetrics());
            var id = Guid.NewGuid();

            _ = await subject.GetServerConfigurationAsync(id, "ftp", CancellationToken.None);
            _ = await subject.GetServerConfigurationAsync(id, "ftp", CancellationToken.None);
            _ = await subject.GetServerConfigurationAsync(id, "rcon", CancellationToken.None);

            Assert.Equal(2, inner.ServerCalls);
        }

        [Fact]
        public async Task InvalidateServerSettings_EvictsPreciseServerNamespace_ButNotOthers()
        {
            var inner = new CountingConfigurationReadService();
            var cache = new FakeMxCache();
            var metrics = CreateMetrics();
            var subject = new CachingConfigurationReadService(inner, cache, metrics);
            var invalidator = new RepositoryCacheInvalidator(cache, metrics);
            var id = Guid.NewGuid();

            _ = await subject.GetServerConfigurationAsync(id, "ftp", CancellationToken.None);
            _ = await subject.GetServerConfigurationAsync(id, "rcon", CancellationToken.None);

            await invalidator.InvalidateServerSettingsAsync(id, "ftp", CancellationToken.None);

            _ = await subject.GetServerConfigurationAsync(id, "ftp", CancellationToken.None); // miss
            _ = await subject.GetServerConfigurationAsync(id, "rcon", CancellationToken.None); // still hit

            Assert.Equal(3, inner.ServerCalls);
        }

        [Fact]
        public async Task InvalidateGlobalNamespace_EvictsGlobalAndAllServerEntries_ForThatNamespace()
        {
            var inner = new CountingConfigurationReadService();
            var cache = new FakeMxCache();
            var metrics = CreateMetrics();
            var subject = new CachingConfigurationReadService(inner, cache, metrics);
            var invalidator = new RepositoryCacheInvalidator(cache, metrics);

            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();

            _ = await subject.GetServerConfigurationAsync(idA, "agent", CancellationToken.None);
            _ = await subject.GetServerConfigurationAsync(idB, "agent", CancellationToken.None);
            _ = await subject.GetGlobalConfigurationAsync("agent", CancellationToken.None);
            _ = await subject.GetServerConfigurationAsync(idA, "banfiles", CancellationToken.None);

            await invalidator.InvalidateGlobalNamespaceAsync("agent", CancellationToken.None);

            _ = await subject.GetServerConfigurationAsync(idA, "agent", CancellationToken.None); // miss
            _ = await subject.GetServerConfigurationAsync(idB, "agent", CancellationToken.None); // miss
            _ = await subject.GetGlobalConfigurationAsync("agent", CancellationToken.None);      // miss
            _ = await subject.GetServerConfigurationAsync(idA, "banfiles", CancellationToken.None); // still hit

            Assert.Equal(5, inner.ServerCalls);
            Assert.Equal(2, inner.GlobalCalls);
        }

        [Fact]
        public async Task CachingConfigurationReadService_NormalizesLegacyAlias_SoInvalidationAffectsAliasEntries()
        {
            // The legacy alias "serverList" is normalized by the write-path invalidator to
            // the canonical namespace before it removes tags. If the caching decorator did
            // not also normalize, an alias-scoped entry would linger until TTL. This test
            // proves the decorator normalizes so a subsequent invalidation of the canonical
            // namespace evicts entries written via either name.
            var inner = new CountingConfigurationReadService();
            var cache = new FakeMxCache();
            var metrics = CreateMetrics();
            var subject = new CachingConfigurationReadService(inner, cache, metrics);
            var invalidator = new RepositoryCacheInvalidator(cache, metrics);
            var id = Guid.NewGuid();

            var canonical = NamespaceSchemaValidationRegistry.NormalizeNamespace("serverList");

            // Sanity: alias must differ from canonical for the test to be meaningful.
            Assert.NotEqual("serverList", canonical);

            _ = await subject.GetGlobalConfigurationAsync("serverList", CancellationToken.None); // miss, cached under canonical
            _ = await subject.GetServerConfigurationAsync(id, "serverList", CancellationToken.None); // miss, cached under canonical
            _ = await subject.GetGlobalConfigurationAsync(canonical, CancellationToken.None); // hit
            _ = await subject.GetServerConfigurationAsync(id, canonical, CancellationToken.None); // hit

            Assert.Equal(1, inner.GlobalCalls);
            Assert.Equal(1, inner.ServerCalls);

            await invalidator.InvalidateGlobalNamespaceAsync(canonical, CancellationToken.None);

            _ = await subject.GetGlobalConfigurationAsync("serverList", CancellationToken.None); // miss again
            _ = await subject.GetServerConfigurationAsync(id, "serverList", CancellationToken.None); // miss again

            Assert.Equal(2, inner.GlobalCalls);
            Assert.Equal(2, inner.ServerCalls);
        }

        [Fact]
        public async Task RepositoryCacheInvalidator_NormalizesLegacyAlias_SoAliasWritesEvictCanonicalTaggedReads()
        {
            // Symmetric to the previous test but from the opposite direction: a write path that
            // uses the legacy alias must still evict the canonical-tagged cache entries produced
            // by the read decorator, otherwise stale entries linger until TTL.
            var inner = new CountingConfigurationReadService();
            var cache = new FakeMxCache();
            var metrics = CreateMetrics();
            var subject = new CachingConfigurationReadService(inner, cache, metrics);
            var invalidator = new RepositoryCacheInvalidator(cache, metrics);
            var id = Guid.NewGuid();

            var canonical = NamespaceSchemaValidationRegistry.NormalizeNamespace("serverList");
            Assert.NotEqual("serverList", canonical);

            _ = await subject.GetServerConfigurationAsync(id, canonical, CancellationToken.None); // miss (canonical tag)
            _ = await subject.GetGlobalConfigurationAsync(canonical, CancellationToken.None); // miss (canonical tag)

            Assert.Equal(1, inner.ServerCalls);
            Assert.Equal(1, inner.GlobalCalls);

            // Invalidate via the alias — must be normalized inside the invalidator to hit the
            // canonical tag used by the reads above.
            await invalidator.InvalidateServerSettingsAsync(id, "serverList", CancellationToken.None);
            await invalidator.InvalidateGlobalNamespaceAsync("serverList", CancellationToken.None);

            _ = await subject.GetServerConfigurationAsync(id, canonical, CancellationToken.None); // miss again
            _ = await subject.GetGlobalConfigurationAsync(canonical, CancellationToken.None); // miss again

            Assert.Equal(2, inner.ServerCalls);
            Assert.Equal(2, inner.GlobalCalls);
        }
    }
}
