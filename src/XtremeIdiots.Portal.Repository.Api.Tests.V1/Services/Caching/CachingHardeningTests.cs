using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using MX.Api.Abstractions;
using MX.Caching.Abstractions;
using MX.Caching.Testing;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Api.V1.Serialization;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.Api.V1.Services.Caching;

using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Services.Caching
{
    /// <summary>
    /// Phase A and C hardening tests: serialisation round-trips, key versioning,
    /// collection caching, map caching, and cache resilience (fail-open).
    /// </summary>
    public class CachingHardeningTests
    {
        private static RepositoryCacheMetrics CreateMetrics() => new();

        private static ApiResult<T> Ok<T>(T value) => new(HttpStatusCode.OK, new ApiResponse<T>(value));

        // -----------------------------------------------------------------------
        // A1 — Serialization round-trip
        // -----------------------------------------------------------------------

        [Fact]
        public void NewtonsoftHybridCacheSerializer_RoundTrip_PreservesInternalSetterFields_GameServerDto()
        {
            // GameServerDto properties use `internal set` + [JsonProperty].
            // The STJ default would deserialize blank/default values; Newtonsoft must restore them.
            var serializer = new NewtonsoftHybridCacheSerializer<ApiResult<GameServerDto>>();

            var dto = new GameServerDto();
            // Use reflection to force-set internal properties for the round-trip assertion.
            var type = typeof(GameServerDto);
            type.GetProperty(nameof(GameServerDto.Title))!.SetValue(dto, "Test Server");
            type.GetProperty(nameof(GameServerDto.Hostname))!.SetValue(dto, "192.168.1.1");
            type.GetProperty(nameof(GameServerDto.QueryPort))!.SetValue(dto, 27015);
            type.GetProperty(nameof(GameServerDto.GameType))!.SetValue(dto, GameType.CallOfDuty4);

            var original = Ok(dto);

            // Serialize to bytes then deserialize back.
            var buffer = new ArrayBufferWriter<byte>();
            serializer.Serialize(original, buffer);
            var deserialized = serializer.Deserialize(new ReadOnlySequence<byte>(buffer.WrittenMemory));

            Assert.True(deserialized.IsSuccess);
            Assert.Equal("Test Server", deserialized.Result!.Data!.Title);
            Assert.Equal("192.168.1.1", deserialized.Result.Data.Hostname);
            Assert.Equal(27015, deserialized.Result.Data.QueryPort);
            Assert.Equal(GameType.CallOfDuty4, deserialized.Result.Data.GameType);
        }

        [Fact]
        public void NewtonsoftHybridCacheSerializer_RoundTrip_PreservesInternalSetterFields_ConfigurationDto()
        {
            var serializer = new NewtonsoftHybridCacheSerializer<ApiResult<ConfigurationDto>>();

            var dto = new ConfigurationDto();
            var type = typeof(ConfigurationDto);
            type.GetProperty(nameof(ConfigurationDto.Namespace))!.SetValue(dto, "ftp");
            type.GetProperty(nameof(ConfigurationDto.Configuration))!.SetValue(dto, /*lang=json,strict*/ "{\"host\":\"10.0.0.1\"}");

            var original = Ok(dto);

            var buffer = new ArrayBufferWriter<byte>();
            serializer.Serialize(original, buffer);
            var deserialized = serializer.Deserialize(new ReadOnlySequence<byte>(buffer.WrittenMemory));

            Assert.True(deserialized.IsSuccess);
            Assert.Equal("ftp", deserialized.Result!.Data!.Namespace);
            Assert.Equal(/*lang=json,strict*/ "{\"host\":\"10.0.0.1\"}", deserialized.Result.Data.Configuration);
        }

        [Fact]
        public void NewtonsoftHybridCacheSerializer_RoundTrip_PreservesInternalSetterFields_MapDto()
        {
            var serializer = new NewtonsoftHybridCacheSerializer<ApiResult<MapDto>>();

            var dto = new MapDto();
            var type = typeof(MapDto);
            type.GetProperty(nameof(MapDto.MapId))!.SetValue(dto, Guid.NewGuid());
            type.GetProperty(nameof(MapDto.MapName))!.SetValue(dto, "mp_crash");
            type.GetProperty(nameof(MapDto.GameType))!.SetValue(dto, GameType.CallOfDuty4);
            type.GetProperty(nameof(MapDto.TotalVotes))!.SetValue(dto, 42);

            var original = Ok(dto);

            var buffer = new ArrayBufferWriter<byte>();
            serializer.Serialize(original, buffer);
            var deserialized = serializer.Deserialize(new ReadOnlySequence<byte>(buffer.WrittenMemory));

            Assert.True(deserialized.IsSuccess);
            Assert.Equal("mp_crash", deserialized.Result!.Data!.MapName);
            Assert.Equal(GameType.CallOfDuty4, deserialized.Result.Data.GameType);
            Assert.Equal(42, deserialized.Result.Data.TotalVotes);
        }

        // -----------------------------------------------------------------------
        // A3 — Key versioning
        // -----------------------------------------------------------------------

        [Fact]
        public void RepositoryCacheKeys_AllKeys_ContainSchemaVersion()
        {
            var id = Guid.NewGuid();
            const string ns = "ftp";

            Assert.Contains(RepositoryCacheKeys.SchemaVersion, RepositoryCacheKeys.GameServerKey(id));
            Assert.Contains(RepositoryCacheKeys.SchemaVersion, RepositoryCacheKeys.DashboardKey("summary", "current"));
            Assert.Contains(RepositoryCacheKeys.SchemaVersion, RepositoryCacheKeys.SettingsServerKey(id, ns));
            Assert.Contains(RepositoryCacheKeys.SchemaVersion, RepositoryCacheKeys.SettingsGlobalKey(ns));
            Assert.Contains(RepositoryCacheKeys.SchemaVersion, RepositoryCacheKeys.SettingsServerCollectionKey(id));
            Assert.Contains(RepositoryCacheKeys.SchemaVersion, RepositoryCacheKeys.SettingsGlobalCollectionKey);
            Assert.Contains(RepositoryCacheKeys.SchemaVersion, RepositoryCacheKeys.MapByIdKey(id));
            Assert.Contains(RepositoryCacheKeys.SchemaVersion, RepositoryCacheKeys.MapByGameNameKey("CallOfDuty4", "mp_crash"));
            Assert.Contains(RepositoryCacheKeys.SchemaVersion, RepositoryCacheKeys.TagPlayerCountsKey);
        }

        // -----------------------------------------------------------------------
        // C1 — Configuration collection caching
        // -----------------------------------------------------------------------

        private sealed class CountingConfigService : IConfigurationReadService
        {
            public int ServerSingleCalls { get; private set; }
            public int GlobalSingleCalls { get; private set; }
            public int ServerCollCalls { get; private set; }
            public int GlobalCollCalls { get; private set; }

            public Task<ApiResult<ConfigurationDto>> GetServerConfigurationAsync(Guid gameServerId, string ns, CancellationToken ct)
            { ServerSingleCalls++; return Task.FromResult(Ok(new ConfigurationDto())); }

            public Task<ApiResult<ConfigurationDto>> GetGlobalConfigurationAsync(string ns, CancellationToken ct)
            { GlobalSingleCalls++; return Task.FromResult(Ok(new ConfigurationDto())); }

            public Task<ApiResult<CollectionModel<ConfigurationDto>>> GetServerConfigurationsAsync(Guid gameServerId, CancellationToken ct)
            { ServerCollCalls++; return Task.FromResult(Ok(new CollectionModel<ConfigurationDto>())); }

            public Task<ApiResult<CollectionModel<ConfigurationDto>>> GetGlobalConfigurationsAsync(CancellationToken ct)
            { GlobalCollCalls++; return Task.FromResult(Ok(new CollectionModel<ConfigurationDto>())); }
        }

        [Fact]
        public async Task CachingConfigReadService_CollectionReads_AreCached()
        {
            var inner = new CountingConfigService();
            var cache = new FakeMxCache();
            var subject = new CachingConfigurationReadService(inner, cache, CreateMetrics(), NullLogger<CachingConfigurationReadService>.Instance);
            var id = Guid.NewGuid();

            // Server collection
            _ = await subject.GetServerConfigurationsAsync(id, CancellationToken.None);
            _ = await subject.GetServerConfigurationsAsync(id, CancellationToken.None);
            Assert.Equal(1, inner.ServerCollCalls);

            // Global collection
            _ = await subject.GetGlobalConfigurationsAsync(CancellationToken.None);
            _ = await subject.GetGlobalConfigurationsAsync(CancellationToken.None);
            Assert.Equal(1, inner.GlobalCollCalls);
        }

        [Fact]
        public async Task InvalidateServerSettings_EvictsServerCollection_ThroughServerAllTag()
        {
            var inner = new CountingConfigService();
            var cache = new FakeMxCache();
            var metrics = CreateMetrics();
            var subject = new CachingConfigurationReadService(inner, cache, metrics, NullLogger<CachingConfigurationReadService>.Instance);
            var invalidator = new RepositoryCacheInvalidator(cache, metrics, NullLogger<RepositoryCacheInvalidator>.Instance);
            var id = Guid.NewGuid();

            // Populate server collection + single entry
            _ = await subject.GetServerConfigurationsAsync(id, CancellationToken.None);
            _ = await subject.GetServerConfigurationAsync(id, "ftp", CancellationToken.None);

            // Invalidate a single namespace — should also evict collection
            await invalidator.InvalidateServerSettingsAsync(id, "ftp", CancellationToken.None);

            // Both should miss now
            _ = await subject.GetServerConfigurationsAsync(id, CancellationToken.None);
            _ = await subject.GetServerConfigurationAsync(id, "ftp", CancellationToken.None);

            Assert.Equal(2, inner.ServerCollCalls);
            Assert.Equal(2, inner.ServerSingleCalls);
        }

        [Fact]
        public async Task InvalidateGlobalNamespace_EvictsGlobalCollection_ThroughGlobalAllTag()
        {
            var inner = new CountingConfigService();
            var cache = new FakeMxCache();
            var metrics = CreateMetrics();
            var subject = new CachingConfigurationReadService(inner, cache, metrics, NullLogger<CachingConfigurationReadService>.Instance);
            var invalidator = new RepositoryCacheInvalidator(cache, metrics, NullLogger<RepositoryCacheInvalidator>.Instance);

            // Populate global collection + single entry
            _ = await subject.GetGlobalConfigurationsAsync(CancellationToken.None);
            _ = await subject.GetGlobalConfigurationAsync("agent", CancellationToken.None);

            // Invalidate single namespace — must also evict collection
            await invalidator.InvalidateGlobalNamespaceAsync("agent", CancellationToken.None);

            // Both should miss now
            _ = await subject.GetGlobalConfigurationsAsync(CancellationToken.None);
            _ = await subject.GetGlobalConfigurationAsync("agent", CancellationToken.None);

            Assert.Equal(2, inner.GlobalCollCalls);
            Assert.Equal(2, inner.GlobalSingleCalls);
        }

        // -----------------------------------------------------------------------
        // C4 — Map caching
        // -----------------------------------------------------------------------

        private sealed class CountingMapService : IMapReadService
        {
            public int ByIdCalls { get; private set; }
            public int ByGameNameCalls { get; private set; }
            public Func<Guid, ApiResult<MapDto>>? ByIdFactory { get; set; }

            public Task<ApiResult<MapDto>> GetMapByIdAsync(Guid mapId, CancellationToken ct)
            { ByIdCalls++; var r = ByIdFactory?.Invoke(mapId) ?? Ok(new MapDto()); return Task.FromResult(r); }

            public Task<ApiResult<MapDto>> GetMapByGameTypeAndNameAsync(GameType gameType, string mapName, CancellationToken ct)
            { ByGameNameCalls++; return Task.FromResult(Ok(new MapDto())); }
        }

        [Fact]
        public async Task CachingMapReadService_ByIdRead_IsCached()
        {
            var inner = new CountingMapService();
            var cache = new FakeMxCache();
            var subject = new CachingMapReadService(inner, cache, CreateMetrics(), NullLogger<CachingMapReadService>.Instance);
            var id = Guid.NewGuid();

            _ = await subject.GetMapByIdAsync(id, CancellationToken.None);
            _ = await subject.GetMapByIdAsync(id, CancellationToken.None);

            Assert.Equal(1, inner.ByIdCalls);
        }

        [Fact]
        public async Task CachingMapReadService_DoesNotCache_NotFoundResult()
        {
            var inner = new CountingMapService { ByIdFactory = _ => new ApiResult<MapDto>(HttpStatusCode.NotFound) };
            var cache = new FakeMxCache();
            var subject = new CachingMapReadService(inner, cache, CreateMetrics(), NullLogger<CachingMapReadService>.Instance);
            var id = Guid.NewGuid();

            _ = await subject.GetMapByIdAsync(id, CancellationToken.None);
            _ = await subject.GetMapByIdAsync(id, CancellationToken.None);

            Assert.Equal(2, inner.ByIdCalls);
        }

        [Fact]
        public async Task CachingMapReadService_ByGameNameRead_IsCached()
        {
            var inner = new CountingMapService();
            var cache = new FakeMxCache();
            var subject = new CachingMapReadService(inner, cache, CreateMetrics(), NullLogger<CachingMapReadService>.Instance);

            _ = await subject.GetMapByGameTypeAndNameAsync(GameType.CallOfDuty4, "mp_crash", CancellationToken.None);
            _ = await subject.GetMapByGameTypeAndNameAsync(GameType.CallOfDuty4, "mp_crash", CancellationToken.None);

            Assert.Equal(1, inner.ByGameNameCalls);
        }

        [Fact]
        public async Task InvalidateMap_EvictsById_Entry()
        {
            var inner = new CountingMapService();
            var cache = new FakeMxCache();
            var metrics = CreateMetrics();
            var subject = new CachingMapReadService(inner, cache, metrics, NullLogger<CachingMapReadService>.Instance);
            var invalidator = new RepositoryCacheInvalidator(cache, metrics, NullLogger<RepositoryCacheInvalidator>.Instance);
            var id = Guid.NewGuid();

            _ = await subject.GetMapByIdAsync(id, CancellationToken.None);
            await invalidator.InvalidateMapAsync(id, CancellationToken.None);
            _ = await subject.GetMapByIdAsync(id, CancellationToken.None);

            Assert.Equal(2, inner.ByIdCalls);
        }

        // -----------------------------------------------------------------------
        // A5 — Cache resilience (fail-open)
        // -----------------------------------------------------------------------

        private sealed class ThrowingCache : IMxCache
        {
            public Task<T> GetOrCreateAsync<T>(CacheKey key, CachePolicy policy, Func<CancellationToken, ValueTask<T>> factory, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("Cache backend unavailable");

            public Task<CacheReadResult<T>> TryGetAsync<T>(CacheKey key, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("Cache backend unavailable");

            public Task SetAsync<T>(CacheKey key, T value, CachePolicy policy, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("Cache backend unavailable");

            public Task RemoveAsync(CacheKey key, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("Cache backend unavailable");

            public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("Cache backend unavailable");
        }

        [Fact]
        public async Task CachingGameServerReadService_CacheFailure_FallsBackToOrigin()
        {
            var inner = new CountingGameServerReadService();
            var subject = new CachingGameServerReadService(inner, new ThrowingCache(), CreateMetrics(), NullLogger<CachingGameServerReadService>.Instance);

            var result = await subject.GetGameServerAsync(Guid.NewGuid(), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, inner.CallCount);
        }

        [Fact]
        public async Task CachingConfigReadService_CacheFailure_FallsBackToOrigin()
        {
            var inner = new CountingConfigService();
            var subject = new CachingConfigurationReadService(inner, new ThrowingCache(), CreateMetrics(), NullLogger<CachingConfigurationReadService>.Instance);

            var result = await subject.GetServerConfigurationAsync(Guid.NewGuid(), "ftp", CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, inner.ServerSingleCalls);
        }

        [Fact]
        public async Task CachingMapReadService_CacheFailure_FallsBackToOrigin()
        {
            var inner = new CountingMapService();
            var subject = new CachingMapReadService(inner, new ThrowingCache(), CreateMetrics(), NullLogger<CachingMapReadService>.Instance);

            var result = await subject.GetMapByIdAsync(Guid.NewGuid(), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, inner.ByIdCalls);
        }

        [Fact]
        public async Task RepositoryCacheInvalidator_InvalidationFailure_DoesNotThrow()
        {
            // Invalidation failures must NOT propagate to the caller (DB write already succeeded).
            var invalidator = new RepositoryCacheInvalidator(new ThrowingCache(), CreateMetrics(), NullLogger<RepositoryCacheInvalidator>.Instance);
            var id = Guid.NewGuid();

            // None of these should throw despite cache being unavailable.
            await invalidator.InvalidateGameServerAsync(id, CancellationToken.None);
            await invalidator.InvalidateDashboardAsync(CancellationToken.None);
            await invalidator.InvalidateServerSettingsAsync(id, "ftp", CancellationToken.None);
            await invalidator.InvalidateGlobalNamespaceAsync("agent", CancellationToken.None);
            await invalidator.InvalidateMapAsync(id, CancellationToken.None);
            await invalidator.InvalidateTagPlayerCountsAsync(CancellationToken.None);
        }

        // -----------------------------------------------------------------------
        // Helpers shared with CountingGameServerReadService (re-declared locally)
        // -----------------------------------------------------------------------

        private sealed class CountingGameServerReadService : IGameServerReadService
        {
            public int CallCount { get; private set; }

            public Task<ApiResult<GameServerDto>> GetGameServerAsync(Guid gameServerId, CancellationToken ct)
            { CallCount++; return Task.FromResult(Ok(new GameServerDto())); }
        }
    }
}
