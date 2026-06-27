using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Validation;

[Trait("Category", "Unit")]
public class AnalyticsQueryValidatorTests
{
    [Fact]
    public void TryValidateWindow_ReturnsFalse_WhenFromIsEqualToTo()
    {
        var now = DateTime.UtcNow;

        var valid = AnalyticsQueryValidator.TryValidateWindow(now, now, out var error);

        Assert.False(valid);
        Assert.Contains("fromUtc", error);
    }

    [Fact]
    public void TryValidateWindow_ReturnsFalse_WhenFromIsAfterTo()
    {
        var fromUtc = DateTime.UtcNow;
        var toUtc = fromUtc.AddMinutes(-1);

        var valid = AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out var error);

        Assert.False(valid);
        Assert.Contains("fromUtc", error);
    }

    [Fact]
    public void TryValidateWindow_ReturnsTrue_WhenWindowIsValid()
    {
        var fromUtc = DateTime.UtcNow.AddHours(-1);
        var toUtc = DateTime.UtcNow;

        var valid = AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out var error);

        Assert.True(valid);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void TryValidateTop_ReturnsFalse_WhenBelowMinimum()
    {
        var valid = AnalyticsQueryValidator.TryValidateTop(AnalyticsQueryDefaults.MinTop - 1, out var error);

        Assert.False(valid);
        Assert.Contains(AnalyticsQueryDefaults.MinTop.ToString(), error);
    }

    [Fact]
    public void TryValidateTop_ReturnsFalse_WhenAboveMaximum()
    {
        var valid = AnalyticsQueryValidator.TryValidateTop(AnalyticsQueryDefaults.MaxTop + 1, out var error);

        Assert.False(valid);
        Assert.Contains(AnalyticsQueryDefaults.MaxTop.ToString(), error);
    }

    [Fact]
    public void TryValidateTop_ReturnsTrue_AtBounds()
    {
        var minValid = AnalyticsQueryValidator.TryValidateTop(AnalyticsQueryDefaults.MinTop, out _);
        var maxValid = AnalyticsQueryValidator.TryValidateTop(AnalyticsQueryDefaults.MaxTop, out _);

        Assert.True(minValid);
        Assert.True(maxValid);
    }

    [Theory]
    [InlineData(AnalyticsBucket.FifteenMinutes, AnalyticsQueryDefaults.FifteenMinuteMaxDays)]
    [InlineData(AnalyticsBucket.OneHour, AnalyticsQueryDefaults.OneHourMaxDays)]
    [InlineData(AnalyticsBucket.OneDay, AnalyticsQueryDefaults.OneDayMaxDays)]
    public void TryValidateBucketWindow_ReturnsTrue_AtMaximumWindow(AnalyticsBucket bucket, int maxDays)
    {
        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc.AddDays(-maxDays);

        var valid = AnalyticsQueryValidator.TryValidateBucketWindow(fromUtc, toUtc, bucket, out var error);

        Assert.True(valid);
        Assert.Equal(string.Empty, error);
    }

    [Theory]
    [InlineData(AnalyticsBucket.FifteenMinutes, AnalyticsQueryDefaults.FifteenMinuteMaxDays)]
    [InlineData(AnalyticsBucket.OneHour, AnalyticsQueryDefaults.OneHourMaxDays)]
    [InlineData(AnalyticsBucket.OneDay, AnalyticsQueryDefaults.OneDayMaxDays)]
    public void TryValidateBucketWindow_ReturnsFalse_WhenWindowExceedsMaximum(AnalyticsBucket bucket, int maxDays)
    {
        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc.AddDays(-(maxDays + 1));

        var valid = AnalyticsQueryValidator.TryValidateBucketWindow(fromUtc, toUtc, bucket, out var error);

        Assert.False(valid);
        Assert.Contains(maxDays.ToString(), error);
    }

    [Fact]
    public void TryValidateComparePeriods_ReturnsFalse_WhenBelowMinimum()
    {
        var valid = AnalyticsQueryValidator.TryValidateComparePeriods(AnalyticsQueryDefaults.MinComparePeriods - 1, out var error);

        Assert.False(valid);
        Assert.Contains(AnalyticsQueryDefaults.MinComparePeriods.ToString(), error);
    }

    [Fact]
    public void TryValidateComparePeriods_ReturnsFalse_WhenAboveMaximum()
    {
        var valid = AnalyticsQueryValidator.TryValidateComparePeriods(AnalyticsQueryDefaults.MaxComparePeriods + 1, out var error);

        Assert.False(valid);
        Assert.Contains(AnalyticsQueryDefaults.MaxComparePeriods.ToString(), error);
    }

    [Fact]
    public void TryValidateComparePeriods_ReturnsTrue_AtBounds()
    {
        var minValid = AnalyticsQueryValidator.TryValidateComparePeriods(AnalyticsQueryDefaults.MinComparePeriods, out _);
        var maxValid = AnalyticsQueryValidator.TryValidateComparePeriods(AnalyticsQueryDefaults.MaxComparePeriods, out _);

        Assert.True(minValid);
        Assert.True(maxValid);
    }

    [Fact]
    public void TryValidateComparisonOptions_ReturnsFalse_WhenTimezoneMissing()
    {
        var valid = AnalyticsQueryValidator.TryValidateComparisonOptions(
            AnalyticsCompareMode.PreviousPeriod,
            AnalyticsQueryDefaults.DefaultComparePeriods,
            AnalyticsAlignMode.None,
            string.Empty,
            out var error);

        Assert.False(valid);
        Assert.Contains("timezone", error);
    }

    [Fact]
    public void TryValidateComparisonOptions_ReturnsFalse_WhenComparePeriodsInvalid()
    {
        var valid = AnalyticsQueryValidator.TryValidateComparisonOptions(
            AnalyticsCompareMode.RollingPeriods,
            AnalyticsQueryDefaults.MaxComparePeriods + 1,
            AnalyticsAlignMode.Week,
            "UTC",
            out var error);

        Assert.False(valid);
        Assert.Contains(AnalyticsQueryDefaults.MaxComparePeriods.ToString(), error);
    }

    [Fact]
    public void TryValidateComparisonOptions_ReturnsTrue_ForValidInputs()
    {
        var valid = AnalyticsQueryValidator.TryValidateComparisonOptions(
            AnalyticsCompareMode.None,
            AnalyticsQueryDefaults.DefaultComparePeriods,
            AnalyticsAlignMode.Month,
            "UTC",
            out var error);

        Assert.True(valid);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void TryGetAlignedWindow_ReturnsFalse_WhenTimezoneInvalid()
    {
        var fromUtc = new DateTime(2025, 1, 7, 10, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2025, 1, 8, 10, 0, 0, DateTimeKind.Utc);

        var valid = AnalyticsQueryValidator.TryGetAlignedWindow(
            fromUtc,
            toUtc,
            AnalyticsAlignMode.Week,
            "Invalid/Timezone",
            out _,
            out _,
            out var error);

        Assert.False(valid);
        Assert.Contains("timezone", error);
    }

    [Fact]
    public void TryGetAlignedWindow_ReturnsInputWindow_WhenAlignNone()
    {
        var fromUtc = new DateTime(2025, 1, 7, 10, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2025, 1, 8, 10, 0, 0, DateTimeKind.Utc);

        var valid = AnalyticsQueryValidator.TryGetAlignedWindow(
            fromUtc,
            toUtc,
            AnalyticsAlignMode.None,
            "UTC",
            out var alignedFromUtc,
            out var alignedToUtc,
            out var error);

        Assert.True(valid);
        Assert.Equal(string.Empty, error);
        Assert.Equal(fromUtc, alignedFromUtc);
        Assert.Equal(toUtc, alignedToUtc);
    }

    [Fact]
    public void TryGetAlignedWindow_AlignsToWeekBoundaries()
    {
        var fromUtc = new DateTime(2025, 1, 8, 10, 0, 0, DateTimeKind.Utc); // Wednesday
        var toUtc = new DateTime(2025, 1, 10, 18, 0, 0, DateTimeKind.Utc); // Friday

        var valid = AnalyticsQueryValidator.TryGetAlignedWindow(
            fromUtc,
            toUtc,
            AnalyticsAlignMode.Week,
            "UTC",
            out var alignedFromUtc,
            out var alignedToUtc,
            out var error);

        Assert.True(valid);
        Assert.Equal(string.Empty, error);
        Assert.Equal(new DateTime(2025, 1, 6, 0, 0, 0, DateTimeKind.Utc), alignedFromUtc); // Monday start
        Assert.Equal(new DateTime(2025, 1, 13, 0, 0, 0, DateTimeKind.Utc), alignedToUtc); // Next Monday
    }

    [Fact]
    public void TryGetAlignedWindow_AlignsToMonthBoundaries()
    {
        var fromUtc = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2025, 2, 2, 8, 0, 0, DateTimeKind.Utc);

        var valid = AnalyticsQueryValidator.TryGetAlignedWindow(
            fromUtc,
            toUtc,
            AnalyticsAlignMode.Month,
            "UTC",
            out var alignedFromUtc,
            out var alignedToUtc,
            out var error);

        Assert.True(valid);
        Assert.Equal(string.Empty, error);
        Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), alignedFromUtc);
        Assert.Equal(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc), alignedToUtc);
    }

    [Fact]
    public void TryValidateComparisonLookback_ReturnsTrue_WhenCompareModeNone()
    {
        var fromUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddDays(300);

        var valid = AnalyticsQueryValidator.TryValidateComparisonLookback(
            fromUtc, toUtc, AnalyticsCompareMode.None, AnalyticsQueryDefaults.MaxComparePeriods, AnalyticsAlignMode.None, out var error);

        Assert.True(valid);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void TryValidateComparisonLookback_ReturnsTrue_ForWeekAlignmentRegardlessOfSpan()
    {
        var fromUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddDays(300);

        var valid = AnalyticsQueryValidator.TryValidateComparisonLookback(
            fromUtc, toUtc, AnalyticsCompareMode.RollingPeriods, AnalyticsQueryDefaults.MaxComparePeriods, AnalyticsAlignMode.Week, out var error);

        Assert.True(valid);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void TryValidateComparisonLookback_ReturnsFalse_WhenSpanShiftedLookbackExceedsMax()
    {
        var fromUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddDays(300); // 300 days x 12 periods = 3600 days > 731

        var valid = AnalyticsQueryValidator.TryValidateComparisonLookback(
            fromUtc, toUtc, AnalyticsCompareMode.RollingPeriods, AnalyticsQueryDefaults.MaxComparePeriods, AnalyticsAlignMode.None, out var error);

        Assert.False(valid);
        Assert.Contains(AnalyticsQueryDefaults.MaxComparisonLookbackDays.ToString(), error);
    }

    [Fact]
    public void TryValidateComparisonLookback_ReturnsTrue_WhenSpanShiftedLookbackWithinMax()
    {
        var fromUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddDays(30); // 30 x 12 = 360 days < 731

        var valid = AnalyticsQueryValidator.TryValidateComparisonLookback(
            fromUtc, toUtc, AnalyticsCompareMode.RollingPeriods, AnalyticsQueryDefaults.MaxComparePeriods, AnalyticsAlignMode.None, out var error);

        Assert.True(valid);
        Assert.Equal(string.Empty, error);
    }
}
