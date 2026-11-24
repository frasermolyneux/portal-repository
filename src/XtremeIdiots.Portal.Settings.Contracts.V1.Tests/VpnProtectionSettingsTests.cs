using System.Text.Json;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Tests;

public sealed class VpnProtectionSettingsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void GlobalAndServerContractsUseUniversalRuleShape()
    {
        var global = new VpnProtectionSettingsDocument
        {
            Enabled = true,
            Rules = [Rule("global")]
        };
        var server = new VpnProtectionServerSettingsDocument
        {
            EnabledOverride = FeatureEnablementOverride.Disabled,
            InheritGlobalRules = true,
            RuleOverrides = [new() { RuleId = "global", Enabled = false }],
            Rules = [Rule("local")]
        };

        var globalJson = JsonSerializer.Serialize(global, JsonOptions);
        var serverJson = JsonSerializer.Serialize(server, JsonOptions);
        var roundtrippedGlobal = JsonSerializer.Deserialize<VpnProtectionSettingsDocument>(globalJson, JsonOptions)!;
        var roundtrippedServer = JsonSerializer.Deserialize<VpnProtectionServerSettingsDocument>(serverJson, JsonOptions)!;

        Assert.Equal(VpnProtectionSettingsConstants.FeatureId, roundtrippedGlobal.FeatureId);
        Assert.True(new VpnProtectionSettingsValidator().Validate(roundtrippedGlobal).IsValid);
        Assert.True(new VpnProtectionSettingsValidator().ValidateServerOverride(roundtrippedServer).IsValid);
        Assert.Equal(FeatureEnablementOverride.Disabled, roundtrippedServer.EnabledOverride);
        Assert.True(roundtrippedServer.InheritGlobalRules);
        Assert.Single(roundtrippedServer.Rules);
        Assert.Single(roundtrippedServer.RuleOverrides);
    }

    [Fact]
    public void MergeKeepsFeatureEnablementIndependentFromRuleInheritanceAndAppendsLocalRules()
    {
        var global = new VpnProtectionSettingsDocument { Enabled = true, Rules = [Rule("global")] };
        var server = new VpnProtectionServerSettingsDocument
        {
            EnabledOverride = FeatureEnablementOverride.Disabled,
            InheritGlobalRules = true,
            RuleOverrides = [new() { RuleId = "global", Enabled = false }],
            Rules = [Rule("local")]
        };

        var inherited = FeatureRuleSetMerger.Merge(global, server);
        Assert.False(inherited.Enabled);
        Assert.Equal(["global", "local"], inherited.Rules.Select(static rule => rule.Id));
        Assert.False(inherited.Rules[0].Enabled);
        Assert.Equal([0, 1], inherited.Rules.Select(static rule => rule.Order));

        server.InheritGlobalRules = false;
        var localOnly = FeatureRuleSetMerger.Merge(global, server);
        Assert.False(localOnly.Enabled);
        Assert.Equal("local", Assert.Single(localOnly.Rules).Id);
    }

    [Fact]
    public void UnknownConditionActionTypeAndParametersRoundtripWithoutLoss()
    {
        const string input = """
            {
              "schemaVersion": 1,
              "featureId": "vpn-protection",
              "enabled": true,
              "rules": [{
                "id": "future",
                "condition": {
                  "conditionId": "future.signal",
                  "parameters": { "newParam": { "nested": [1, 2, 3] } },
                  "futureConditionProperty": true
                },
                "actions": [{
                  "id": "action",
                  "typeId": "moderation.observation",
                  "parameters": {
                    "reason": "reason",
                    "futureActionParameter": { "value": "unchanged" }
                  }
                }, {
                  "id": "future-action",
                  "typeId": "moderation.future-action",
                  "parameters": { "newParameter": { "nested": [1, 2, 3] } }
                }]
              }]
            }
            """;

        var document = JsonSerializer.Deserialize<VpnProtectionSettingsDocument>(input, JsonOptions)!;
        Assert.True(new VpnProtectionSettingsValidator().Validate(document).IsValid);
        var roundtrip = JsonSerializer.Serialize(document, JsonOptions);
        using var parsed = JsonDocument.Parse(roundtrip);
        Assert.True(parsed.RootElement.GetProperty("rules")[0].GetProperty("condition")
            .GetProperty("futureConditionProperty").GetBoolean());
        Assert.Equal("unchanged", parsed.RootElement.GetProperty("rules")[0].GetProperty("actions")[0]
            .GetProperty("parameters").GetProperty("futureActionParameter").GetProperty("value").GetString());
        Assert.Equal("moderation.future-action", parsed.RootElement.GetProperty("rules")[0].GetProperty("actions")[1]
            .GetProperty("typeId").GetString());
    }

    [Fact]
    public void ValidationRejectsInvalidKnownParameters()
    {
        var document = new VpnProtectionSettingsDocument
        {
            Enabled = true,
            Rules =
            [
                new()
                {
                    Id = "invalid",
                    Condition = new()
                    {
                        ConditionId = "player.first-seen-guid",
                        Parameters = new Dictionary<string, JsonElement>
                        {
                            ["operator"] = JsonDocument.Parse("\"Unknown\"").RootElement.Clone(),
                            ["referenceScope"] = JsonDocument.Parse("\"ObservedHistory\"").RootElement.Clone()
                        }
                    },
                    Actions = [new() { Id = "action", TypeId = "unsupported" }]
                }
            ]
        };

        var validation = new VpnProtectionSettingsValidator().Validate(document);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, static error => error.Contains("operator", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidationRejectsInvalidCrossFieldConditionCombinations()
    {
        Assert.False(ValidateCondition("player.first-seen-guid", new Dictionary<string, JsonElement>
        {
            ["operator"] = JsonSerializer.SerializeToElement("WithinAge"),
            ["referenceScope"] = JsonSerializer.SerializeToElement("ObservedHistory")
        }).IsValid);

        Assert.False(ValidateCondition("player.tags", new Dictionary<string, JsonElement>
        {
            ["operator"] = JsonSerializer.SerializeToElement("ContainsAny"),
            ["tags"] = JsonSerializer.SerializeToElement(Array.Empty<string>())
        }).IsValid);

        Assert.False(ValidateCondition("player.tags", new Dictionary<string, JsonElement>
        {
            ["operator"] = JsonSerializer.SerializeToElement("ContainsAny"),
            ["tags"] = JsonSerializer.SerializeToElement(new[] { "not-a-guid" })
        }).IsValid);

        Assert.False(ValidateCondition("vpn.intelligence", new Dictionary<string, JsonElement>
        {
            ["signal"] = JsonSerializer.SerializeToElement(nameof(VpnProtectionSignal.ProxyCheckIsVpn)),
            ["operator"] = JsonSerializer.SerializeToElement("Contains"),
            ["expectedValue"] = JsonSerializer.SerializeToElement("true")
        }).IsValid);

        Assert.False(ValidateCondition("vpn.intelligence", new Dictionary<string, JsonElement>
        {
            ["signal"] = JsonSerializer.SerializeToElement(nameof(VpnProtectionSignal.ProxyCheckRiskScore)),
            ["operator"] = JsonSerializer.SerializeToElement("GreaterThan"),
            ["expectedValue"] = JsonSerializer.SerializeToElement("101")
        }).IsValid);

        Assert.True(ValidateCondition("player.first-seen-guid", new Dictionary<string, JsonElement>
        {
            ["operator"] = JsonSerializer.SerializeToElement("WithinAge"),
            ["value"] = JsonSerializer.SerializeToElement("14.00:00:00"),
            ["referenceScope"] = JsonSerializer.SerializeToElement("ObservedHistory")
        }).IsValid);

        Assert.False(ValidateCondition("player.first-seen-guid", new Dictionary<string, JsonElement>
        {
            ["operator"] = JsonSerializer.SerializeToElement("IsNew"),
            ["referenceScope"] = JsonSerializer.SerializeToElement("SiegeBaseline")
        }).IsValid);
        Assert.True(ValidateCondition("player.first-seen-guid", new Dictionary<string, JsonElement>
        {
            ["operator"] = JsonSerializer.SerializeToElement("IsNew"),
            ["referenceScope"] = JsonSerializer.SerializeToElement("SiegeBaseline")
        }, RulesEngineCatalogueV1.SiegeModeFeature).IsValid);
    }

    [Fact]
    public void CatalogueDeclaresGameAndExecutorCapabilityAlternatives()
    {
        var catalogue = RulesEngineCatalogueV1.Current;
        Assert.All(catalogue.Features, feature => Assert.Equal(new[] { 1, 2, 3, 24 }, feature.GameTypes));
        Assert.All(catalogue.Triggers, trigger => Assert.Equal(new[] { 1, 2, 3, 24 }, trigger.GameTypes));
        Assert.All(catalogue.Conditions, condition => Assert.Equal(new[] { 1, 2, 3, 24 }, condition.GameTypes));
        Assert.All(catalogue.Actions, action => Assert.Equal(new[] { 1, 2, 3, 24 }, action.GameTypes));

        var drop = catalogue.Actions.Single(action => action.ActionTypeId == "player.kick");
        Assert.Contains(drop.ExecutionProviders, provider =>
            provider.ExecutorId == "cod4x-plugin" &&
            provider.GameTypes.SequenceEqual(new[] { 24 }) &&
            provider.RequiredRuntimeCapabilities.Contains(RulesEngineCatalogueV1.Cod4xPlayerDropCapability));
        Assert.Contains(drop.ExecutionProviders, provider =>
            provider.ExecutorId == "rcon" &&
            provider.RequiredIntegrationCapabilities.Contains(RulesEngineCatalogueV1.RconPlayerDropCapability));

        var rconMessage = catalogue.Actions.Single(action => action.ActionTypeId == "message.public")
            .ExecutionProviders.Single(provider => provider.ExecutorId == "rcon");
        Assert.Equal("RconSafePrintableAscii", Assert.Single(rconMessage.ParameterConstraints).AllowedCharacterSet);
    }

    [Fact]
    public void NativeTextValidationEnforcesUtf8ByteLimitAndRejectsControlCharacters()
    {
        Assert.True(ValidatePublicMessage(new string('a', 256)).IsValid);
        Assert.False(ValidatePublicMessage(new string('a', 257)).IsValid);
        Assert.False(ValidatePublicMessage(new string('é', 129)).IsValid);
        Assert.False(ValidatePublicMessage("line\u000abreak").IsValid);
        Assert.False(ValidatePublicMessage("bad\u007ftext").IsValid);
    }

    private static SettingsValidationResult ValidatePublicMessage(string message)
    {
        var document = new VpnProtectionSettingsDocument
        {
            Enabled = true,
            Rules =
            [
                new()
                {
                    Id = "message",
                    Condition = new()
                    {
                        ConditionId = "player.first-seen-guid",
                        Parameters = new Dictionary<string, JsonElement>
                        {
                            ["operator"] = JsonDocument.Parse("\"IsNew\"").RootElement.Clone(),
                            ["referenceScope"] = JsonDocument.Parse("\"ObservedHistory\"").RootElement.Clone()
                        }
                    },
                    Actions =
                    [
                        new()
                        {
                            Id = "message",
                            TypeId = "message.public",
                            Parameters = new Dictionary<string, JsonElement>
                            {
                                ["message"] = JsonDocument.Parse(JsonSerializer.Serialize(message)).RootElement.Clone()
                            }
                        }
                    ]
                }
            ]
        };
        return new VpnProtectionSettingsValidator().Validate(document);
    }

    private static SettingsValidationResult ValidateCondition(
        string conditionId,
        Dictionary<string, JsonElement> parameters,
        string featureId = RulesEngineCatalogueV1.VpnProtectionFeature)
    {
        var document = new FeatureRuleSetDocument
        {
            FeatureId = featureId,
            Enabled = true,
            Rules =
            [
                new()
                {
                    Id = "condition",
                    Condition = new() { ConditionId = conditionId, Parameters = parameters },
                    Actions =
                    [
                        new()
                        {
                            Id = "observe",
                            TypeId = "moderation.observation",
                            Parameters = new Dictionary<string, JsonElement>
                            {
                                ["reason"] = JsonSerializer.SerializeToElement("condition test")
                            }
                        }
                    ]
                }
            ]
        };
        return new RuleSetValidator().Validate(document, RuleSetValidationOptions.ForFeature(featureId));
    }

    private static RuleDefinition Rule(string id) => new()
    {
        Id = id,
        Condition = new()
        {
            ConditionId = "player.first-seen-guid",
            Parameters = new Dictionary<string, JsonElement>
            {
                ["operator"] = JsonDocument.Parse("\"IsNew\"").RootElement.Clone(),
                ["referenceScope"] = JsonDocument.Parse("\"ObservedHistory\"").RootElement.Clone()
            }
        },
        Actions =
        [
            new()
            {
                Id = "observe",
                TypeId = "moderation.observation",
                Parameters = new Dictionary<string, JsonElement>
                {
                    ["reason"] = JsonDocument.Parse("\"matched\"").RootElement.Clone(),
                    ["postToForums"] = JsonDocument.Parse("true").RootElement.Clone()
                }
            }
        ]
    };
}
