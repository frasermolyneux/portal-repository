namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

public sealed record RuleTimingSettings
{
    public int ConnectionDelaySeconds { get; init; } = 5;
    public int FreshnessSeconds { get; init; } = 120;

    public RuleTimingSettings Apply(RuleTimingOverrides? overrides) => new()
    {
        ConnectionDelaySeconds = overrides?.ConnectionDelaySeconds ?? ConnectionDelaySeconds,
        FreshnessSeconds = overrides?.FreshnessSeconds ?? FreshnessSeconds
    };

    public bool IsValid => ConnectionDelaySeconds is >= 0 and <= 60 &&
        FreshnessSeconds is > 0 and <= 900 && ConnectionDelaySeconds < FreshnessSeconds;
}

public sealed record RuleTimingOverrides
{
    public int? ConnectionDelaySeconds { get; init; }
    public int? FreshnessSeconds { get; init; }
}
