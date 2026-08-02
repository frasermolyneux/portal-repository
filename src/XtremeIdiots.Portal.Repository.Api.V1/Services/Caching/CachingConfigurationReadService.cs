using System.Diagnostics;

using MX.Api.Abstractions;
using MX.Caching.Abstractions;
using MX.Caching.TableStorage;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;

using Microsoft.Extensions.Logging;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// Cache-aside decorator over <see cref="IConfigurationReadService"/>. Single-namespace
    /// entries are stored with dual tags — one precise, one namespace-scoped — so a global
    /// upsert can invalidate every server's resolved entry for that namespace across all
    /// Repository API instances via the shared Table Storage tag index. Collection entries
    /// are tagged globally/per-server so any mutation to any namespace invalidates the
    /// corresponding collection read.
    /// </summary>
    /// <remarks>
    /// <b>Tiered policy:</b>
    /// <list type="bullet">
    ///   <item>L1 (in-process) TTL: 45 seconds — bounds stale exposure within a single instance.</item>
    ///   <item>L2 (distributed / Table Storage) TTL: 5 minutes — shared across all instances.</item>
    /// </list>
    /// Maximum bounded stale window = L1 TTL = 45 seconds after a cross-instance tag invalidation.
    /// </remarks>
    public sealed class CachingConfigurationReadService : IConfigurationReadService
    {
        internal static readonly TimeSpan L1Ttl = TimeSpan.FromSeconds(45);
        internal static readonly TimeSpan L2Ttl = TimeSpan.FromMinutes(5);

        private sealed class FactoryAbortException<T>(ApiResult<T> result) : Exception
        {
            public ApiResult<T> Result { get; } = result;
        }

        private readonly IConfigurationReadService inner;
        private readonly IMxCache cache;
        private readonly RepositoryCacheMetrics metrics;
        private readonly ILogger<CachingConfigurationReadService> logger;

        // Sanitize user-supplied string values before including in log messages to prevent log injection (CWE-117).
        private static string SanitizeForLog(string value) =>
            value.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);

        public CachingConfigurationReadService(
            IConfigurationReadService inner,
            IMxCache cache,
            RepositoryCacheMetrics metrics,
            ILogger<CachingConfigurationReadService> logger)
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

        public async Task<ApiResult<ConfigurationDto>> GetServerConfigurationAsync(Guid gameServerId, string ns, CancellationToken cancellationToken)
        {
            // Defence-in-depth: mirror the inner service's rejection of oversized namespaces so
            // an oversized value never reaches key/tag construction. Callers going through
            // controllers already short-circuit on the same guard.
            if (string.IsNullOrWhiteSpace(ns) || ns.Length > 128)
            {
                return await inner.GetServerConfigurationAsync(gameServerId, ns, cancellationToken).ConfigureAwait(false);
            }

            // Normalize the namespace (e.g. legacy alias "serverList" -> canonical) so cache
            // keys/tags stay aligned with those used by the write-path invalidator.
            ns = NamespaceSchemaValidationRegistry.NormalizeNamespace(ns);

            var key = new CacheKey(RepositoryCacheKeys.SettingsServerKey(gameServerId, ns));
            var sw = Stopwatch.StartNew();

            var policy = new CachePolicy
            {
                Enabled = true,
                Tier = CacheTier.Tiered,
                L1Ttl = L1Ttl,
                Ttl = L2Ttl,
                Tags = new[]
                {
                    RepositoryCacheKeys.SettingsServerTag(gameServerId, ns),
                    RepositoryCacheKeys.SettingsNamespaceTag(ns)
                }
            };

            try
            {
                var cached = await cache.TryGetAsync<ApiResult<ConfigurationDto>>(key, cancellationToken).ConfigureAwait(false);
                if (cached.Found)
                {
                    metrics.RecordHit(RepositoryCacheKeys.SurfaceSettings);
                    metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                    return cached.Value!;
                }

                var result = await cache.GetOrCreateAsync(
                    key,
                    policy,
                    async ct =>
                    {
                        var fetched = await inner.GetServerConfigurationAsync(gameServerId, ns, ct).ConfigureAwait(false);
                        if (!fetched.IsSuccess)
                        {
                            throw new FactoryAbortException<ConfigurationDto>(fetched);
                        }
                        return fetched;
                    },
                    cancellationToken).ConfigureAwait(false);

                metrics.RecordMiss(RepositoryCacheKeys.SurfaceSettings);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                return result;
            }
            catch (FactoryAbortException<ConfigurationDto> ex)
            {
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                return ex.Result;
            }
            catch (CacheValueTooLargeException ex)
            {
                logger.LogWarning(
                    ex,
                    "Cache value too large for server config {GameServerId}/{Ns} ({ValueLength} bytes, max {MaximumLength}); skipping cache write.",
                    gameServerId, SanitizeForLog(ns), ex.ValueLength, ex.MaximumLength);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceSettings, "oversize");
                return await inner.GetServerConfigurationAsync(gameServerId, ns, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache read failed for server config {GameServerId}/{Ns}; falling back to origin.", gameServerId, SanitizeForLog(ns));
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceSettings, "read");
                return await inner.GetServerConfigurationAsync(gameServerId, ns, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<ApiResult<ConfigurationDto>> GetGlobalConfigurationAsync(string ns, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ns) || ns.Length > 128)
            {
                return await inner.GetGlobalConfigurationAsync(ns, cancellationToken).ConfigureAwait(false);
            }

            ns = NamespaceSchemaValidationRegistry.NormalizeNamespace(ns);

            var key = new CacheKey(RepositoryCacheKeys.SettingsGlobalKey(ns));
            var sw = Stopwatch.StartNew();

            var policy = new CachePolicy
            {
                Enabled = true,
                Tier = CacheTier.Tiered,
                L1Ttl = L1Ttl,
                Ttl = L2Ttl,
                Tags = new[]
                {
                    RepositoryCacheKeys.SettingsGlobalTag(ns),
                    RepositoryCacheKeys.SettingsNamespaceTag(ns)
                }
            };

            try
            {
                var cached = await cache.TryGetAsync<ApiResult<ConfigurationDto>>(key, cancellationToken).ConfigureAwait(false);
                if (cached.Found)
                {
                    metrics.RecordHit(RepositoryCacheKeys.SurfaceSettings);
                    metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                    return cached.Value!;
                }

                var result = await cache.GetOrCreateAsync(
                    key,
                    policy,
                    async ct =>
                    {
                        var fetched = await inner.GetGlobalConfigurationAsync(ns, ct).ConfigureAwait(false);
                        if (!fetched.IsSuccess)
                        {
                            throw new FactoryAbortException<ConfigurationDto>(fetched);
                        }
                        return fetched;
                    },
                    cancellationToken).ConfigureAwait(false);

                metrics.RecordMiss(RepositoryCacheKeys.SurfaceSettings);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                return result;
            }
            catch (FactoryAbortException<ConfigurationDto> ex)
            {
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                return ex.Result;
            }
            catch (CacheValueTooLargeException ex)
            {
                logger.LogWarning(
                    ex,
                    "Cache value too large for global config {Ns} ({ValueLength} bytes, max {MaximumLength}); skipping cache write.",
                    SanitizeForLog(ns), ex.ValueLength, ex.MaximumLength);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceSettings, "oversize");
                return await inner.GetGlobalConfigurationAsync(ns, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache read failed for global config {Ns}; falling back to origin.", SanitizeForLog(ns));
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceSettings, "read");
                return await inner.GetGlobalConfigurationAsync(ns, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<ApiResult<CollectionModel<ConfigurationDto>>> GetServerConfigurationsAsync(Guid gameServerId, CancellationToken cancellationToken)
        {
            var key = new CacheKey(RepositoryCacheKeys.SettingsServerCollectionKey(gameServerId));
            var sw = Stopwatch.StartNew();

            var policy = new CachePolicy
            {
                Enabled = true,
                Tier = CacheTier.Tiered,
                L1Ttl = L1Ttl,
                Ttl = L2Ttl,
                Tags = new[]
                {
                    RepositoryCacheKeys.SettingsServerAllTag(gameServerId)
                }
            };

            try
            {
                var cached = await cache.TryGetAsync<ApiResult<CollectionModel<ConfigurationDto>>>(key, cancellationToken).ConfigureAwait(false);
                if (cached.Found)
                {
                    metrics.RecordHit(RepositoryCacheKeys.SurfaceSettings);
                    metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                    return cached.Value!;
                }

                var result = await cache.GetOrCreateAsync(
                    key,
                    policy,
                    async ct =>
                    {
                        var fetched = await inner.GetServerConfigurationsAsync(gameServerId, ct).ConfigureAwait(false);
                        if (!fetched.IsSuccess)
                        {
                            throw new FactoryAbortException<CollectionModel<ConfigurationDto>>(fetched);
                        }
                        return fetched;
                    },
                    cancellationToken).ConfigureAwait(false);

                metrics.RecordMiss(RepositoryCacheKeys.SurfaceSettings);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                return result;
            }
            catch (FactoryAbortException<CollectionModel<ConfigurationDto>> ex)
            {
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                return ex.Result;
            }
            catch (CacheValueTooLargeException ex)
            {
                logger.LogWarning(
                    ex,
                    "Cache value too large for server config collection {GameServerId} ({ValueLength} bytes, max {MaximumLength}); skipping cache write.",
                    gameServerId, ex.ValueLength, ex.MaximumLength);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceSettings, "oversize");
                return await inner.GetServerConfigurationsAsync(gameServerId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache read failed for server config collection {GameServerId}; falling back to origin.", gameServerId);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceSettings, "read");
                return await inner.GetServerConfigurationsAsync(gameServerId, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<ApiResult<CollectionModel<ConfigurationDto>>> GetGlobalConfigurationsAsync(CancellationToken cancellationToken)
        {
            var key = new CacheKey(RepositoryCacheKeys.SettingsGlobalCollectionKey);
            var sw = Stopwatch.StartNew();

            var policy = new CachePolicy
            {
                Enabled = true,
                Tier = CacheTier.Tiered,
                L1Ttl = L1Ttl,
                Ttl = L2Ttl,
                Tags = new[] { RepositoryCacheKeys.SettingsGlobalAllTag }
            };

            try
            {
                var cached = await cache.TryGetAsync<ApiResult<CollectionModel<ConfigurationDto>>>(key, cancellationToken).ConfigureAwait(false);
                if (cached.Found)
                {
                    metrics.RecordHit(RepositoryCacheKeys.SurfaceSettings);
                    metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                    return cached.Value!;
                }

                var result = await cache.GetOrCreateAsync(
                    key,
                    policy,
                    async ct =>
                    {
                        var fetched = await inner.GetGlobalConfigurationsAsync(ct).ConfigureAwait(false);
                        return fetched;
                    },
                    cancellationToken).ConfigureAwait(false);

                metrics.RecordMiss(RepositoryCacheKeys.SurfaceSettings);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                return result;
            }
            catch (CacheValueTooLargeException ex)
            {
                logger.LogWarning(
                    ex,
                    "Cache value too large for global config collection ({ValueLength} bytes, max {MaximumLength}); skipping cache write.",
                    ex.ValueLength, ex.MaximumLength);
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceSettings, "oversize");
                return await inner.GetGlobalConfigurationsAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Cache read failed for global config collection; falling back to origin.");
                metrics.RecordLatency(RepositoryCacheKeys.SurfaceSettings, sw.Elapsed.TotalMilliseconds);
                metrics.RecordFailure(RepositoryCacheKeys.SurfaceSettings, "read");
                return await inner.GetGlobalConfigurationsAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
