using System.Diagnostics.Metrics;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// Owns the <see cref="Meter"/> and instrument definitions used by the repository-side
    /// cache-aside decorators. Metrics complement the built-in MX.Caching hit/miss/eviction
    /// counters by attributing behaviour to the specific repository surface being served
    /// (game server, dashboard, settings).
    /// </summary>
    public sealed class RepositoryCacheMetrics : IDisposable
    {
        /// <summary>Fully qualified meter name emitted to Application Insights / OpenTelemetry.</summary>
        public const string MeterName = "XtremeIdiots.Portal.Repository.Api.V1.Cache";

        private readonly Meter _meter;
        private readonly Counter<long> _hits;
        private readonly Counter<long> _misses;
        private readonly Counter<long> _evictions;
        private readonly Counter<long> _failures;
        private readonly Histogram<double> _latencyMs;

        public RepositoryCacheMetrics()
        {
            _meter = new Meter(MeterName, "1.0.0");
            _hits = _meter.CreateCounter<long>("repository_cache_hits_total", unit: "count", description: "Server-side cache-aside hits, tagged by surface.");
            _misses = _meter.CreateCounter<long>("repository_cache_misses_total", unit: "count", description: "Server-side cache-aside misses, tagged by surface.");
            _evictions = _meter.CreateCounter<long>("repository_cache_evictions_total", unit: "count", description: "Server-side cache-aside eviction requests (by tag), tagged by surface.");
            _failures = _meter.CreateCounter<long>("repository_cache_failures_total", unit: "count", description: "Server-side cache operation failures (read/write/evict), tagged by surface and operation.");
            _latencyMs = _meter.CreateHistogram<double>("repository_cache_latency_ms", unit: "ms", description: "Cache read latency in milliseconds from first request to response (hit or miss+origin fetch).");
        }

        /// <summary>Records a cache hit for the given surface (e.g. <c>gameserver</c>, <c>dashboard</c>, <c>settings</c>).</summary>
        public void RecordHit(string surface) => _hits.Add(1, new KeyValuePair<string, object?>("surface", surface));

        /// <summary>Records a cache miss for the given surface.</summary>
        public void RecordMiss(string surface) => _misses.Add(1, new KeyValuePair<string, object?>("surface", surface));

        /// <summary>Records an eviction / tag-invalidation for the given surface and tag.</summary>
        public void RecordEviction(string surface, string tag)
            => _evictions.Add(1,
                new KeyValuePair<string, object?>("surface", surface),
                new KeyValuePair<string, object?>("tag", tag));

        /// <summary>
        /// Records a cache operation failure (read fall-through, write error, or eviction error).
        /// Use <paramref name="operation"/> values: <c>read</c>, <c>write</c>, <c>evict</c>.
        /// </summary>
        public void RecordFailure(string surface, string operation)
            => _failures.Add(1,
                new KeyValuePair<string, object?>("surface", surface),
                new KeyValuePair<string, object?>("operation", operation));

        /// <summary>Records end-to-end response latency in milliseconds (from before cache read to after factory/cache write).</summary>
        public void RecordLatency(string surface, double milliseconds)
            => _latencyMs.Record(milliseconds, new KeyValuePair<string, object?>("surface", surface));

        public void Dispose() => _meter.Dispose();
    }
}
