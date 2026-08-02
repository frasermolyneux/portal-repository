using MX.Caching.Abstractions;

using XtremeIdiots.Portal.Repository.Api.V1.Validation;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// Default <see cref="IRepositoryCacheInvalidator"/> — dispatches tag invalidations to
    /// the shared <see cref="IMxCache"/> and records evictions on <see cref="RepositoryCacheMetrics"/>.
    /// </summary>
    public sealed class RepositoryCacheInvalidator : IRepositoryCacheInvalidator
    {
        private readonly IMxCache cache;
        private readonly RepositoryCacheMetrics metrics;

        public RepositoryCacheInvalidator(IMxCache cache, RepositoryCacheMetrics metrics)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(metrics);
            this.cache = cache;
            this.metrics = metrics;
        }

        public async Task InvalidateGameServerAsync(Guid gameServerId, CancellationToken cancellationToken)
        {
            var tag = RepositoryCacheKeys.GameServerTag(gameServerId);
            await cache.RemoveByTagAsync(tag, cancellationToken).ConfigureAwait(false);
            metrics.RecordEviction(RepositoryCacheKeys.SurfaceGameServer, tag);
        }

        public async Task InvalidateDashboardAsync(CancellationToken cancellationToken)
        {
            await cache.RemoveByTagAsync(RepositoryCacheKeys.DashboardTag, cancellationToken).ConfigureAwait(false);
            metrics.RecordEviction(RepositoryCacheKeys.SurfaceDashboard, RepositoryCacheKeys.DashboardTag);
        }

        public async Task InvalidateServerSettingsAsync(Guid gameServerId, string ns, CancellationToken cancellationToken)
        {
            // Normalize (e.g. legacy alias "serverList" -> canonical) so the tag matches the
            // one used by CachingConfigurationReadService on the read path — otherwise an
            // alias-scoped write would leave canonical-tagged reads stale until TTL.
            if (!string.IsNullOrWhiteSpace(ns))
            {
                ns = NamespaceSchemaValidationRegistry.NormalizeNamespace(ns);
            }

            var tag = RepositoryCacheKeys.SettingsServerTag(gameServerId, ns);
            await cache.RemoveByTagAsync(tag, cancellationToken).ConfigureAwait(false);
            metrics.RecordEviction(RepositoryCacheKeys.SurfaceSettings, tag);
        }

        public async Task InvalidateGlobalNamespaceAsync(string ns, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(ns))
            {
                ns = NamespaceSchemaValidationRegistry.NormalizeNamespace(ns);
            }

            // Global tag removes the global-resolved entry; namespace tag also removes every
            // server-resolved entry for the namespace across all instances.
            var globalTag = RepositoryCacheKeys.SettingsGlobalTag(ns);
            var namespaceTag = RepositoryCacheKeys.SettingsNamespaceTag(ns);

            await cache.RemoveByTagAsync(globalTag, cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(namespaceTag, cancellationToken).ConfigureAwait(false);

            metrics.RecordEviction(RepositoryCacheKeys.SurfaceSettings, globalTag);
            metrics.RecordEviction(RepositoryCacheKeys.SurfaceSettings, namespaceTag);
        }
    }
}
