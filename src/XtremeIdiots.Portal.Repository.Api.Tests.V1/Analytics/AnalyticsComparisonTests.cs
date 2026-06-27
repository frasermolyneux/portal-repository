using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Api.V1.Analytics;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Analytics;

[Trait("Category", "Unit")]
public class AnalyticsComparisonTests
{
    private static readonly DateTime WeekStart = new(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc); // Monday
    private static readonly DateTime WeekEnd = new(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GetWindows_ReturnsEmpty_WhenCompareModeNone()
    {
        var windows = AnalyticsComparison.GetWindows(WeekStart, WeekEnd, AnalyticsCompareMode.None, 3, AnalyticsAlignMode.Week);

        Assert.Empty(windows);
    }

    [Fact]
    public void GetWindows_ReturnsSinglePriorWeek_ForPreviousPeriodWeekAlign()
    {
        var windows = AnalyticsComparison.GetWindows(WeekStart, WeekEnd, AnalyticsCompareMode.PreviousPeriod, 5, AnalyticsAlignMode.Week);

        var window = Assert.Single(windows);
        Assert.Equal(1, window.Offset);
        Assert.Equal(WeekStart.AddDays(-7), window.FromUtc);
        Assert.Equal(WeekEnd.AddDays(-7), window.ToUtc);
        Assert.Equal("Previous week", window.PeriodLabel);
    }

    [Fact]
    public void GetWindows_ReturnsNRollingWeeks_WithExpectedOffsetsAndLabels()
    {
        var windows = AnalyticsComparison.GetWindows(WeekStart, WeekEnd, AnalyticsCompareMode.RollingPeriods, 3, AnalyticsAlignMode.Week);

        Assert.Equal(3, windows.Count);
        Assert.Equal(WeekStart.AddDays(-7), windows[0].FromUtc);
        Assert.Equal(WeekStart.AddDays(-14), windows[1].FromUtc);
        Assert.Equal(WeekStart.AddDays(-21), windows[2].FromUtc);
        Assert.Equal("Previous week", windows[0].PeriodLabel);
        Assert.Equal("2 weeks ago", windows[1].PeriodLabel);
        Assert.Equal("3 weeks ago", windows[2].PeriodLabel);
    }

    [Fact]
    public void GetWindows_ShiftsByCalendarMonth_ForMonthAlign()
    {
        var from = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        var windows = AnalyticsComparison.GetWindows(from, to, AnalyticsCompareMode.RollingPeriods, 2, AnalyticsAlignMode.Month);

        Assert.Equal(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), windows[0].FromUtc);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), windows[1].FromUtc);
        Assert.Equal("Previous month", windows[0].PeriodLabel);
    }

    [Fact]
    public void GetWindows_ShiftsByWindowSpan_WhenAlignNone()
    {
        var from = new DateTime(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc); // 2-day span

        var windows = AnalyticsComparison.GetWindows(from, to, AnalyticsCompareMode.RollingPeriods, 2, AnalyticsAlignMode.None);

        Assert.Equal(new DateTime(2024, 1, 6, 0, 0, 0, DateTimeKind.Utc), windows[0].FromUtc);
        Assert.Equal(new DateTime(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc), windows[0].ToUtc);
        Assert.Equal(new DateTime(2024, 1, 4, 0, 0, 0, DateTimeKind.Utc), windows[1].FromUtc);
        Assert.Equal("Previous period", windows[0].PeriodLabel);
    }

    [Fact]
    public void BuildComparisonSeries_MapsPriorBucketsOntoCurrentByIndex()
    {
        var currentStarts = new List<DateTime>
        {
            WeekStart,
            WeekStart.AddHours(1),
            WeekStart.AddHours(2)
        };
        var window = new AnalyticsComparison.ComparisonWindow(WeekStart.AddDays(-7), WeekEnd.AddDays(-7), 1, "Previous week");
        var priorByBucket = new Dictionary<DateTime, double>
        {
            [WeekStart.AddDays(-7)] = 5,
            [WeekStart.AddDays(-7).AddHours(1)] = 7,
            [WeekStart.AddDays(-7).AddHours(2)] = 9
        };

        var series = AnalyticsComparison.BuildComparisonSeries("sessions", "Sessions", window, currentStarts, AnalyticsBucket.OneHour, priorByBucket, elapsedCutoffUtc: null);

        Assert.Equal("sessions_cmp1", series.Key);
        Assert.Equal("comparison", series.Role);
        Assert.Equal("Previous week", series.PeriodLabel);
        Assert.Collection(series.Values,
            v => { Assert.Equal(WeekStart, v.BucketStartUtc); Assert.Equal(5, v.Value); },
            v => { Assert.Equal(WeekStart.AddHours(1), v.BucketStartUtc); Assert.Equal(7, v.Value); },
            v => { Assert.Equal(WeekStart.AddHours(2), v.BucketStartUtc); Assert.Equal(9, v.Value); });
    }

    [Fact]
    public void BuildComparisonSeries_TruncatesAtElapsedCutoff()
    {
        var currentStarts = new List<DateTime>
        {
            WeekStart,
            WeekStart.AddHours(1),
            WeekStart.AddHours(2)
        };
        var window = new AnalyticsComparison.ComparisonWindow(WeekStart.AddDays(-7), WeekEnd.AddDays(-7), 1, "Previous week");
        var priorByBucket = new Dictionary<DateTime, double>
        {
            [WeekStart.AddDays(-7)] = 5,
            [WeekStart.AddDays(-7).AddHours(1)] = 7,
            [WeekStart.AddDays(-7).AddHours(2)] = 9
        };

        var series = AnalyticsComparison.BuildComparisonSeries("sessions", "Sessions", window, currentStarts, AnalyticsBucket.OneHour, priorByBucket, elapsedCutoffUtc: WeekStart.AddHours(2));

        Assert.Equal(2, series.Values.Count);
    }

    [Fact]
    public void BuildSummary_UsesSinglePriorTotal_ForPreviousPeriod()
    {
        var summary = AnalyticsComparison.BuildSummary(120, new[] { 100d });

        Assert.Equal(120, summary.CurrentTotal);
        Assert.Equal(100, summary.BaselineTotal);
        Assert.Equal(20, summary.Delta);
        Assert.Equal(20, summary.DeltaPercent);
    }

    [Fact]
    public void BuildSummary_AveragesPriorTotals_ForRollingPeriods()
    {
        var summary = AnalyticsComparison.BuildSummary(150, new[] { 100d, 200d });

        Assert.Equal(150, summary.BaselineTotal);
        Assert.Equal(0, summary.Delta);
        Assert.Equal(0, summary.DeltaPercent);
    }

    [Fact]
    public void BuildSummary_ReturnsZeroPercent_WhenBaselineIsZero()
    {
        var summary = AnalyticsComparison.BuildSummary(50, new[] { 0d });

        Assert.Equal(0, summary.BaselineTotal);
        Assert.Equal(50, summary.Delta);
        Assert.Equal(0, summary.DeltaPercent);
    }

    [Fact]
    public void ApplyIndex100_RebasesSeriesToFirstNonZeroValue()
    {
        var currentStarts = new List<DateTime> { WeekStart, WeekStart.AddHours(1), WeekStart.AddHours(2) };
        var window = new AnalyticsComparison.ComparisonWindow(WeekStart.AddDays(-7), WeekEnd.AddDays(-7), 1, "Previous week");
        var priorByBucket = new Dictionary<DateTime, double>
        {
            [WeekStart.AddDays(-7)] = 5,
            [WeekStart.AddDays(-7).AddHours(1)] = 7,
            [WeekStart.AddDays(-7).AddHours(2)] = 9
        };
        var series = AnalyticsComparison.BuildComparisonSeries("sessions", "Sessions", window, currentStarts, AnalyticsBucket.OneHour, priorByBucket, elapsedCutoffUtc: null);

        AnalyticsComparison.ApplyIndex100([series]);

        Assert.Equal(100, series.Values[0].Value);
        Assert.Equal(140, series.Values[1].Value);
        Assert.Equal(180, series.Values[2].Value);
    }
}
