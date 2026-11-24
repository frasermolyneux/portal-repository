using System.Text.Json;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.SiegeMode;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Tests;

public sealed class SiegeModeSettingsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void DefaultsAreDisabledWithNoRulesAndValidLifecycleDuration()
    {
        var document = new SiegeModeSettingsDocument();
        var roundtripped = JsonSerializer.Deserialize<SiegeModeSettingsDocument>(
            JsonSerializer.Serialize(document, JsonOptions), JsonOptions)!;

        Assert.False(roundtripped.Enabled);
        Assert.Equal(240, roundtripped.DurationMinutes);
        Assert.Empty(roundtripped.Rules);
        Assert.True(new SiegeModeSettingsValidator().Validate(roundtripped).IsValid);
    }

    [Fact]
    public void ServerOverrideMergesLocalAndGlobalRulesAndDurationWithoutChangingEnablementRules()
    {
        var global = new SiegeModeSettingsDocument { Enabled = true, DurationMinutes = 180, Rules = [Rule("global")] };
        var server = new SiegeModeServerSettingsOverrideDocument
        {
            EnabledOverride = FeatureEnablementOverride.Enabled,
            DurationMinutesOverride = 60,
            InheritGlobalRules = true,
            Rules = [Rule("local")]
        };

        var effective = SiegeModeSettingsMerger.Merge(global, server);
        var frozen = SiegeModeSettingsMerger.Freeze(effective);

        Assert.True(effective.Enabled);
        Assert.Equal(60, effective.DurationMinutes);
        Assert.Equal(["global", "local"], effective.Rules.Select(static rule => rule.Id));
        Assert.True(new SiegeModeSettingsValidator().Validate(frozen).IsValid);
        Assert.True(new SiegeModeSettingsValidator().ValidateServerOverride(server).IsValid);
    }

    [Fact]
    public void ServerCanDisableGlobalInheritanceIndependentlyFromSiegeEnablement()
    {
        var global = new SiegeModeSettingsDocument { Enabled = true, Rules = [Rule("global")] };
        var server = new SiegeModeServerSettingsOverrideDocument
        {
            EnabledOverride = FeatureEnablementOverride.Disabled,
            InheritGlobalRules = false,
            Rules = [Rule("local")]
        };

        var effective = SiegeModeSettingsMerger.Merge(global, server);

        Assert.False(effective.Enabled);
        Assert.Equal("local", Assert.Single(effective.Rules).Id);
    }

    [Fact]
    public void InvalidDurationAndConditionParameterAreRejected()
    {
        var document = new SiegeModeSettingsDocument
        {
            Enabled = true,
            DurationMinutes = 0,
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
                            ["operator"] = JsonDocument.Parse("\"IsNew\"").RootElement.Clone(),
                            ["referenceScope"] = JsonDocument.Parse("\"Other\"").RootElement.Clone()
                        }
                    },
                    Actions = [new() { Id = "observe", TypeId = "moderation.observation" }]
                }
            ]
        };

        var validation = new SiegeModeSettingsValidator().Validate(document);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, static error => error.Contains("durationMinutes", StringComparison.Ordinal));
        Assert.Contains(validation.Errors, static error => error.Contains("referenceScope", StringComparison.Ordinal));
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
                ["referenceScope"] = JsonDocument.Parse("\"SiegeBaseline\"").RootElement.Clone()
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
                    ["reason"] = JsonDocument.Parse("\"matched\"").RootElement.Clone()
                }
            }
        ]
    };
}
