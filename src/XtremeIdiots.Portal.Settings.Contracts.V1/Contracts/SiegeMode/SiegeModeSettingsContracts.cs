using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.SiegeMode;

public static class SiegeModeSettingsConstants
{
    public const string Namespace = "siegeMode";
    public const int DefaultDurationMinutes = 240;
    public const int MaxConditionDepth = 8;
    public const int MaxConditionsPerRule = 100;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SiegeConditionGroupOperator
{
    Unknown = 0,
    And,
    Or
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SiegePlayerSignal
{
    Unknown = 0,
    NewGuid,
    NewIp,
    PlayerTag
}

public sealed class SiegeModeSettingsDocument
{
    public int SchemaVersion { get; set; } = SchemaVersionSupport.CurrentSchemaVersion;
    public int DurationMinutes { get; set; } = SiegeModeSettingsConstants.DefaultDurationMinutes;
    public List<SiegeModeRule> Rules { get; set; } = CreateDefaultRules();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    private static List<SiegeModeRule> CreateDefaultRules() =>
    [
        new()
        {
            Id = "new-guid",
            Condition = new() { PlayerSignal = SiegePlayerSignal.NewGuid, Operator = VpnProtectionComparisonOperator.Equal, ExpectedValue = "true" }
        },
        new()
        {
            Id = "new-vpn-ip",
            Condition = new()
            {
                GroupOperator = SiegeConditionGroupOperator.And,
                Conditions =
                [
                    new()
                    {
                        GroupOperator = SiegeConditionGroupOperator.Or,
                        Conditions =
                        [
                            new() { Signal = VpnProtectionSignal.ProxyCheckIsVpn, Operator = VpnProtectionComparisonOperator.Equal, ExpectedValue = "true" },
                            new() { Signal = VpnProtectionSignal.MaxMindIsAnonymousVpn, Operator = VpnProtectionComparisonOperator.Equal, ExpectedValue = "true" }
                        ]
                    },
                    new() { PlayerSignal = SiegePlayerSignal.NewIp, Operator = VpnProtectionComparisonOperator.Equal, ExpectedValue = "true" },
                    new() { PlayerSignal = SiegePlayerSignal.PlayerTag, Operator = VpnProtectionComparisonOperator.NotEqual, ExpectedValue = "vpn-exempt" }
                ]
            }
        }
    ];
}

public sealed class SiegeModeRule
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public SiegeModeCondition? Condition { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class SiegeModeCondition
{
    public SiegeConditionGroupOperator? GroupOperator { get; set; }
    public List<SiegeModeCondition>? Conditions { get; set; }
    public VpnProtectionSignal? Signal { get; set; }
    public SiegePlayerSignal? PlayerSignal { get; set; }
    public VpnProtectionComparisonOperator? Operator { get; set; }
    public string? ExpectedValue { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
