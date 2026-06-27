namespace XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;

public static class AnalyticsQueryDefaults
{
    public const int DefaultTop = 10;
    public const int MinTop = 1;
    public const int MaxTop = 100;

    public const int DefaultComparePeriods = 1;
    public const int MinComparePeriods = 1;
    public const int MaxComparePeriods = 12;

    public const int FifteenMinuteMaxDays = 3;
    public const int OneHourMaxDays = 31;
    public const int OneDayMaxDays = 366;

    /// <summary>
    /// Maximum total prior-period lookback (current window span x comparePeriods) permitted when the
    /// comparison windows are span-shifted (alignMode None). Week/Month alignment is naturally bounded.
    /// Guards against materialising very large row sets for the comparison query.
    /// </summary>
    public const int MaxComparisonLookbackDays = 731;
}
