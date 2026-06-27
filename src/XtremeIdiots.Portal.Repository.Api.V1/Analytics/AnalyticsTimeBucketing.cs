using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;

namespace XtremeIdiots.Portal.Repository.Api.V1.Analytics;

/// <summary>
/// Shared time-bucketing helpers for analytics time series. Centralised here so every analytics
/// controller buckets timestamps identically and comparison overlays stay aligned.
/// </summary>
internal static class AnalyticsTimeBucketing
{
    /// <summary>
    /// Builds the ordered list of bucket start timestamps spanning <paramref name="fromUtc"/> (inclusive)
    /// to <paramref name="toUtc"/> (exclusive) for the requested <paramref name="bucket"/> granularity.
    /// </summary>
    public static List<DateTime> BuildBuckets(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket)
    {
        var result = new List<DateTime>();
        for (var cursor = Truncate(fromUtc, bucket); cursor < toUtc; cursor = Add(cursor, bucket))
        {
            result.Add(cursor);
        }

        return result;
    }

    /// <summary>
    /// Truncates a timestamp down to the start of its bucket for the requested granularity.
    /// </summary>
    public static DateTime Truncate(DateTime value, AnalyticsBucket bucket)
    {
        return bucket switch
        {
            AnalyticsBucket.FifteenMinutes => new DateTime(value.Year, value.Month, value.Day, value.Hour, (value.Minute / 15) * 15, 0, DateTimeKind.Utc),
            AnalyticsBucket.OneHour => new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0, DateTimeKind.Utc),
            _ => new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    /// <summary>
    /// Advances a bucket start to the next bucket start for the requested granularity.
    /// </summary>
    public static DateTime Add(DateTime value, AnalyticsBucket bucket)
    {
        return bucket switch
        {
            AnalyticsBucket.FifteenMinutes => value.AddMinutes(15),
            AnalyticsBucket.OneHour => value.AddHours(1),
            _ => value.AddDays(1)
        };
    }
}
