using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

public static class VpnProtectionSettingsConstants
{
    public const string Namespace = "vpnProtection";
    public const int SchemaVersion = SchemaVersionSupport.CurrentSchemaVersion;

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
public enum VpnProtectionComparisonOperator
{
    Unknown = 0,
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains
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

    public VpnProtectionSignal Signal { get; set; }

    public VpnProtectionComparisonOperator Operator { get; set; }

    public string ExpectedValue { get; set; } = string.Empty;

    public VpnProtectionAction Action { get; set; }

    public string? ReasonTemplate { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class VpnProtectionRuleOverride
{
    public string Id { get; set; } = string.Empty;

    public bool? Enabled { get; set; }

    public VpnProtectionSignal? Signal { get; set; }

    public VpnProtectionComparisonOperator? Operator { get; set; }

    public string? ExpectedValue { get; set; }

    public VpnProtectionAction? Action { get; set; }

    public string? ReasonTemplate { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
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

    public required VpnProtectionSignal Signal { get; init; }

    public required VpnProtectionComparisonOperator Operator { get; init; }

    public required string ExpectedValue { get; init; }

    public required VpnProtectionAction Action { get; init; }

    public required string ReasonTemplate { get; init; }

    public required int OrderIndex { get; init; }
}