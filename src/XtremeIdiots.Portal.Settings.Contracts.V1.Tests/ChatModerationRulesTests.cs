using System.Text.Json;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Moderation;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Tests;

public sealed class ChatModerationRulesTests
{
    [Fact]
    public void FeatureIsDisabledAndStagedSeparatelyFromCurrentCapabilities()
    {
        var document = new ChatModerationRulesDocument();
        Assert.False(document.Enabled);
        Assert.True(new ChatModerationRulesValidator().Validate(document).IsValid);
        Assert.DoesNotContain(RulesEngineCatalogueV1.Current.Features, item => item.FeatureId == document.FeatureId);
        var feature = Assert.Single(ChatModerationCatalogueV1.Staged.Features,
            item => item.FeatureId == document.FeatureId);
        Assert.Equal([ChatModerationRulesConstants.TriggerId], feature.TriggerIds);
    }

    [Fact]
    public void ContentSafetyCategoryAndThresholdAreValidated()
    {
        var condition = new RuleConditionNode
        {
            ConditionId = ChatModerationRulesConstants.ContentSafety,
            Parameters = new()
            {
                ["category"] = JsonSerializer.SerializeToElement("Hate"),
                ["threshold"] = JsonSerializer.SerializeToElement(4)
            }
        };
        var document = Document(condition);
        var validator = new ChatModerationRulesValidator();
        Assert.True(validator.Validate(document).IsValid);
        condition.Parameters["threshold"] = JsonSerializer.SerializeToElement(7);
        Assert.False(validator.Validate(document).IsValid);
        condition.Parameters["threshold"] = JsonSerializer.SerializeToElement(4);
        condition.Parameters["category"] = JsonSerializer.SerializeToElement("FutureCategory");
        Assert.False(validator.Validate(document).IsValid);
    }

    [Fact]
    public void LiteralConditionRequiresSupportedPolicyAndValidatesOverrides()
    {
        var condition = new RuleConditionNode
        {
            ConditionId = ChatModerationRulesConstants.BannedWords,
            Parameters = new()
            {
                ["policy"] = JsonSerializer.SerializeToElement(new ChatLiteralPolicy
                {
                    MatcherVersion = ChatLiteralPolicyConstants.MatcherVersion,
                    Entries = [new() { Id = "word", Phrase = "word", MatchMode = ChatLiteralPolicyConstants.WholeWord }]
                }, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            }
        };
        var validator = new ChatModerationRulesValidator();
        Assert.True(validator.Validate(Document(condition)).IsValid);
        var server = new ChatModerationRulesServerDocument
        {
            RuleOverrides = [new() { RuleId = "rule", Condition = condition }]
        };
        Assert.True(validator.ValidateServerOverride(server).IsValid);
        condition.Parameters["policy"] = JsonSerializer.SerializeToElement(new { matcherVersion = "future", entries = Array.Empty<object>() });
        Assert.False(validator.ValidateServerOverride(server).IsValid);
        condition.Parameters["policy"] = JsonSerializer.SerializeToElement<object?>(null);
        Assert.False(validator.ValidateServerOverride(server).IsValid);
        server.RuleOverrides[0].Condition = null;
        Assert.True(validator.ValidateServerOverride(server).IsValid);
    }

    [Fact]
    public void PrivateNudgeCooldownRequiresValidPolicyAndDuration()
    {
        var document = Document(new RuleConditionNode { ConditionId = "always", Parameters = [] });
        document.Rules[0].Actions =
        [
            new()
            {
                Id = "nudge", TypeId = "message.private", Parameters = new()
                {
                    ["message"] = JsonSerializer.SerializeToElement("Please stay polite"),
                    ["cooldown"] = JsonSerializer.SerializeToElement(new { policyId = "nudge", durationSeconds = 60 })
                }
            }
        ];
        var validator = new ChatModerationRulesValidator();
        Assert.True(validator.Validate(document).IsValid);
        document.Rules[0].Actions[0].Parameters["cooldown"] =
            JsonSerializer.SerializeToElement(new { policyId = "nudge", durationSeconds = 0 });
        Assert.False(validator.Validate(document).IsValid);
        document.Rules[0].Actions[0].Parameters["cooldown"] =
            JsonSerializer.SerializeToElement(new { policyId = "nudge policy", durationSeconds = 60 });
        Assert.False(validator.Validate(document).IsValid);
    }

    private static ChatModerationRulesDocument Document(RuleConditionNode condition) => new()
    {
        Rules =
        [
            new()
            {
                Id = "rule", Condition = condition,
                Actions = [new() { Id = "observe", TypeId = "moderation.observation", Parameters = new()
                {
                    ["reason"] = JsonSerializer.SerializeToElement("Observed")
                } }]
            }
        ]
    };
}
