using System.Text.Json;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Tests;

[Trait("Category", "Unit")]
public sealed class VpnProtectionSettingsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Defaults_AreDisabledAndEmpty_AndValidate()
    {
        var document = new VpnProtectionSettingsDocument();
        var roundtripped = JsonSerializer.Deserialize<VpnProtectionSettingsDocument>(
            JsonSerializer.Serialize(document, JsonOptions), JsonOptions);

        Assert.NotNull(roundtripped);
        Assert.Null(roundtripped.Enabled);
        Assert.Empty(roundtripped.Rules);
        Assert.True(new VpnProtectionSettingsValidator().Validate(roundtripped).IsValid);

        var effective = new VpnProtectionSettingsMerger().Merge(roundtripped, null);
        Assert.False(effective.Enabled);
        Assert.Empty(effective.Rules);
    }

    [Fact]
    public void RoundtripAndValidate_NestedRules_Succeeds()
    {
        var input = """
            {
                "schemaVersion": 2,
                "enabled": true,
                "inheritGlobalRules": true,
                "rules": [
                    {
                        "id": "high-risk-vpn",
                        "enabled": true,
                        "condition": {
                            "groupOperator": "And",
                            "conditions": [
                                {
                                    "groupOperator": "Or",
                                    "conditions": [
                                        { "signal": "ProxyCheckIsVpn", "operator": "Equal", "expectedValue": "true" },
                                        { "signal": "MaxMindIsAnonymousVpn", "operator": "Equal", "expectedValue": "true" }
                                    ]
                                },
                                { "signal": "ProxyCheckRiskScore", "operator": "GreaterThanOrEqual", "expectedValue": "75" }
                            ]
                        },
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

        var roundtripped = JsonSerializer.Deserialize<VpnProtectionSettingsDocument>(
            JsonSerializer.Serialize(document, JsonOptions), JsonOptions);

        Assert.NotNull(roundtripped);
        Assert.True(roundtripped.Enabled);
        Assert.Equal(VpnProtectionAction.Ban, Assert.Single(roundtripped.Rules).Action);
        Assert.Equal(RuleConditionGroupOperator.And, roundtripped.Rules[0].Condition!.GroupOperator);
        Assert.Equal(RuleConditionGroupOperator.Or, roundtripped.Rules[0].Condition!.Conditions![0].GroupOperator);
        Assert.Equal(VpnProtectionSignal.ProxyCheckIsVpn,
            roundtripped.Rules[0].Condition!.Conditions![0].Conditions![0].Signal);
        Assert.NotNull(roundtripped.ExtensionData);
        Assert.Contains("futureProperty", roundtripped.ExtensionData.Keys);
    }

    [Fact]
    public void Validate_LegacyFlatRule_IsNotMigrated()
    {
        var document = JsonSerializer.Deserialize<VpnProtectionSettingsDocument>("""
            {"enabled":true,"rules":[{"id":"vpn","signal":"ProxyCheckIsVpn",
            "operator":"Equal","expectedValue":"true","action":"Ban"}]}
            """, JsonOptions);

        Assert.NotNull(document);
        Assert.False(new VpnProtectionSettingsValidator().Validate(document).IsValid);
        Assert.NotNull(Assert.Single(document.Rules).ExtensionData);
        Assert.Null(document.Rules[0].Condition);
    }

    [Theory]
    [InlineData("signal", "\"ProxyCheckIsVpn\"")]
    [InlineData("OPERATOR", "\"Equal\"")]
    [InlineData("expectedValue", "\"false\"")]
    public void Validate_LegacyOverrideFields_AreRejected(string field, string value)
    {
        var json = $$"""{"ruleOverrides":[{"id":"vpn","{{field}}":{{value}}}]}""";
        var server = JsonSerializer.Deserialize<VpnProtectionSettingsDocument>(json, JsonOptions);
        var global = new VpnProtectionSettingsDocument
        {
            Enabled = true,
            Rules = [CreateRule("vpn", VpnProtectionSignal.ProxyCheckIsVpn, "true", VpnProtectionAction.Ban)]
        };

        Assert.NotNull(server);
        Assert.False(new VpnProtectionSettingsValidator().Validate(server).IsValid);
        Assert.True(new VpnProtectionSettingsMerger().Merge(global, server).ValidationFailed);
    }

    [Fact]
    public void Validate_UnknownOverrideFields_AreStillAllowed()
    {
        var server = JsonSerializer.Deserialize<VpnProtectionSettingsDocument>(
            """{"ruleOverrides":[{"id":"vpn","futureProperty":true}]}""", JsonOptions);

        Assert.True(new VpnProtectionSettingsValidator().Validate(server).IsValid);
    }

    [Fact]
    public void Validate_LegacyFieldsAlongsideNestedRule_AreRejected()
    {
        var document = JsonSerializer.Deserialize<VpnProtectionSettingsDocument>("""
            {"rules":[{"id":"vpn","signal":"ProxyCheckIsVpn","action":"Ban",
            "condition":{"signal":"ProxyCheckIsVpn","operator":"Equal","expectedValue":"true"}}]}
            """, JsonOptions);

        Assert.False(new VpnProtectionSettingsValidator().Validate(document).IsValid);
    }

    [Fact]
    public void Merge_TrimmedNestedExpectedValues_DoNotMutateSource()
    {
        var leaf = Leaf(VpnProtectionSignal.ProxyCheckStatus, RuleComparisonOperator.Equal, " Success ");
        var global = new VpnProtectionSettingsDocument
        {
            Rules =
            [
                new VpnProtectionRule
                {
                    Id = "status",
                    Action = VpnProtectionAction.Observation,
                    Condition = new VpnProtectionCondition
                    {
                        GroupOperator = RuleConditionGroupOperator.And,
                        Conditions = [leaf]
                    }
                }
            ]
        };

        var effective = new VpnProtectionSettingsMerger().Merge(global, null);

        Assert.False(effective.ValidationFailed);
        Assert.Equal("Success", Assert.Single(Assert.Single(effective.Rules).Condition.Conditions!).ExpectedValue);
        Assert.Equal(" Success ", leaf.ExpectedValue);
    }

    [Fact]
    public void Validate_LegacySchemaVersion_IsRejected()
    {
        var document = new VpnProtectionSettingsDocument { SchemaVersion = 1 };

        Assert.False(new VpnProtectionSettingsValidator().Validate(document).IsValid);
    }

    [Fact]
    public void Merge_LegacyObservation_DefaultsToForumPublication()
    {
        var document = new VpnProtectionSettingsDocument
        {
            Enabled = true,
            Rules = [CreateRule("vpn", VpnProtectionSignal.ProxyCheckIsVpn, "true", VpnProtectionAction.Observation)]
        };

        var effective = new VpnProtectionSettingsMerger().Merge(document, null);

        Assert.False(effective.ValidationFailed);
        Assert.True(Assert.Single(effective.Rules).PostObservationToForums);
    }

    [Theory]
    [InlineData(true, null, true)]
    [InlineData(false, null, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    public void Merge_ObservationPublication_InheritsAndOverrides(bool globalValue, bool? serverOverride, bool expected)
    {
        var rule = CreateRule("vpn", VpnProtectionSignal.ProxyCheckIsVpn, "true", VpnProtectionAction.Observation);
        rule.PostObservationToForums = globalValue;
        var global = new VpnProtectionSettingsDocument { Enabled = true, Rules = [rule] };
        var server = new VpnProtectionSettingsDocument
        {
            RuleOverrides = [new VpnProtectionRuleOverride { Id = "vpn", PostObservationToForums = serverOverride }]
        };

        var roundtripped = JsonSerializer.Deserialize<VpnProtectionSettingsDocument>(
            JsonSerializer.Serialize(server, JsonOptions), JsonOptions);
        var effective = new VpnProtectionSettingsMerger().Merge(global, roundtripped);

        Assert.False(effective.ValidationFailed);
        Assert.Equal(expected, Assert.Single(effective.Rules).PostObservationToForums);
        Assert.Equal(serverOverride, Assert.Single(roundtripped!.RuleOverrides).PostObservationToForums);
    }

    [Fact]
    public void Validate_InvalidConditionsAndUnsafeReason_ReturnsErrors()
    {
        var document = new VpnProtectionSettingsDocument
        {
            Rules =
            [
                new VpnProtectionRule
                {
                    Id = "vpn",
                    Condition = Leaf(VpnProtectionSignal.ProxyCheckIsVpn, RuleComparisonOperator.GreaterThan, "not-a-boolean"),
                    Action = VpnProtectionAction.Kick,
                    ReasonTemplate = "VPN; quit {unsupported}"
                },
                new VpnProtectionRule
                {
                    Id = "VPN",
                    Condition = Leaf(VpnProtectionSignal.ProxyCheckRiskScore, RuleComparisonOperator.GreaterThanOrEqual, "101"),
                    Action = VpnProtectionAction.Unknown
                },
                new VpnProtectionRule
                {
                    Id = "source-status",
                    Condition = Leaf(VpnProtectionSignal.ProxyCheckStatus, RuleComparisonOperator.Contains, "Success"),
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
    public void Validate_RejectsTreeBeyondSharedDepthLimit()
    {
        var condition = Leaf(VpnProtectionSignal.ProxyCheckIsVpn, RuleComparisonOperator.Equal, "true");
        for (var depth = 1; depth <= RuleConditionTreeConstants.MaxDepth; depth++)
        {
            condition = new VpnProtectionCondition
            {
                GroupOperator = RuleConditionGroupOperator.And,
                Conditions = [condition]
            };
        }

        var document = new VpnProtectionSettingsDocument
        {
            Rules = [new() { Id = "deep", Condition = condition, Action = VpnProtectionAction.Kick }]
        };

        Assert.False(new VpnProtectionSettingsValidator().Validate(document).IsValid);
    }

    [Fact]
    public void Validate_EnforcesSharedNodeLimit()
    {
        var root = new VpnProtectionCondition
        {
            GroupOperator = RuleConditionGroupOperator.And,
            Conditions = Enumerable.Range(0, RuleConditionTreeConstants.MaxConditionsPerRule - 1)
                .Select(_ => Leaf(VpnProtectionSignal.ProxyCheckIsVpn, RuleComparisonOperator.Equal, "true"))
                .ToList()
        };
        var document = new VpnProtectionSettingsDocument
        {
            Rules = [new() { Id = "nodes", Condition = root, Action = VpnProtectionAction.Kick }]
        };

        Assert.True(new VpnProtectionSettingsValidator().Validate(document).IsValid);
        root.Conditions!.Add(Leaf(VpnProtectionSignal.ProxyCheckIsVpn, RuleComparisonOperator.Equal, "true"));
        Assert.False(new VpnProtectionSettingsValidator().Validate(document).IsValid);
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
                    Condition = Leaf(VpnProtectionSignal.ProxyCheckIsProxy, RuleComparisonOperator.Equal, "true"),
                    ReasonTemplate = "Server VPN rule {ruleId}"
                },
                new VpnProtectionRuleOverride { Id = "tor", Enabled = false }
            ],
            Rules =
            [
                CreateRule("hosting", VpnProtectionSignal.MaxMindIsHostingProvider, "true", VpnProtectionAction.Observation)
            ],
            ExcludedPlayerTags = ["Server Exempt", "global exempt"]
        };

        var effective = new VpnProtectionSettingsMerger().Merge(global, server);

        Assert.True(effective.Enabled);
        Assert.False(effective.ValidationFailed);
        Assert.Equal(3, effective.Rules.Count);
        Assert.Equal(VpnProtectionAction.Kick, effective.Rules[0].Action);
        Assert.Equal(VpnProtectionSignal.ProxyCheckIsProxy, effective.Rules[0].Condition.Signal);
        Assert.Equal("Server VPN rule {ruleId}", effective.Rules[0].ReasonTemplate);
        Assert.False(effective.Rules[1].Enabled);
        Assert.Equal(VpnProtectionSignal.MaxMindIsTorExitNode, effective.Rules[1].Condition.Signal);
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
        Assert.Equal("local", Assert.Single(effective.Rules).Id);
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
            Condition = Leaf(signal, RuleComparisonOperator.Equal, expectedValue),
            Action = action
        };

    private static VpnProtectionCondition Leaf(
        VpnProtectionSignal signal,
        RuleComparisonOperator comparisonOperator,
        string expectedValue) => new()
        {
            Signal = signal,
            Operator = comparisonOperator,
            ExpectedValue = expectedValue
        };
}
