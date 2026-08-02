namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// Central definitions of cache keys and tags used by repository-side cache-aside decorators.
    /// Keys and tags are stable strings so they survive process restarts and cross-instance tag
    /// eviction through the shared Table Storage <see cref="MX.Caching.Abstractions.ICacheTagIndex"/>.
    /// </summary>
    public static class RepositoryCacheKeys
    {
        // --- Surfaces (used as metric labels) ---
        public const string SurfaceGameServer = "gameserver";
        public const string SurfaceDashboard = "dashboard";
        public const string SurfaceSettings = "settings";

        // --- Game server ---
        public static string GameServerKey(Guid gameServerId) => $"gameserver:{gameServerId:N}";
        public static string GameServerTag(Guid gameServerId) => $"gameserver:{gameServerId:N}";

        // --- Dashboard aggregations ---
        public static string DashboardKey(string metric, string window) => $"dashboard:{metric}:{window}";
        public const string DashboardTag = "dashboard";

        // --- Settings ---
        public static string SettingsServerKey(Guid gameServerId, string ns) => $"settings:{gameServerId:N}:{ns}";
        public static string SettingsGlobalKey(string ns) => $"settings:global:{ns}";

        public static string SettingsServerTag(Guid gameServerId, string ns) => $"settings:server:{gameServerId:N}:{ns}";
        public static string SettingsGlobalTag(string ns) => $"settings:global:{ns}";

        /// <summary>
        /// Cross-instance namespace tag applied to every server-resolved and global entry for a
        /// namespace. A global-namespace mutation invalidates every server's resolved entry for
        /// that namespace by evicting this tag.
        /// </summary>
        public static string SettingsNamespaceTag(string ns) => $"settings:ns:{ns}";
    }
}
