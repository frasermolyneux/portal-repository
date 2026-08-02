namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// High-level, surface-aware cache-invalidation seam used by mutation-side controllers
    /// so they don't need direct <see cref="MX.Caching.Abstractions.IMxCache"/> knowledge.
    /// Each invalidation resolves to <c>RemoveByTagAsync</c> against the shared cache backend
    /// so it takes effect across all Repository API instances via the tag index.
    /// </summary>
    public interface IRepositoryCacheInvalidator
    {
        /// <summary>Invalidates every cached read for a single game server.</summary>
        Task InvalidateGameServerAsync(Guid gameServerId, CancellationToken cancellationToken = default);

        /// <summary>Invalidates all cached dashboard aggregations.</summary>
        Task InvalidateDashboardAsync(CancellationToken cancellationToken = default);

        /// <summary>Invalidates the cached resolved settings document for one server + namespace.</summary>
        Task InvalidateServerSettingsAsync(Guid gameServerId, string ns, CancellationToken cancellationToken = default);

        /// <summary>Invalidates the cached global settings document for a namespace and every
        /// server-resolved entry for that namespace across all instances.</summary>
        Task InvalidateGlobalNamespaceAsync(string ns, CancellationToken cancellationToken = default);
    }
}
