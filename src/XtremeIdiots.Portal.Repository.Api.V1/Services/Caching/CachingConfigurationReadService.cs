using MX.Api.Abstractions;
using MX.Caching.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// Cache-aside decorator over <see cref="IConfigurationReadService"/>. Entries are stored
    /// with a 5-minute TTL and dual tags — one precise, one namespace-scoped — so a global
    /// upsert can invalidate every server's resolved entry for that namespace across all
    /// Repository API instances via the shared Table Storage tag index.
    /// </summary>
    public sealed class CachingConfigurationReadService : IConfigurationReadService
    {
        internal static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

        private readonly IConfigurationReadService inner;
        private readonly IMxCache cache;
        private readonly RepositoryCacheMetrics metrics;

        public CachingConfigurationReadService(IConfigurationReadService inner, IMxCache cache, RepositoryCacheMetrics metrics)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(metrics);
            this.inner = inner;
            this.cache = cache;
            this.metrics = metrics;
        }

        public async Task<ApiResult<ConfigurationDto>> GetServerConfigurationAsync(Guid gameServerId, string ns, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ns))
            {
                return await inner.GetServerConfigurationAsync(gameServerId, ns, cancellationToken).ConfigureAwait(false);
            }

            // Normalize the namespace (e.g. legacy alias "serverList" -> canonical) so cache
            // keys/tags stay aligned with those used by the write-path invalidator; otherwise
            // an alias-scoped entry would survive canonical-scoped invalidation until TTL.
            ns = NamespaceSchemaValidationRegistry.NormalizeNamespace(ns);

            var key = new CacheKey(RepositoryCacheKeys.SettingsServerKey(gameServerId, ns));

            var existing = await cache.TryGetAsync<ApiResult<ConfigurationDto>>(key, cancellationToken).ConfigureAwait(false);
            if (existing.Found)
            {
                metrics.RecordHit(RepositoryCacheKeys.SurfaceSettings);
                return existing.Value!;
            }

            metrics.RecordMiss(RepositoryCacheKeys.SurfaceSettings);
            var fetched = await inner.GetServerConfigurationAsync(gameServerId, ns, cancellationToken).ConfigureAwait(false);

            if (fetched.IsSuccess)
            {
                var policy = new CachePolicy
                {
                    Enabled = true,
                    Tier = CacheTier.Distributed,
                    Ttl = Ttl,
                    Tags = new[]
                    {
                        RepositoryCacheKeys.SettingsServerTag(gameServerId, ns),
                        RepositoryCacheKeys.SettingsNamespaceTag(ns)
                    }
                };
                await cache.SetAsync(key, fetched, policy, cancellationToken).ConfigureAwait(false);
            }

            return fetched;
        }

        public async Task<ApiResult<ConfigurationDto>> GetGlobalConfigurationAsync(string ns, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ns))
            {
                return await inner.GetGlobalConfigurationAsync(ns, cancellationToken).ConfigureAwait(false);
            }

            ns = NamespaceSchemaValidationRegistry.NormalizeNamespace(ns);

            var key = new CacheKey(RepositoryCacheKeys.SettingsGlobalKey(ns));

            var existing = await cache.TryGetAsync<ApiResult<ConfigurationDto>>(key, cancellationToken).ConfigureAwait(false);
            if (existing.Found)
            {
                metrics.RecordHit(RepositoryCacheKeys.SurfaceSettings);
                return existing.Value!;
            }

            metrics.RecordMiss(RepositoryCacheKeys.SurfaceSettings);
            var fetched = await inner.GetGlobalConfigurationAsync(ns, cancellationToken).ConfigureAwait(false);

            if (fetched.IsSuccess)
            {
                var policy = new CachePolicy
                {
                    Enabled = true,
                    Tier = CacheTier.Distributed,
                    Ttl = Ttl,
                    Tags = new[]
                    {
                        RepositoryCacheKeys.SettingsGlobalTag(ns),
                        RepositoryCacheKeys.SettingsNamespaceTag(ns)
                    }
                };
                await cache.SetAsync(key, fetched, policy, cancellationToken).ConfigureAwait(false);
            }

            return fetched;
        }
    }
}
