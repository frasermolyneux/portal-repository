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

        /// <summary>Invalidates the cached resolved settings document for one server + namespace,
        /// and the server's collection entry (if any).</summary>
        Task InvalidateServerSettingsAsync(Guid gameServerId, string ns, CancellationToken cancellationToken = default);

        /// <summary>Invalidates the cached global settings document for a namespace, every
        /// server-resolved entry for that namespace, and both collection entries.</summary>
        Task InvalidateGlobalNamespaceAsync(string ns, CancellationToken cancellationToken = default);

        /// <summary>Invalidates all cached reads for a single map.</summary>
        Task InvalidateMapAsync(Guid mapId, CancellationToken cancellationToken = default);

        /// <summary>Evicts all cached map entries by the shared <c>map:all</c> tag.
        /// Use after bulk map mutations such as RebuildMapPopularity.</summary>
        Task InvalidateAllMapsAsync(CancellationToken cancellationToken = default);

        /// <summary>Invalidates the tag player-count aggregate.</summary>
        Task InvalidateTagPlayerCountsAsync(CancellationToken cancellationToken = default);
    }
}
