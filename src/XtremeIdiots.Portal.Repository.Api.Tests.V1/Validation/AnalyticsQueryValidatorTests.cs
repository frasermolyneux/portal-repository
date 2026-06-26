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
}
