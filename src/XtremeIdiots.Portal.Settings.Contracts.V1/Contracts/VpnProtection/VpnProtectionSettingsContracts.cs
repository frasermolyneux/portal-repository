using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

public static class VpnProtectionSettingsConstants
{
    public const string Namespace = "vpnProtection";
    public const int SchemaVersion = RuleSetConstants.SchemaVersion;
    public const string FeatureId = RulesEngineCatalogueV1.VpnProtectionFeature;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VpnProtectionSignal
{
    Unknown = 0,
    ProxyCheckRiskScore,
    ProxyCheckIsProxy,
    ProxyCheckIsVpn,
    ProxyCheckProxyType,
    ProxyCheckAsNumber,
    ProxyCheckAsOrganization,
    MaxMindAnonymizerConfidence,
    MaxMindIsAnonymous,
    MaxMindIsAnonymousVpn,
    MaxMindIsHostingProvider,
    MaxMindIsPublicProxy,
    MaxMindIsResidentialProxy,
    MaxMindIsTorExitNode,
    MaxMindProviderName,
    MaxMindStatus,
    ProxyCheckStatus,
    IsPartial
}

public sealed class VpnProtectionSettingsDocument : FeatureRuleSetDocument
{
    public VpnProtectionSettingsDocument()
    {
        FeatureId = VpnProtectionSettingsConstants.FeatureId;
        Enabled = false;
    }
}

public sealed class VpnProtectionServerSettingsDocument : FeatureRuleSetOverrideDocument
{
    public VpnProtectionServerSettingsDocument()
    {
        FeatureId = VpnProtectionSettingsConstants.FeatureId;
        EnabledOverride = FeatureEnablementOverride.Inherit;
    }
}

public sealed class VpnProtectionSettingsValidator
{
    private static readonly RuleSetValidationOptions Options =
        RuleSetValidationOptions.ForFeature(VpnProtectionSettingsConstants.FeatureId);

    public SettingsValidationResult Validate(VpnProtectionSettingsDocument? document) =>
        new RuleSetValidator().Validate(document, Options);

    public SettingsValidationResult ValidateServerOverride(VpnProtectionServerSettingsDocument? document) =>
        new RuleSetValidator().ValidateOverride(document, Options);
}
