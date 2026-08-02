using System.Diagnostics;

using MX.Api.Abstractions;
using MX.Caching.Abstractions;
using MX.Caching.TableStorage;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;

using Microsoft.Extensions.Logging;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// Cache-aside decorator over <see cref="IMapReadService"/>. Successful single-map reads
    /// are stored with precise map-id and map-by-game-name keys so mutation-side controllers
    /// can evict via <see cref="IRepositoryCacheInvalidator.InvalidateMapAsync"/>.
    /// Non-success results (e.g. 404) are never cached.
    /// </summary>
    /// <remarks>
    /// <b>Tiered policy:</b>
    /// <list type="bullet">
    ///   <item>L1 (in-process) TTL: 15 seconds — bounds stale exposure within a single instance.</item>
    ///   <item>L2 (distributed / Table Storage) TTL: 5 minutes — shared across all instances.</item>
    /// </list>
    /// Maximum bounded stale window = L1 TTL = 15 seconds after a cross-instance tag invalidation.
    /// Mutation paths that MUST invalidate: create, update, delete, votes, popularity-rebuild,
    /// image update/clear.
    /// </remarks>
    public sealed class CachingMapReadService : IMapReadService
    {
        internal static readonly TimeSpan L1Ttl = TimeSpan.FromSeconds(15);
        internal static readonly TimeSpan L2Ttl = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Shorter L2 TTL applied to game+name entries that carry no per-map tag.
        /// Because <see cref="IRepositoryCacheInvalidator.InvalidateMapAsync"/> evicts by
        /// <c>MapTag(mapId)</c> which is NOT applied to game+name entries, those entries can
        /// only be bulk-evicted via <see cref="IRepositoryCacheInvalidator.InvalidateAllMapsAsync"/>.
        /// A tighter TTL bounds cross-instance staleness after per-map mutations.
        /// </summary>
        internal static readonly TimeSpan L2TtlGameName = TimeSpan.FromMinutes(2);

        // Sanitize user-supplied string values before including in log messages to prevent log injection (CWE-117).
        private static string SanitizeForLog(string value) =>
            value.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);

        private sealed class FactoryAbortException(ApiResult<MapDto> result) : Exception
        {
            public ApiResult<MapDto> Result { get; } = result;
        }

        private readonly IMapReadService inner;
        private readonly IMxCache cache;
        private readonly RepositoryCacheMetrics metrics;
        private readonly ILogger<CachingMapReadService> logger;

        public CachingMapReadService(
            IMapReadService inner,
            IMxCache cache,
            RepositoryCacheMetrics metrics,
            ILogger<CachingMapReadService> logger)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(metrics);
            ArgumentNullException.ThrowIfNull(logger);
            this.inner = inner;
            this.cache = cache;
            this.metrics = metrics;
            this.logger = logger;
        }

        public async Task<ApiResult<MapDto>> GetMapByIdAsync(Guid mapId, CancellationToken cancellationToken)
        {
            var key = new CacheKey(RepositoryCacheKeys.MapByIdKey(mapId));
            return await ExecuteAsync(
                key,
                RepositoryCacheKeys.MapTag(mapId),
                ct => inner.GetMapByIdAsync(mapId, ct),
                $"map-id:{mapId}",
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<ApiResult<MapDto>> GetMapByGameTypeAndNameAsync(GameType gameType, string mapName, CancellationToken cancellationToken)
        {
            var key = new CacheKey(RepositoryCacheKeys.MapByGameNameKey(gameType.ToString(), mapName));
            // We cannot use a stable mapId tag here because we don't know the mapId without
            // hitting the DB. This entry carries MapAllTag only — it is NOT tagged with
            // MapTag(mapId) and therefore will NOT be evicted by InvalidateMapAsync(mapId).
            // Only InvalidateAllMapsAsync (called after RebuildMapPopularity) evicts it.
            // A shorter L2 TTL (L2TtlGameName) limits cross-instance staleness after
            // per-map mutations such as rename, delete, and vote updates.
            return await ExecuteAsync(
                key,
                tag: null,  // no per-map tag; mapId is unknown at key-build time
                ct => inner.GetMapByGameTypeAndNameAsync(gameType, mapName, ct),
                $"map-game-name:{gameType}/{SanitizeForLog(mapName)}",
                cancellationToken,
                l2TtlOverride: L2TtlGameName).ConfigureAwait(false);
        }

        private async Task<ApiResult<MapDto>> ExecuteAsync(
            CacheKey key,
            string? tag,
            Func<CancellationToken, Task<ApiResult<MapDto>>> factory,
            string logContext,
            CancellationToken cancellationToken,
            TimeSpan? l2TtlOverride = null)
        {
            var sw = Stopwatch.StartNew();

            var policy = new CachePolicy
            {
                Enabled = true,
                Tier = CacheTier.Tiered,
                L1Ttl = L1Ttl,
                Ttl = l2TtlOverride ?? L2Ttl,
                Tags = tag != null ? new[] { tag, RepositoryCacheKeys.MapAllTag } : [RepositoryCacheKeys.MapAllTag]
            };

            try
            {
                var cached = await cache.TryGetAsync<ApiResult<MapDto>>(key, cancellationToken).ConfigureAwait(false);
                if (cached.Found)
                {
                    metrics.RecordHit(RepositoryCacheKeys.SurfaceMap);
                    metrics.RecordLatency(RepositoryCacheKeys.SurfaceMap, sw.Elapsed.TotalMilliseconds);
                    return cached.Value!;
                }

                var result = await cache.GetOrCreateAsync(
                    key,
                    policy,
                    async ct =>
                    {
                        var fetched = await factory(ct).ConfigureAwait(false);
                        if (!fetched.IsSuccess)
                        {
                            throw new FactoryAbortException(fetched);
                        }
                        return fetched;
                    },
                    cancellationToken).ConfigureAwait(false);

                metrics.RecordMiss(RepositoryCacheKeys.SurfaceMap);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceMap, sw.Elapsed.TotalMilliseconds);
                return result;
            }
            catch (FactoryAbortException ex)
            {
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceMap, sw.Elapsed.TotalMilliseconds);
                return ex.Result;
            }
            catch (CacheValueTooLargeException ex)
            {
                logger.LogWarning(
                    ex,
                    "Cache value too large for {MapContext} ({ValueLength} bytes, max {MaximumLength}); skipping cache write.",
                    logContext, ex.ValueLength, ex.MaximumLength);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceMap, sw.Elapsed.TotalMilliseconds);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceMap, "oversize");
                return await factory(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache read failed for {MapContext}; falling back to origin.", logContext);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceMap, sw.Elapsed.TotalMilliseconds);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceMap, "read");
                return await factory(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
