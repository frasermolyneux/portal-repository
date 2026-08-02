using MX.Api.Abstractions;
using MX.Caching.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// Cache-aside decorator over <see cref="IGameServerReadService"/>. Successful reads are
    /// stored under <c>gameserver:{id}</c> with a matching tag so mutation-side controllers can
    /// evict via <see cref="IRepositoryCacheInvalidator"/>. Non-success results (e.g. 404) are
    /// never cached to avoid pinning a stale negative.
    /// </summary>
    public sealed class CachingGameServerReadService : IGameServerReadService
    {
        internal static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

        private readonly IGameServerReadService inner;
        private readonly IMxCache cache;
        private readonly RepositoryCacheMetrics metrics;

        public CachingGameServerReadService(IGameServerReadService inner, IMxCache cache, RepositoryCacheMetrics metrics)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(metrics);
            this.inner = inner;
            this.cache = cache;
            this.metrics = metrics;
        }

        public async Task<ApiResult<GameServerDto>> GetGameServerAsync(Guid gameServerId, CancellationToken cancellationToken)
        {
            var key = new CacheKey(RepositoryCacheKeys.GameServerKey(gameServerId));
            var tag = RepositoryCacheKeys.GameServerTag(gameServerId);

            var existing = await cache.TryGetAsync<ApiResult<GameServerDto>>(key, cancellationToken).ConfigureAwait(false);
            if (existing.Found)
            {
                metrics.RecordHit(RepositoryCacheKeys.SurfaceGameServer);
                return existing.Value!;
            }

            metrics.RecordMiss(RepositoryCacheKeys.SurfaceGameServer);
            var fetched = await inner.GetGameServerAsync(gameServerId, cancellationToken).ConfigureAwait(false);

            // Only cache successful reads. Errors and 404s stay uncached so a subsequent create
            // is picked up immediately.
            if (fetched.IsSuccess)
            {
                var policy = new CachePolicy
                {
                    Enabled = true,
                    Tier = CacheTier.Distributed,
                    Ttl = Ttl,
                    Tags = new[] { tag }
                };

                await cache.SetAsync(key, fetched, policy, cancellationToken).ConfigureAwait(false);
            }

            return fetched;
        }
    }
}
