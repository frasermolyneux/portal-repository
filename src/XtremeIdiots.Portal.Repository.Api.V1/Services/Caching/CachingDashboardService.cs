using MX.Api.Abstractions;
using MX.Caching.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// Cache-aside decorator over <see cref="IDashboardService"/>. All four aggregations are
    /// stored with a 90-second TTL (mid-point of the 60–120s window in the work package) and
    /// tagged <c>dashboard</c> so any admin action mutation can evict the whole surface via
    /// <see cref="IRepositoryCacheInvalidator.InvalidateDashboardAsync"/>.
    /// </summary>
    public sealed class CachingDashboardService : IDashboardService
    {
        internal static readonly TimeSpan Ttl = TimeSpan.FromSeconds(90);

        private readonly IDashboardService inner;
        private readonly IMxCache cache;
        private readonly RepositoryCacheMetrics metrics;

        public CachingDashboardService(IDashboardService inner, IMxCache cache, RepositoryCacheMetrics metrics)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(metrics);
            this.inner = inner;
            this.cache = cache;
            this.metrics = metrics;
        }

        public Task<ApiResult<DashboardSummaryDto>> GetDashboardSummaryAsync(CancellationToken cancellationToken)
            => GetOrCreateAsync("summary", "current",
                (ct) => inner.GetDashboardSummaryAsync(ct),
                cancellationToken);

        public Task<ApiResult<CollectionModel<AdminLeaderboardEntryDto>>> GetAdminLeaderboardAsync(int days, CancellationToken cancellationToken)
            => GetOrCreateAsync("admin-leaderboard", DaysWindow(days),
                (ct) => inner.GetAdminLeaderboardAsync(days, ct),
                cancellationToken);

        public Task<ApiResult<CollectionModel<ModerationTrendDataPointDto>>> GetModerationTrendAsync(int days, CancellationToken cancellationToken)
            => GetOrCreateAsync("moderation-trend", DaysWindow(days),
                (ct) => inner.GetModerationTrendAsync(days, ct),
                cancellationToken);

        public Task<ApiResult<ServerUtilizationCollectionDto>> GetServerUtilizationAsync(CancellationToken cancellationToken)
            => GetOrCreateAsync("server-utilization", "24h",
                (ct) => inner.GetServerUtilizationAsync(ct),
                cancellationToken);

        private async Task<ApiResult<T>> GetOrCreateAsync<T>(string metric, string window, Func<CancellationToken, Task<ApiResult<T>>> factory, CancellationToken cancellationToken)
        {
            var key = new CacheKey(RepositoryCacheKeys.DashboardKey(metric, window));

            var existing = await cache.TryGetAsync<ApiResult<T>>(key, cancellationToken).ConfigureAwait(false);
            if (existing.Found)
            {
                metrics.RecordHit(RepositoryCacheKeys.SurfaceDashboard);
                return existing.Value!;
            }

            metrics.RecordMiss(RepositoryCacheKeys.SurfaceDashboard);
            var fetched = await factory(cancellationToken).ConfigureAwait(false);

            if (fetched.IsSuccess)
            {
                var policy = new CachePolicy
                {
                    Enabled = true,
                    Tier = CacheTier.Distributed,
                    Ttl = Ttl,
                    Tags = new[] { RepositoryCacheKeys.DashboardTag }
                };
                await cache.SetAsync(key, fetched, policy, cancellationToken).ConfigureAwait(false);
            }

            return fetched;
        }

        private static string DaysWindow(int days) => days <= 0 ? "30d" : $"{days}d";
    }
}
