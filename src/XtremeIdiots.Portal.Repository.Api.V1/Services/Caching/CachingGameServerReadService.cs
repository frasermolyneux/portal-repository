using System.Diagnostics;

using MX.Api.Abstractions;
using MX.Caching.Abstractions;
using MX.Caching.TableStorage;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

using Microsoft.Extensions.Logging;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// Cache-aside decorator over <see cref="IGameServerReadService"/>. Successful reads are
    /// stored under <c>repository:v1:gameserver:{id}</c> with a matching tag so mutation-side
    /// controllers can evict via <see cref="IRepositoryCacheInvalidator"/>.
    /// Non-success results (e.g. 404) are never cached to avoid pinning a stale negative.
    /// </summary>
    /// <remarks>
    /// <b>Tiered policy:</b>
    /// <list type="bullet">
    ///   <item>L1 (in-process) TTL: 12 seconds — bounds stale exposure within a single instance.</item>
    ///   <item>L2 (distributed / Table Storage) TTL: 60 seconds — shared across all instances.</item>
    /// </list>
    /// Maximum bounded stale window = L1 TTL = 12 seconds after a cross-instance tag invalidation.
    /// </remarks>
    public sealed class CachingGameServerReadService : IGameServerReadService
    {
        internal static readonly TimeSpan L1Ttl = TimeSpan.FromSeconds(12);
        internal static readonly TimeSpan L2Ttl = TimeSpan.FromSeconds(60);

        // Raised inside the GetOrCreateAsync factory when the origin returned a non-success
        // result; the factory exception prevents HybridCache from storing the response.
        private sealed class FactoryAbortException(ApiResult<GameServerDto> result) : Exception
        {
            public ApiResult<GameServerDto> Result { get; } = result;
        }

        private readonly IGameServerReadService inner;
        private readonly IMxCache cache;
        private readonly RepositoryCacheMetrics metrics;
        private readonly ILogger<CachingGameServerReadService> logger;

        public CachingGameServerReadService(
            IGameServerReadService inner,
            IMxCache cache,
            RepositoryCacheMetrics metrics,
            ILogger<CachingGameServerReadService> logger)
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

        public async Task<ApiResult<GameServerDto>> GetGameServerAsync(Guid gameServerId, CancellationToken cancellationToken)
        {
            var key = new CacheKey(RepositoryCacheKeys.GameServerKey(gameServerId));
            var tag = RepositoryCacheKeys.GameServerTag(gameServerId);
            var sw = Stopwatch.StartNew();

            var policy = new CachePolicy
            {
                Enabled = true,
                Tier = CacheTier.Tiered,
                L1Ttl = L1Ttl,
                Ttl = L2Ttl,
                Tags = new[] { tag }
            };

            try
            {
                var cached = await cache.TryGetAsync<ApiResult<GameServerDto>>(key, cancellationToken).ConfigureAwait(false);
                if (cached.Found)
                {
                    metrics.RecordHit(RepositoryCacheKeys.SurfaceGameServer);
                    metrics.RecordLatency(RepositoryCacheKeys.SurfaceGameServer, sw.Elapsed.TotalMilliseconds);
                    return cached.Value!;
                }

                var result = await cache.GetOrCreateAsync(
                    key,
                    policy,
                    async ct =>
                    {
                        var fetched = await inner.GetGameServerAsync(gameServerId, ct).ConfigureAwait(false);
                        if (!fetched.IsSuccess)
                        {
                            throw new FactoryAbortException(fetched);
                        }
                        return fetched;
                    },
                    cancellationToken).ConfigureAwait(false);

                metrics.RecordMiss(RepositoryCacheKeys.SurfaceGameServer);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceGameServer, sw.Elapsed.TotalMilliseconds);
                return result;
            }
            catch (FactoryAbortException ex)
            {
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceGameServer, sw.Elapsed.TotalMilliseconds);
                return ex.Result;
            }
            catch (CacheValueTooLargeException ex)
            {
                // A5 — oversize: the origin read succeeded but the value cannot be stored in L2.
                // Return the successfully fetched result after re-fetching from origin; do not 500.
                logger.LogWarning(
                    ex,
                    "Cache value too large for game server {GameServerId} ({ValueLength} bytes, max {MaximumLength}); skipping cache write.",
                    gameServerId, ex.ValueLength, ex.MaximumLength);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceGameServer, sw.Elapsed.TotalMilliseconds);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceGameServer, "oversize");
                return await inner.GetGameServerAsync(gameServerId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A5 — Cache resilience: read failure falls back to authoritative SQL path.
                logger.LogWarning(ex, "Cache read failed for game server {GameServerId}; falling back to origin.", gameServerId);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceGameServer, sw.Elapsed.TotalMilliseconds);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceGameServer, "read");
                return await inner.GetGameServerAsync(gameServerId, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
