using System.Text.Json;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.WelcomeMessages;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Tests;

public sealed class WelcomeRulesSettingsTests
{
    [Fact]
    public void WelcomeUsesOrdinaryRulesAndSeparateDisabledNamespace()
    {
        var document = new WelcomeRulesSettingsDocument { Rules = [WelcomeRule()] };
        Assert.False(document.Enabled);
        Assert.True(new WelcomeRulesSettingsValidator().Validate(document).IsValid);
        var feature = RulesEngineCatalogueV1.Current.Features.Single(item => item.FeatureId == document.FeatureId);
        Assert.Equal(["player.connected"], feature.TriggerIds);
        Assert.DoesNotContain("player.kick", feature.ActionTypeIds);
    }

    [Fact]
    public void TimingMergesAtEachScopeWithoutResettingRuleOverrides()
    {
        var rule = WelcomeRule();
        rule.Timing = new() { ConnectionDelaySeconds = 12 };
        var global = new WelcomeRulesSettingsDocument
        {
            Timing = new() { ConnectionDelaySeconds = 8, FreshnessSeconds = 100 },
            Rules = [rule]
        };
        var server = new WelcomeRulesServerSettingsDocument
        {
            Timing = new() { ConnectionDelaySeconds = 3 },
            RuleOverrides = [new() { RuleId = "general", Timing = new() { FreshnessSeconds = 80 } }]
        };
        var merged = FeatureRuleSetMerger.Merge(global, server);
        Assert.Equal(3, merged.Timing.ConnectionDelaySeconds);
        var effective = merged.Timing.Apply(Assert.Single(merged.Rules).Timing);
        Assert.Equal(12, effective.ConnectionDelaySeconds);
        Assert.Equal(80, effective.FreshnessSeconds);
        server.Timing = new() { FreshnessSeconds = 10 };
        server.RuleOverrides.Clear();
        Assert.Throws<ArgumentException>(() => FeatureRuleSetMerger.Merge(global, server));
    }

    [Fact]
    public void TemplateModeIsExplicitAndOnlyApprovedTextCanOptIn()
    {
        var rule = WelcomeRule();
        var action = rule.Actions[0];
        action.TemplateParameters.Clear();
        action.Parameters["message"] = JsonSerializer.SerializeToElement("literal {not-a-token}");
        var document = new WelcomeRulesSettingsDocument { Rules = [rule] };
        var validator = new WelcomeRulesSettingsValidator();
        Assert.True(validator.Validate(document).IsValid);
        action.TemplateParameters = ["message"];
        Assert.False(validator.Validate(document).IsValid);
        action.Parameters["message"] = JsonSerializer.SerializeToElement("Hello {context.player.Name}");
        Assert.True(validator.Validate(document).IsValid);
        action.TemplateParameters = ["durationMinutes"];
        Assert.False(validator.Validate(document).IsValid);
    }

    [Fact]
    public void OrderedListsAllowLongMessagesAndValidateEntireBatch()
    {
        var rule = WelcomeRule();
        rule.Actions[0].Parameters = new()
        {
            ["messages"] = JsonSerializer.SerializeToElement(new[] { new string('x', 500), "second" })
        };
        rule.Actions[0].TemplateParameters = ["messages"];
        var document = new WelcomeRulesSettingsDocument { Rules = [rule] };
        var validator = new WelcomeRulesSettingsValidator();
        Assert.True(validator.Validate(document).IsValid);
        rule.Actions[0].Parameters["messages"] = JsonSerializer.SerializeToElement(new[] { "first", "bad\n" });
        Assert.False(validator.Validate(document).IsValid);
        rule.Actions[0].Parameters.Clear();
        rule.Actions[0].TemplateParameters.Clear();
        Assert.False(validator.Validate(document).IsValid);
    }

    [Theory]
    [InlineData(61, 120)]
    [InlineData(5, 5)]
    [InlineData(0, 901)]
    public void TimingRejectsInvalidDeliveryWindow(int delay, int freshness)
    {
        var document = new WelcomeRulesSettingsDocument
        {
            Timing = new() { ConnectionDelaySeconds = delay, FreshnessSeconds = freshness }
        };
        Assert.False(new WelcomeRulesSettingsValidator().Validate(document).IsValid);
    }

    private static RuleDefinition WelcomeRule() => new()
    {
        Id = "general",
        Condition = new() { ConditionId = "always", Parameters = [] },
        Actions = [new() { Id = "hello", TypeId = "message.public", Parameters = new()
        {
            ["message"] = JsonSerializer.SerializeToElement("Welcome {context.player.Name}")
        }, TemplateParameters = ["message"] }]
    };
}
