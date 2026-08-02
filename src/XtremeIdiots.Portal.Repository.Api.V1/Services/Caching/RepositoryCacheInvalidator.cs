using MX.Caching.Abstractions;

using XtremeIdiots.Portal.Repository.Api.V1.Validation;

using Microsoft.Extensions.Logging;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// Default <see cref="IRepositoryCacheInvalidator"/> — dispatches tag invalidations to
    /// the shared <see cref="IMxCache"/> and records evictions on <see cref="RepositoryCacheMetrics"/>.
    /// </summary>
    /// <remarks>
    /// A5 — Invalidation failures are surfaced as structured warnings and reflected in metrics
    /// but must never fail the caller's response (the DB mutation already succeeded).
    /// </remarks>
    public sealed class RepositoryCacheInvalidator : IRepositoryCacheInvalidator
    {
        private readonly IMxCache cache;
        private readonly RepositoryCacheMetrics metrics;
        private readonly ILogger<RepositoryCacheInvalidator> logger;

        // Sanitize user-supplied string values before including in log messages to prevent log injection (CWE-117).
        private static string SanitizeForLog(string value) =>
            value.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);

        public RepositoryCacheInvalidator(IMxCache cache, RepositoryCacheMetrics metrics, ILogger<RepositoryCacheInvalidator> logger)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(metrics);
            ArgumentNullException.ThrowIfNull(logger);
            this.cache = cache;
            this.metrics = metrics;
            this.logger = logger;
        }

        public async Task InvalidateGameServerAsync(Guid gameServerId, CancellationToken cancellationToken)
        {
            var tag = RepositoryCacheKeys.GameServerTag(gameServerId);
            try
            {
                await cache.RemoveByTagAsync(tag, cancellationToken).ConfigureAwait(false);
                metrics.RecordEviction(RepositoryCacheKeys.SurfaceGameServer, tag);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache invalidation failed for game server {GameServerId}. Tag index may be stale until TTL.", gameServerId);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceGameServer, "evict");
            }
        }

        public async Task InvalidateDashboardAsync(CancellationToken cancellationToken)
        {
            try
            {
                await cache.RemoveByTagAsync(RepositoryCacheKeys.DashboardTag, cancellationToken).ConfigureAwait(false);
                metrics.RecordEviction(RepositoryCacheKeys.SurfaceDashboard, RepositoryCacheKeys.DashboardTag);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache invalidation failed for dashboard surface. Tag index may be stale until TTL.");
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceDashboard, "evict");
            }
        }

        public async Task InvalidateServerSettingsAsync(Guid gameServerId, string ns, CancellationToken cancellationToken)
        {
            // Normalize (e.g. legacy alias "serverList" -> canonical) so the tag matches the
            // one used by CachingConfigurationReadService on the read path.
            if (!string.IsNullOrWhiteSpace(ns))
            {
                ns = NamespaceSchemaValidationRegistry.NormalizeNamespace(ns);
            }

            // Invalidate the precise single-namespace entry.
            var singleTag = RepositoryCacheKeys.SettingsServerTag(gameServerId, ns);
            // Invalidate the server's collection entry (all-namespaces aggregate).
            var allTag = RepositoryCacheKeys.SettingsServerAllTag(gameServerId);

            try
            {
                await cache.RemoveByTagAsync(singleTag, cancellationToken).ConfigureAwait(false);
                metrics.RecordEviction(RepositoryCacheKeys.SurfaceSettings, singleTag);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache invalidation failed for server settings {GameServerId}/{Ns}. Tag index may be stale until TTL.", gameServerId, SanitizeForLog(ns));
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceSettings, "evict");
            }

            try
            {
                await cache.RemoveByTagAsync(allTag, cancellationToken).ConfigureAwait(false);
                metrics.RecordEviction(RepositoryCacheKeys.SurfaceSettings, allTag);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache invalidation failed for server settings collection {GameServerId}. Tag index may be stale until TTL.", gameServerId);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceSettings, "evict");
            }
        }

        public async Task InvalidateGlobalNamespaceAsync(string ns, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(ns))
            {
                ns = NamespaceSchemaValidationRegistry.NormalizeNamespace(ns);
            }

            var globalTag = RepositoryCacheKeys.SettingsGlobalTag(ns);
            var namespaceTag = RepositoryCacheKeys.SettingsNamespaceTag(ns);
            var globalAllTag = RepositoryCacheKeys.SettingsGlobalAllTag;

            foreach (var (tag, surface) in new[] {
                (globalTag, RepositoryCacheKeys.SurfaceSettings),
                (namespaceTag, RepositoryCacheKeys.SurfaceSettings),
                (globalAllTag, RepositoryCacheKeys.SurfaceSettings)
            })
            {
                try
                {
                    await cache.RemoveByTagAsync(tag, cancellationToken).ConfigureAwait(false);
                    metrics.RecordEviction(surface, tag);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Cache invalidation failed for global namespace {Ns} (tag {Tag}). Tag index may be stale until TTL.", SanitizeForLog(ns), SanitizeForLog(tag));
                    metrics.RecordFailure(surface, "evict");
                }
            }
        }

        public async Task InvalidateMapAsync(Guid mapId, CancellationToken cancellationToken)
        {
            var tag = RepositoryCacheKeys.MapTag(mapId);
            try
            {
                await cache.RemoveByTagAsync(tag, cancellationToken).ConfigureAwait(false);
                metrics.RecordEviction(RepositoryCacheKeys.SurfaceMap, tag);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache invalidation failed for map {MapId}. Tag index may be stale until TTL.", mapId);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceMap, "evict");
            }
        }

        public async Task InvalidateAllMapsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await cache.RemoveByTagAsync(RepositoryCacheKeys.MapAllTag, cancellationToken).ConfigureAwait(false);
                metrics.RecordEviction(RepositoryCacheKeys.SurfaceMap, RepositoryCacheKeys.MapAllTag);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache invalidation failed for all-maps tag. Map entries may be stale until TTL.");
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceMap, "evict");
            }
        }

        public async Task InvalidateTagPlayerCountsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await cache.RemoveByTagAsync(RepositoryCacheKeys.TagPlayerCountsTag, cancellationToken).ConfigureAwait(false);
                metrics.RecordEviction(RepositoryCacheKeys.SurfaceTags, RepositoryCacheKeys.TagPlayerCountsTag);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache invalidation failed for tag player counts. Tag index may be stale until TTL.");
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceTags, "evict");
            }
        }
    }
}
