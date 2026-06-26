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

    public static bool TryValidateComparePeriods(int comparePeriods, out string error)
    {
        if (comparePeriods < AnalyticsQueryDefaults.MinComparePeriods || comparePeriods > AnalyticsQueryDefaults.MaxComparePeriods)
        {
            error = $"comparePeriods must be between {AnalyticsQueryDefaults.MinComparePeriods} and {AnalyticsQueryDefaults.MaxComparePeriods}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidateComparisonOptions(
        AnalyticsCompareMode compareMode,
        int comparePeriods,
        AnalyticsAlignMode alignMode,
        string timezone,
        out string error)
    {
        if (string.IsNullOrWhiteSpace(timezone))
        {
            error = "timezone is required.";
            return false;
        }

        if (!TryValidateComparePeriods(comparePeriods, out error))
        {
            return false;
        }

        if (!Enum.IsDefined(compareMode))
        {
            error = "compareMode is invalid.";
            return false;
        }

        if (!Enum.IsDefined(alignMode))
        {
            error = "alignMode is invalid.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryGetAlignedWindow(
        DateTime fromUtc,
        DateTime toUtc,
        AnalyticsAlignMode alignMode,
        string timezone,
        out DateTime alignedFromUtc,
        out DateTime alignedToUtc,
        out string error)
    {
        alignedFromUtc = fromUtc;
        alignedToUtc = toUtc;

        if (!TryValidateWindow(fromUtc, toUtc, out error))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(timezone))
        {
            error = "timezone is required.";
            return false;
        }

        if (alignMode == AnalyticsAlignMode.None)
        {
            error = string.Empty;
            return true;
        }

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            error = "timezone is invalid.";
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            error = "timezone is invalid.";
            return false;
        }

        var fromLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc), tz);
        var toLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(toUtc, DateTimeKind.Utc), tz);

        var alignedFromLocal = alignMode switch
        {
            AnalyticsAlignMode.Week => AlignToWeekStart(fromLocal),
            AnalyticsAlignMode.Month => AlignToMonthStart(fromLocal),
            _ => fromLocal
        };

        var alignedToLocal = alignMode switch
        {
            AnalyticsAlignMode.Week => AlignToNextWeekBoundary(toLocal),
            AnalyticsAlignMode.Month => AlignToNextMonthBoundary(toLocal),
            _ => toLocal
        };

        alignedFromUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(alignedFromLocal, DateTimeKind.Unspecified), tz);
        alignedToUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(alignedToLocal, DateTimeKind.Unspecified), tz);

        if (alignedFromUtc >= alignedToUtc)
        {
            error = "Aligned window is invalid.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static DateTime AlignToWeekStart(DateTime value)
    {
        var diff = ((int)value.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return value.Date.AddDays(-diff);
    }

    private static DateTime AlignToNextWeekBoundary(DateTime value)
    {
        var start = AlignToWeekStart(value);
        return start < value ? start.AddDays(7) : start;
    }

    private static DateTime AlignToMonthStart(DateTime value)
    {
        return new DateTime(value.Year, value.Month, 1);
    }

    private static DateTime AlignToNextMonthBoundary(DateTime value)
    {
        var start = AlignToMonthStart(value);
        return start < value ? start.AddMonths(1) : start;
    }
}
