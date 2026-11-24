using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

public static class VpnProtectionSettingsConstants
{
    public const string Namespace = "vpnProtection";
    public const int SchemaVersion = 2;

    public const int MaxRules = 100;
    public const int MaxRuleIdLength = 64;
    public const int MaxExpectedValueLength = 256;
    public const int MaxReasonTemplateLength = 256;
    public const int MaxExcludedPlayerTags = 100;
    public const int MaxPlayerTagLength = 128;
    public const int MinNumericValue = 0;
    public const int MaxNumericValue = 100;

    public const string DefaultReasonTemplate = "VPN Protection: matched rule '{ruleId}'";

    public static IReadOnlySet<string> AllowedReasonTemplatePlaceholders { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "{ruleId}",
            "{signal}",
            "{actualValue}",
            "{expectedValue}"
        };
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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VpnProtectionAction
{
    Unknown = 0,
    Observation,
    Kick,
    Ban
}

public sealed class VpnProtectionSettingsDocument
{
    public int SchemaVersion { get; set; } = VpnProtectionSettingsConstants.SchemaVersion;

    public bool? Enabled { get; set; }

    public bool? InheritGlobalRules { get; set; }

    public List<VpnProtectionRule> Rules { get; set; } = [];

    public List<VpnProtectionRuleOverride> RuleOverrides { get; set; } = [];

    public string[] ExcludedPlayerTags { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class VpnProtectionRule
{
    public string Id { get; set; } = string.Empty;

    public bool? Enabled { get; set; }

    public VpnProtectionCondition? Condition { get; set; }

    public VpnProtectionAction Action { get; set; }

    public bool PostObservationToForums { get; set; } = true;

    public string? ReasonTemplate { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class VpnProtectionRuleOverride
{
    public string Id { get; set; } = string.Empty;

    public bool? Enabled { get; set; }

    public VpnProtectionCondition? Condition { get; set; }

    public VpnProtectionAction? Action { get; set; }

    public bool? PostObservationToForums { get; set; }

    public string? ReasonTemplate { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class VpnProtectionCondition : RuleCondition<VpnProtectionSignal, VpnProtectionCondition>
{
}

public sealed record EffectiveVpnProtectionSettings
{
    public bool Enabled { get; init; }

    public IReadOnlyList<EffectiveVpnProtectionRule> Rules { get; init; } = [];

    public IReadOnlySet<string> ExcludedPlayerTags { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool ValidationFailed { get; init; }

    public static EffectiveVpnProtectionSettings Disabled(bool validationFailed = false) => new()
    {
        Enabled = false,
        ValidationFailed = validationFailed
    };
}

public sealed record EffectiveVpnProtectionRule
{
    public required string Id { get; init; }

    public required bool Enabled { get; init; }

    public required VpnProtectionCondition Condition { get; init; }

    public required VpnProtectionAction Action { get; init; }

    public bool PostObservationToForums { get; init; } = true;

    public required string ReasonTemplate { get; init; }

    public required int OrderIndex { get; init; }
}
