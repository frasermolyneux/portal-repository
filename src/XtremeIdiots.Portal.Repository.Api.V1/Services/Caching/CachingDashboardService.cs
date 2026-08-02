using System.Diagnostics;

using MX.Api.Abstractions;
using MX.Caching.Abstractions;
using MX.Caching.TableStorage;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;

using Microsoft.Extensions.Logging;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// Cache-aside decorator over <see cref="IDashboardService"/>. All four aggregations are
    /// stored with Tiered policies and tagged <c>dashboard</c> so any admin action mutation
    /// can evict the whole surface via <see cref="IRepositoryCacheInvalidator.InvalidateDashboardAsync"/>.
    /// </summary>
    /// <remarks>
    /// <b>Tiered policy:</b>
    /// <list type="bullet">
    ///   <item>L1 (in-process) TTL: 22 seconds — bounds stale exposure within a single instance.</item>
    ///   <item>L2 (distributed / Table Storage) TTL: 90 seconds — shared across all instances.</item>
    /// </list>
    /// Maximum bounded stale window = L1 TTL = 22 seconds after a cross-instance tag invalidation.
    /// </remarks>
    public sealed class CachingDashboardService : IDashboardService
    {
        internal static readonly TimeSpan L1Ttl = TimeSpan.FromSeconds(22);
        internal static readonly TimeSpan L2Ttl = TimeSpan.FromSeconds(90);

        private sealed class FactoryAbortException<T>(ApiResult<T> result) : Exception
        {
            public ApiResult<T> Result { get; } = result;
        }

        private readonly IDashboardService inner;
        private readonly IMxCache cache;
        private readonly RepositoryCacheMetrics metrics;
        private readonly ILogger<CachingDashboardService> logger;

        public CachingDashboardService(
            IDashboardService inner,
            IMxCache cache,
            RepositoryCacheMetrics metrics,
            ILogger<CachingDashboardService> logger)
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

        public Task<ApiResult<DashboardSummaryDto>> GetDashboardSummaryAsync(CancellationToken cancellationToken)
            => GetOrCreateAsync("summary", "current",
                ct => inner.GetDashboardSummaryAsync(ct),
                cancellationToken);

        public Task<ApiResult<CollectionModel<AdminLeaderboardEntryDto>>> GetAdminLeaderboardAsync(int days, CancellationToken cancellationToken)
            => GetOrCreateAsync("admin-leaderboard", DaysWindow(days),
                ct => inner.GetAdminLeaderboardAsync(days, ct),
                cancellationToken);

        public Task<ApiResult<CollectionModel<ModerationTrendDataPointDto>>> GetModerationTrendAsync(int days, CancellationToken cancellationToken)
            => GetOrCreateAsync("moderation-trend", DaysWindow(days),
                ct => inner.GetModerationTrendAsync(days, ct),
                cancellationToken);

        public Task<ApiResult<ServerUtilizationCollectionDto>> GetServerUtilizationAsync(CancellationToken cancellationToken)
            => GetOrCreateAsync("server-utilization", "24h",
                ct => inner.GetServerUtilizationAsync(ct),
                cancellationToken);

        private async Task<ApiResult<T>> GetOrCreateAsync<T>(string metric, string window, Func<CancellationToken, Task<ApiResult<T>>> factory, CancellationToken cancellationToken)
        {
            var key = new CacheKey(RepositoryCacheKeys.DashboardKey(metric, window));
            var sw = Stopwatch.StartNew();

            var policy = new CachePolicy
            {
                Enabled = true,
                Tier = CacheTier.Tiered,
                L1Ttl = L1Ttl,
                Ttl = L2Ttl,
                Tags = new[] { RepositoryCacheKeys.DashboardTag }
            };

            try
            {
                var wasMiss = false;
                var result = await cache.GetOrCreateAsync(
                    key,
                    policy,
                    async ct =>
                    {
                        var fetched = await factory(ct).ConfigureAwait(false);
                        if (!fetched.IsSuccess)
                        {
                            throw new FactoryAbortException<T>(fetched);
                        }

                        wasMiss = true;
                        return fetched;
                    },
                    cancellationToken).ConfigureAwait(false);

                if (wasMiss)
                {
                    metrics.RecordMiss(RepositoryCacheKeys.SurfaceDashboard);
                }
                else
                {
                    metrics.RecordHit(RepositoryCacheKeys.SurfaceDashboard);
                }

                metrics.RecordLatency(RepositoryCacheKeys.SurfaceDashboard, sw.Elapsed.TotalMilliseconds);
                return result;
            }
            catch (FactoryAbortException<T> ex)
            {
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceDashboard, sw.Elapsed.TotalMilliseconds);
                return ex.Result;
            }
            catch (CacheValueTooLargeException ex)
            {
                logger.LogWarning(
                    ex,
                    "Cache value too large for dashboard {Metric}/{Window} ({ValueLength} bytes, max {MaximumLength}); skipping cache write.",
                    metric, window, ex.ValueLength, ex.MaximumLength);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceDashboard, "oversize");
                return await factory(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A5 — resilience: cache failure falls back to authoritative SQL path.
                logger.LogWarning(ex, "Cache read failed for dashboard {Metric}/{Window}; falling back to origin.", metric, window);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceDashboard, "read");
                return await factory(cancellationToken).ConfigureAwait(false);
            }
        }

        private static string DaysWindow(int days) => days <= 0 ? "30d" : $"{days}d";
    }
}
