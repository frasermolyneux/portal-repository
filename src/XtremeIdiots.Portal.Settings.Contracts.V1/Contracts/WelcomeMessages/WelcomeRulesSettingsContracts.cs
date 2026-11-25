using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.WelcomeMessages;

public static class WelcomeRulesSettingsConstants
{
    public const string Namespace = "welcomeRules";
    public const string FeatureId = RulesEngineCatalogueV1.WelcomeMessagesFeature;
    public const int SchemaVersion = RuleSetConstants.SchemaVersion;
}

public sealed class WelcomeRulesSettingsDocument : FeatureRuleSetDocument
{
    public WelcomeRulesSettingsDocument()
    {
        FeatureId = WelcomeRulesSettingsConstants.FeatureId;
        Enabled = false;
        Timing = new();
    }
}

public sealed class WelcomeRulesServerSettingsDocument : FeatureRuleSetOverrideDocument
{
    public WelcomeRulesServerSettingsDocument()
    {
        FeatureId = WelcomeRulesSettingsConstants.FeatureId;
        EnabledOverride = FeatureEnablementOverride.Inherit;
    }
}

public sealed class WelcomeRulesSettingsValidator
{
    private static readonly RuleSetValidationOptions Options =
        RuleSetValidationOptions.ForFeature(WelcomeRulesSettingsConstants.FeatureId);

    public SettingsValidationResult Validate(WelcomeRulesSettingsDocument? document)
    {
        var result = new RuleSetValidator().Validate(document, Options);
        if (document?.ExtensionData?.Keys.Any(key => key.Equals("defaults", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("ruleOverrides", StringComparison.OrdinalIgnoreCase) || key.Equals("inheritGlobalRules", StringComparison.OrdinalIgnoreCase)) == true)
        {
            result.Errors.Add("Legacy welcome settings cannot be used in the welcomeRules namespace.");
        }
        return result;
    }

    public SettingsValidationResult ValidateServerOverride(WelcomeRulesServerSettingsDocument? document) =>
        new RuleSetValidator().ValidateOverride(document, Options);
}
