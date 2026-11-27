using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;

namespace XtremeIdiots.Portal.Repository.Api.V1.Analytics;

internal static class AnalyticsBucketCounts
{
    internal sealed class PlayerSample
    {
        public DateTime Timestamp { get; set; }
        public int PlayerCount { get; set; }
    }

    internal sealed record PlayerAggregate(double Average, int Peak, int SampleCount);

    internal sealed class PlayerObservation
    {
        public DateTime Timestamp { get; set; }
        public Guid PlayerId { get; set; }
    }

    internal sealed class BucketCount
    {
        public DateTime Date { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Count { get; set; }
    }

    internal sealed class BucketPlayers
    {
        public DateTime Date { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public double Average { get; set; }
        public int Peak { get; set; }
        public int SampleCount { get; set; }
    }

    public static async Task<Dictionary<DateTime, int>> CountAsync(
        IQueryable<DateTime> timestamps, AnalyticsBucket bucket, CancellationToken cancellationToken)
    {
        var counts = await CountQuery(timestamps, bucket).ToListAsync(cancellationToken).ConfigureAwait(false);
        return counts.ToDictionary(
            item => DateTime.SpecifyKind(item.Date.AddHours(item.Hour).AddMinutes(item.Minute), DateTimeKind.Utc),
            item => item.Count);
    }

    internal static IQueryable<BucketCount> CountQuery(IQueryable<DateTime> timestamps, AnalyticsBucket bucket)
    {
        var includeHour = bucket is AnalyticsBucket.OneHour or AnalyticsBucket.FifteenMinutes;
        var includeMinute = bucket == AnalyticsBucket.FifteenMinutes;
        return timestamps
            .GroupBy(timestamp => new
            {
                Date = timestamp.Date,
                Hour = includeHour ? timestamp.Hour : 0,
                Minute = includeMinute ? timestamp.Minute / 15 * 15 : 0
            })
            .Select(group => new BucketCount { Date = group.Key.Date, Hour = group.Key.Hour, Minute = group.Key.Minute, Count = group.Count() });
    }

    public static async Task<Dictionary<DateTime, PlayerAggregate>> AggregatePlayersAsync(
        IQueryable<PlayerSample> samples, AnalyticsBucket bucket, CancellationToken cancellationToken)
    {
        var aggregates = await PlayersQuery(samples, bucket).ToListAsync(cancellationToken).ConfigureAwait(false);
        return aggregates.ToDictionary(
            item => DateTime.SpecifyKind(item.Date.AddHours(item.Hour).AddMinutes(item.Minute), DateTimeKind.Utc),
            item => new PlayerAggregate(item.Average, item.Peak, item.SampleCount));
    }

    internal static IQueryable<BucketPlayers> PlayersQuery(IQueryable<PlayerSample> samples, AnalyticsBucket bucket)
    {
        var includeHour = bucket is AnalyticsBucket.OneHour or AnalyticsBucket.FifteenMinutes;
        var includeMinute = bucket == AnalyticsBucket.FifteenMinutes;
        return samples
            .GroupBy(sample => new
            {
                Date = sample.Timestamp.Date,
                Hour = includeHour ? sample.Timestamp.Hour : 0,
                Minute = includeMinute ? sample.Timestamp.Minute / 15 * 15 : 0
            })
            .Select(group => new BucketPlayers
            {
                Date = group.Key.Date,
                Hour = group.Key.Hour,
                Minute = group.Key.Minute,
                Average = group.Average(sample => (double)sample.PlayerCount),
                Peak = group.Max(sample => sample.PlayerCount),
                SampleCount = group.Count()
            });
    }

    public static async Task<Dictionary<DateTime, int>> CountDistinctPlayersAsync(
        IQueryable<PlayerObservation> observations, AnalyticsBucket bucket, CancellationToken cancellationToken)
    {
        var counts = await DistinctPlayersQuery(observations, bucket).ToListAsync(cancellationToken).ConfigureAwait(false);
        return counts.ToDictionary(
            item => DateTime.SpecifyKind(item.Date.AddHours(item.Hour).AddMinutes(item.Minute), DateTimeKind.Utc),
            item => item.Count);
    }

    internal static IQueryable<BucketCount> DistinctPlayersQuery(IQueryable<PlayerObservation> observations, AnalyticsBucket bucket)
    {
        var includeHour = bucket is AnalyticsBucket.OneHour or AnalyticsBucket.FifteenMinutes;
        var includeMinute = bucket == AnalyticsBucket.FifteenMinutes;
        return observations
            .GroupBy(observation => new
            {
                Date = observation.Timestamp.Date,
                Hour = includeHour ? observation.Timestamp.Hour : 0,
                Minute = includeMinute ? observation.Timestamp.Minute / 15 * 15 : 0
            })
            .Select(group => new BucketCount
            {
                Date = group.Key.Date,
                Hour = group.Key.Hour,
                Minute = group.Key.Minute,
                Count = group.Select(observation => observation.PlayerId).Distinct().Count()
            });
    }
}
