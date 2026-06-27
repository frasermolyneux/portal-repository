using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;

namespace XtremeIdiots.Portal.Repository.Api.V1.Analytics;

/// <summary>
/// Shared engine that turns comparison query options into real prior-period windows and overlay
/// series. Replaces the previous per-controller faked baseline. Comparison series are aligned to the
/// current window by bucket index so the front-end can overlay them directly.
/// </summary>
internal static class AnalyticsComparison
{
    public readonly record struct ComparisonWindow(DateTime FromUtc, DateTime ToUtc, int Offset, string PeriodLabel);

    /// <summary>
    /// Resolves the prior-period windows implied by the comparison options. Returns an empty list when
    /// no comparison is requested. Windows are calendar-shifted for Week/Month alignment, otherwise
    /// shifted by the current window span.
    /// </summary>
    public static IReadOnlyList<ComparisonWindow> GetWindows(
        DateTime alignedFromUtc,
        DateTime alignedToUtc,
        AnalyticsCompareMode compareMode,
        int comparePeriods,
        AnalyticsAlignMode alignMode)
    {
        var count = compareMode switch
        {
            AnalyticsCompareMode.PreviousPeriod => 1,
            AnalyticsCompareMode.RollingPeriods => Math.Max(1, comparePeriods),
            _ => 0
        };

        var windows = new List<ComparisonWindow>(count);
        for (var offset = 1; offset <= count; offset++)
        {
            var (from, to) = Shift(alignedFromUtc, alignedToUtc, alignMode, offset);
            windows.Add(new ComparisonWindow(from, to, offset, Label(offset, alignMode)));
        }

        return windows;
    }

    /// <summary>
    /// Builds a comparison overlay series for a single prior window, mapping each comparison bucket onto
    /// the matching current bucket by index. When <paramref name="elapsedCutoffUtc"/> is supplied, the
    /// series is truncated to the elapsed portion of an in-progress current period (elapsed-aligned).
    /// </summary>
    /// <remarks>
    /// Mapping is by bucket index. For Month alignment, unequal month lengths mean the overlay tail can
    /// map current buckets onto positions outside the prior month (returned as 0) or leave prior buckets
    /// unused; absolute keying prevents cross-window contamination but the tail is approximate.
    /// </remarks>
    public static AnalyticsSeriesDto BuildComparisonSeries(
        string key,
        string label,
        in ComparisonWindow window,
        IReadOnlyList<DateTime> currentBucketStarts,
        AnalyticsBucket bucket,
        IReadOnlyDictionary<DateTime, double> comparisonValuesByBucket,
        DateTime? elapsedCutoffUtc)
    {
        var values = new List<AnalyticsSeriesValueDto>(currentBucketStarts.Count);
        var cmpCursor = AnalyticsTimeBucketing.Truncate(window.FromUtc, bucket);

        foreach (var currentStart in currentBucketStarts)
        {
            if (elapsedCutoffUtc.HasValue && currentStart >= elapsedCutoffUtc.Value)
            {
                break;
            }

            var value = comparisonValuesByBucket.TryGetValue(cmpCursor, out var v) ? v : 0d;
            values.Add(new AnalyticsSeriesValueDto
            {
                BucketStartUtc = currentStart,
                Value = Math.Round(value, 2)
            });

            cmpCursor = AnalyticsTimeBucketing.Add(cmpCursor, bucket);
        }

        return new AnalyticsSeriesDto
        {
            Key = $"{key}_cmp{window.Offset}",
            Label = $"{label} ({window.PeriodLabel})",
            Role = "comparison",
            PeriodLabel = window.PeriodLabel,
            Values = values
        };
    }

    /// <summary>
    /// Computes the comparison summary. Baseline is the single prior period total (PreviousPeriod) or the
    /// average of the prior period totals (RollingPeriods).
    /// </summary>
    public static AnalyticsCompareSummaryDto BuildSummary(double currentTotal, IReadOnlyCollection<double> comparisonTotals)
    {
        var baseline = comparisonTotals.Count == 0 ? 0d : comparisonTotals.Average();
        var delta = currentTotal - baseline;
        var deltaPercent = baseline == 0d ? 0d : delta / baseline * 100d;

        return new AnalyticsCompareSummaryDto
        {
            CurrentTotal = Math.Round(currentTotal, 2),
            BaselineTotal = Math.Round(baseline, 2),
            Delta = Math.Round(delta, 2),
            DeltaPercent = Math.Round(deltaPercent, 2)
        };
    }

    /// <summary>
    /// Rebases each series to 100 at its first non-zero value so shapes can be compared regardless of
    /// absolute magnitude (index-100 normalisation).
    /// </summary>
    public static void ApplyIndex100(IEnumerable<AnalyticsSeriesDto> series)
    {
        foreach (var s in series)
        {
            var baseValue = s.Values.FirstOrDefault(v => v.Value != 0d)?.Value ?? 0d;
            if (baseValue == 0d)
            {
                continue;
            }

            foreach (var v in s.Values)
            {
                v.Value = Math.Round(v.Value / baseValue * 100d, 2);
            }
        }
    }

    private static (DateTime From, DateTime To) Shift(DateTime fromUtc, DateTime toUtc, AnalyticsAlignMode alignMode, int offset)
    {
        return alignMode switch
        {
            AnalyticsAlignMode.Week => (fromUtc.AddDays(-7 * offset), toUtc.AddDays(-7 * offset)),
            AnalyticsAlignMode.Month => (fromUtc.AddMonths(-offset), toUtc.AddMonths(-offset)),
            _ => ShiftBySpan(fromUtc, toUtc, offset)
        };
    }

    private static (DateTime From, DateTime To) ShiftBySpan(DateTime fromUtc, DateTime toUtc, int offset)
    {
        var shift = TimeSpan.FromTicks((toUtc - fromUtc).Ticks * offset);
        return (fromUtc - shift, toUtc - shift);
    }

    private static string Label(int offset, AnalyticsAlignMode alignMode)
    {
        var unit = alignMode switch
        {
            AnalyticsAlignMode.Week => "week",
            AnalyticsAlignMode.Month => "month",
            _ => "period"
        };

        return offset == 1 ? $"Previous {unit}" : $"{offset} {unit}s ago";
    }
}
