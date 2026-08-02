namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// Central definitions of cache keys and tags used by repository-side cache-aside decorators.
    /// Keys and tags are stable strings so they survive process restarts and cross-instance tag
    /// eviction through the shared Table Storage <see cref="MX.Caching.Abstractions.ICacheTagIndex"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Key schema version: v1.</b>
    /// Bump <see cref="SchemaVersion"/> to <c>v2</c> whenever a cached DTO shape or key
    /// structure changes in a way that makes existing cache entries unreadable, forcing all
    /// instances to cold-miss and re-populate from the authoritative SQL path after deployment.
    /// </para>
    /// </remarks>
    public static class RepositoryCacheKeys
    {
        /// <summary>
        /// Schema version embedded in every cache key. Increment on breaking DTO/key changes.
        /// </summary>
        public const string SchemaVersion = "v1";

        // --- Surfaces (used as metric labels) ---
        public const string SurfaceGameServer = "gameserver";
        public const string SurfaceDashboard = "dashboard";
        public const string SurfaceSettings = "settings";
        public const string SurfaceMap = "map";
        public const string SurfaceTags = "tags";

        // --- Game server ---
        public static string GameServerKey(Guid gameServerId) => $"repository:{SchemaVersion}:gameserver:{gameServerId:N}";
        public static string GameServerTag(Guid gameServerId) => $"gameserver:{gameServerId:N}";

        // --- Dashboard aggregations ---
        public static string DashboardKey(string metric, string window) => $"repository:{SchemaVersion}:dashboard:{metric}:{window}";
        public const string DashboardTag = "dashboard";

        // --- Settings (single entry) ---
        public static string SettingsServerKey(Guid gameServerId, string ns) => $"repository:{SchemaVersion}:settings:{gameServerId:N}:{ns}";
        public static string SettingsGlobalKey(string ns) => $"repository:{SchemaVersion}:settings:global:{ns}";

        // --- Settings (collection entry) ---
        public static string SettingsServerCollectionKey(Guid gameServerId) => $"repository:{SchemaVersion}:settings:{gameServerId:N}:__collection";
        public const string SettingsGlobalCollectionKey = $"repository:{SchemaVersion}:settings:global:__collection";

        public static string SettingsServerTag(Guid gameServerId, string ns) => $"settings:server:{gameServerId:N}:{ns}";
        public static string SettingsGlobalTag(string ns) => $"settings:global:{ns}";

        /// <summary>Tag applied to every server-config entry for a given server.</summary>
        public static string SettingsServerAllTag(Guid gameServerId) => $"settings:server:{gameServerId:N}:all";

        /// <summary>Tag applied to every global-config entry.</summary>
        public const string SettingsGlobalAllTag = "settings:global:all";

        /// <summary>
        /// Cross-instance namespace tag applied to every server-resolved and global entry for a
        /// namespace. A global-namespace mutation invalidates every server's resolved entry for
        /// that namespace by evicting this tag.
        /// </summary>
        public static string SettingsNamespaceTag(string ns) => $"settings:ns:{ns}";

        // --- Maps ---
        public static string MapByIdKey(Guid mapId) => $"repository:{SchemaVersion}:map:{mapId:N}";
        public static string MapByGameNameKey(string gameType, string mapName) => $"repository:{SchemaVersion}:map:{gameType}:{mapName}";
        public static string MapTag(Guid mapId) => $"map:{mapId:N}";

        /// <summary>Tag applied to every cached map entry. Use for bulk eviction (e.g. after RebuildMapPopularity).</summary>
        public const string MapAllTag = "map:all";

        // --- Tag player counts ---
        public const string TagPlayerCountsKey = $"repository:{SchemaVersion}:tags:playercounts";
        public const string TagPlayerCountsTag = "tags:playercounts";
    }
}
