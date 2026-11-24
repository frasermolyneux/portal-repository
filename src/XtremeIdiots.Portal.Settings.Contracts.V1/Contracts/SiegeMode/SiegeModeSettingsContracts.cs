using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.SiegeMode;

public static class SiegeModeSettingsConstants
{
    public const string Namespace = "siegeMode";
    public const string FeatureId = RulesEngineCatalogueV1.SiegeModeFeature;
    public const int DefaultDurationMinutes = 240;
}

public sealed class SiegeModeSettingsDocument : FeatureRuleSetDocument
{
    public SiegeModeSettingsDocument()
    {
        FeatureId = SiegeModeSettingsConstants.FeatureId;
        Enabled = false;
        DurationMinutes = SiegeModeSettingsConstants.DefaultDurationMinutes;
    }

    public int DurationMinutes { get; set; }
}

public sealed class SiegeModeServerSettingsOverrideDocument : FeatureRuleSetOverrideDocument
{
    public SiegeModeServerSettingsOverrideDocument()
    {
        FeatureId = SiegeModeSettingsConstants.FeatureId;
        EnabledOverride = FeatureEnablementOverride.Inherit;
    }

    public int? DurationMinutesOverride { get; set; }
}

public sealed class SiegeModeSettingsValidator
{
    private static readonly RuleSetValidationOptions Options =
        RuleSetValidationOptions.ForFeature(SiegeModeSettingsConstants.FeatureId);

    public SettingsValidationResult Validate(SiegeModeSettingsDocument? document)
    {
        var result = new RuleSetValidator().Validate(document, Options);
        if (document is not null && document.DurationMinutes <= 0)
        {
            result.Errors.Add("durationMinutes must be positive.");
        }
        return result;
    }

    public SettingsValidationResult ValidateServerOverride(SiegeModeServerSettingsOverrideDocument? document)
    {
        var result = new RuleSetValidator().ValidateOverride(document, Options);
        if (document?.DurationMinutesOverride is <= 0)
        {
            result.Errors.Add("durationMinutesOverride must be positive when provided.");
        }
        return result;
    }
}

public sealed record EffectiveSiegeModeSettings(
    string FeatureId,
    bool Enabled,
    int DurationMinutes,
    IReadOnlyList<RuleDefinition> Rules);

public static class SiegeModeSettingsMerger
{
    public static EffectiveSiegeModeSettings Merge(
        SiegeModeSettingsDocument global,
        SiegeModeServerSettingsOverrideDocument? serverOverride)
    {
        var effective = FeatureRuleSetMerger.Merge(global, serverOverride);
        return new EffectiveSiegeModeSettings(
            effective.FeatureId,
            effective.Enabled,
            serverOverride?.DurationMinutesOverride ?? global.DurationMinutes,
            effective.Rules);
    }

    public static SiegeModeSettingsDocument Freeze(EffectiveSiegeModeSettings settings) => new()
    {
        FeatureId = settings.FeatureId,
        Enabled = settings.Enabled,
        DurationMinutes = settings.DurationMinutes,
        Rules = settings.Rules.ToList()
    };
}
