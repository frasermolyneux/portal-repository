using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;

namespace XtremeIdiots.Portal.Repository.Api.V1.Validation;

internal static class AnalyticsQueryValidator
{
    public static bool TryValidateWindow(DateTime fromUtc, DateTime toUtc, out string error)
    {
        if (fromUtc >= toUtc)
        {
            error = "fromUtc must be earlier than toUtc.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidateTop(int top, out string error)
    {
        if (top < AnalyticsQueryDefaults.MinTop || top > AnalyticsQueryDefaults.MaxTop)
        {
            error = $"top must be between {AnalyticsQueryDefaults.MinTop} and {AnalyticsQueryDefaults.MaxTop}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidateBucketWindow(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, out string error)
    {
        if (!TryValidateWindow(fromUtc, toUtc, out error))
        {
            return false;
        }

        var range = toUtc - fromUtc;
        var maxDays = bucket switch
        {
            AnalyticsBucket.FifteenMinutes => AnalyticsQueryDefaults.FifteenMinuteMaxDays,
            AnalyticsBucket.OneHour => AnalyticsQueryDefaults.OneHourMaxDays,
            AnalyticsBucket.OneDay => AnalyticsQueryDefaults.OneDayMaxDays,
            _ => AnalyticsQueryDefaults.OneDayMaxDays
        };

        if (range > TimeSpan.FromDays(maxDays))
        {
            error = $"The selected bucket supports up to {maxDays} days per query window.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
