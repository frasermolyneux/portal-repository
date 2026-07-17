using System.Text.Json;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Tests;

[Trait("Category", "Unit")]
public sealed class VpnProtectionSettingsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void RoundtripAndValidate_ValidDocument_Succeeds()
    {
        var input = """
            {
                "schemaVersion": 1,
                "enabled": true,
                "inheritGlobalRules": true,
                "rules": [
                    {
                        "id": "high-risk-vpn",
                        "enabled": true,
                        "signal": "ProxyCheckRiskScore",
                        "operator": "GreaterThanOrEqual",
                        "expectedValue": "75",
                        "action": "Ban",
                        "reasonTemplate": "VPN Protection: {ruleId} ({actualValue})"
                    }
                ],
                "ruleOverrides": [],
                "excludedPlayerTags": ["Trusted VPN"],
                "futureProperty": true
            }
            """;

        var document = JsonSerializer.Deserialize<VpnProtectionSettingsDocument>(input, JsonOptions);

        Assert.NotNull(document);
        Assert.True(new VpnProtectionSettingsValidator().Validate(document).IsValid);

        var output = JsonSerializer.Serialize(document, JsonOptions);
        var roundtripped = JsonSerializer.Deserialize<VpnProtectionSettingsDocument>(output, JsonOptions);

        Assert.NotNull(roundtripped);
        Assert.True(roundtripped.Enabled);
        Assert.Single(roundtripped.Rules);
        Assert.Equal(VpnProtectionAction.Ban, roundtripped.Rules[0].Action);
        Assert.NotNull(roundtripped.ExtensionData);
        Assert.Contains("futureProperty", roundtripped.ExtensionData.Keys);
    }

    [Fact]
    public void Validate_InvalidConditionAndUnsafeReason_ReturnsErrors()
    {
        var document = new VpnProtectionSettingsDocument
        {
            Rules =
            [
                new VpnProtectionRule
                {
                    Id = "vpn",
                    Signal = VpnProtectionSignal.ProxyCheckIsVpn,
                    Operator = VpnProtectionComparisonOperator.GreaterThan,
                    ExpectedValue = "not-a-boolean",
                    Action = VpnProtectionAction.Kick,
                    ReasonTemplate = "VPN; quit {unsupported}"
                },
                new VpnProtectionRule
                {
                    Id = "VPN",
                    Signal = VpnProtectionSignal.ProxyCheckRiskScore,
                    Operator = VpnProtectionComparisonOperator.GreaterThanOrEqual,
                    ExpectedValue = "101",
                    Action = VpnProtectionAction.Unknown
                },
                new VpnProtectionRule
                {
                    Id = "source-status",
                    Signal = VpnProtectionSignal.ProxyCheckStatus,
                    Operator = VpnProtectionComparisonOperator.Contains,
                    ExpectedValue = "Success",
                    Action = VpnProtectionAction.Observation
                }
            ],
            ExcludedPlayerTags = ["Trusted", "trusted"]
        };

        var validation = new VpnProtectionSettingsValidator().Validate(document);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, static error => error.Contains("not supported for boolean", StringComparison.Ordinal));
        Assert.Contains(validation.Errors, static error => error.Contains("must be 'true' or 'false'", StringComparison.Ordinal));
        Assert.Contains(validation.Errors, static error => error.Contains("command separators", StringComparison.Ordinal));
        Assert.Contains(validation.Errors, static error => error.Contains("unsupported placeholder", StringComparison.Ordinal));
        Assert.Contains(validation.Errors, static error => error.Contains("must be unique", StringComparison.Ordinal));
        Assert.Contains(validation.Errors, static error => error.Contains("between 0 and 100", StringComparison.Ordinal));
        Assert.Contains(validation.Errors, static error => error.Contains("action must be specified", StringComparison.Ordinal));
        Assert.Contains(validation.Errors, static error => error.Contains("source status signal", StringComparison.Ordinal));
        Assert.Contains(validation.Errors, static error => error.Contains("excludedPlayerTags", StringComparison.Ordinal));
    }

    [Fact]
    public void Merge_ServerOverridesGlobalAndAddsLocalRule_ReturnsEffectiveSettings()
    {
        var global = new VpnProtectionSettingsDocument
        {
            Enabled = false,
            Rules =
            [
                CreateRule("vpn", VpnProtectionSignal.ProxyCheckIsVpn, "true", VpnProtectionAction.Observation),
                CreateRule("tor", VpnProtectionSignal.MaxMindIsTorExitNode, "true", VpnProtectionAction.Ban)
            ],
            ExcludedPlayerTags = ["Global Exempt"]
        };
        var server = new VpnProtectionSettingsDocument
        {
            Enabled = true,
            RuleOverrides =
            [
                new VpnProtectionRuleOverride
                {
                    Id = "vpn",
                    Action = VpnProtectionAction.Kick,
                    ReasonTemplate = "Server VPN rule {ruleId}"
                },
                new VpnProtectionRuleOverride { Id = "tor", Enabled = false }
            ],
            Rules =
            [
                CreateRule(
                    "hosting",
                    VpnProtectionSignal.MaxMindIsHostingProvider,
                    "true",
                    VpnProtectionAction.Observation)
            ],
            ExcludedPlayerTags = ["Server Exempt", "global exempt"]
        };

        var effective = new VpnProtectionSettingsMerger().Merge(global, server);

        Assert.True(effective.Enabled);
        Assert.False(effective.ValidationFailed);
        Assert.Equal(3, effective.Rules.Count);
        Assert.Equal(VpnProtectionAction.Kick, effective.Rules[0].Action);
        Assert.Equal("Server VPN rule {ruleId}", effective.Rules[0].ReasonTemplate);
        Assert.False(effective.Rules[1].Enabled);
        Assert.Equal(VpnProtectionSettingsConstants.DefaultReasonTemplate, effective.Rules[2].ReasonTemplate);
        Assert.Equal(2, effective.ExcludedPlayerTags.Count);
        Assert.Contains("Global Exempt", effective.ExcludedPlayerTags);
        Assert.Contains("Server Exempt", effective.ExcludedPlayerTags);
    }

    [Fact]
    public void Merge_ServerDisablesInheritance_UsesOnlyLocalRules()
    {
        var global = new VpnProtectionSettingsDocument
        {
            Enabled = true,
            Rules = [CreateRule("global", VpnProtectionSignal.ProxyCheckIsVpn, "true", VpnProtectionAction.Ban)]
        };
        var server = new VpnProtectionSettingsDocument
        {
            InheritGlobalRules = false,
            Rules = [CreateRule("local", VpnProtectionSignal.ProxyCheckIsProxy, "true", VpnProtectionAction.Kick)]
        };

        var effective = new VpnProtectionSettingsMerger().Merge(global, server);

        Assert.True(effective.Enabled);
        var rule = Assert.Single(effective.Rules);
        Assert.Equal("local", rule.Id);
    }

    [Fact]
    public void Merge_LocalRuleCollidesWithInheritedRule_FailsClosed()
    {
        var global = new VpnProtectionSettingsDocument
        {
            Enabled = true,
            Rules = [CreateRule("duplicate", VpnProtectionSignal.ProxyCheckIsVpn, "true", VpnProtectionAction.Ban)]
        };
        var server = new VpnProtectionSettingsDocument
        {
            Enabled = true,
            Rules = [CreateRule("DUPLICATE", VpnProtectionSignal.ProxyCheckIsProxy, "true", VpnProtectionAction.Kick)]
        };

        var effective = new VpnProtectionSettingsMerger().Merge(global, server);

        Assert.False(effective.Enabled);
        Assert.True(effective.ValidationFailed);
        Assert.Empty(effective.Rules);
    }

    private static VpnProtectionRule CreateRule(
        string id,
        VpnProtectionSignal signal,
        string expectedValue,
        VpnProtectionAction action) => new()
        {
            Id = id,
            Signal = signal,
            Operator = VpnProtectionComparisonOperator.Equal,
            ExpectedValue = expectedValue,
            Action = action
        };
}