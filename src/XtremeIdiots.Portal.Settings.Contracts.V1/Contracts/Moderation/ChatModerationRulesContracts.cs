using System.Text.Json;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Moderation;

public static class ChatModerationRulesConstants
{
    public const string Namespace = "chatModerationRules";
    public const string FeatureId = "chat-moderation";
    public const string TriggerId = "chat.message";
    public const string PlayerFirstSeen = "chat.player-first-seen";
    public const string PlayerTagName = "chat.player-tag-name";
    public const string MinimumLength = "chat.minimum-length";
    public const string Audience = "chat.audience";
    public const string ContentSafety = "chat.content-safety";
    public const string BannedWords = "chat.banned-words";
    public const string Hackusation = "chat.hackusation";
}

public sealed class ChatModerationRulesDocument : FeatureRuleSetDocument
{
    public ChatModerationRulesDocument()
    {
        FeatureId = ChatModerationRulesConstants.FeatureId;
        Enabled = false;
    }
}

public sealed class ChatModerationRulesServerDocument : FeatureRuleSetOverrideDocument
{
    public ChatModerationRulesServerDocument()
    {
        FeatureId = ChatModerationRulesConstants.FeatureId;
        EnabledOverride = FeatureEnablementOverride.Inherit;
    }
}

public sealed class ChatModerationCooldown
{
    public string PolicyId { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
}

public static class ChatModerationCatalogueV1
{
    public static RulesEngineCatalogue Staged { get; } = Create();

    private static RulesEngineCatalogue Create()
    {
        var current = RulesEngineCatalogueV1.Current;
        var games = RulesEngineCatalogueV1.SupportedGameTypes;
        var conditions = new ConditionDefinition[]
        {
            new()
            {
                ConditionId = ChatModerationRulesConstants.PlayerFirstSeen, DisplayName = "Player first seen",
                ValueType = "boolean", GameTypes = games,
                Parameters = [new() { Name = "withinDays", Type = "integer", Required = true, Minimum = 0, Maximum = 36500, UiControl = "number" }]
            },
            new()
            {
                ConditionId = ChatModerationRulesConstants.PlayerTagName, DisplayName = "Player tag name",
                ValueType = "boolean", GameTypes = games,
                Parameters = [new() { Name = "tagName", Type = "string", Required = true, MaxLength = 60, UiControl = "text" }]
            },
            new()
            {
                ConditionId = ChatModerationRulesConstants.MinimumLength, DisplayName = "Minimum message length",
                ValueType = "boolean", GameTypes = games,
                Parameters = [new() { Name = "minimum", Type = "integer", Required = true, Minimum = 1, UiControl = "number" }]
            },
            new()
            {
                ConditionId = ChatModerationRulesConstants.Audience, DisplayName = "Chat audience",
                ValueType = "boolean", GameTypes = games,
                Parameters = [new() { Name = "scope", Type = "string", Required = true, AllowedValues = ["All", "Team"], UiControl = "select" }]
            },
            new()
            {
                ConditionId = ChatModerationRulesConstants.ContentSafety, DisplayName = "Content Safety",
                ValueType = "boolean", GameTypes = games,
                Parameters =
                [
                    new() { Name = "category", Type = "string", Required = true, AllowedValues = ["Hate", "Violence", "Sexual", "SelfHarm"], UiControl = "select" },
                    new() { Name = "threshold", Type = "integer", Required = true, Minimum = 0, Maximum = 6, UiControl = "number" }
                ]
            },
            LiteralCondition(ChatModerationRulesConstants.BannedWords, "Banned words"),
            LiteralCondition(ChatModerationRulesConstants.Hackusation, "Hackusation phrases")
        };
        var actions = current.Actions.Select(action => action.ActionTypeId == "message.private"
            ? action with
            {
                Parameters = [.. action.Parameters, new() { Name = "cooldown", Type = "object", UiControl = "cooldown" }]
            }
            : action).ToArray();
        return current with
        {
            Triggers = [.. current.Triggers, new()
            {
                TriggerId = ChatModerationRulesConstants.TriggerId, DisplayName = "Chat message", GameTypes = games,
                ContextFields = ["gameServerId", "occurrenceId", "playerId", "chatType", "message"],
                CorrelationScope = "occurrence"
            }],
            Conditions = [.. current.Conditions, .. conditions],
            Actions = actions,
            Features = [.. current.Features, new()
            {
                FeatureId = ChatModerationRulesConstants.FeatureId, DisplayName = "Chat moderation",
                GameTypes = games, TriggerIds = [ChatModerationRulesConstants.TriggerId],
                ConditionIds = [RulesEngineCatalogueV1.AlwaysCondition, .. conditions.Select(item => item.ConditionId)],
                ActionTypeIds = ["moderation.observation", "moderation.kick-with-record", "moderation.temporary-ban", "moderation.ban", "message.private"],
                SupportsGlobalRules = true, SupportsServerOverrides = true
            }]
        };
    }

    private static ConditionDefinition LiteralCondition(string id, string name) => new()
    {
        ConditionId = id,
        DisplayName = name,
        ValueType = "boolean",
        GameTypes = RulesEngineCatalogueV1.SupportedGameTypes,
        Parameters = [new() { Name = "policy", Type = "object", Required = true, UiControl = "literal-policy" }]
    };
}

public sealed class ChatModerationRulesValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly RuleSetValidationOptions Options = CreateOptions();

    public SettingsValidationResult Validate(ChatModerationRulesDocument? document)
    {
        var result = new RuleSetValidator().Validate(document, Options);
        if (document is not null && result.IsValid)
        {
            if (document.Timing is not null)
            {
                result.Errors.Add("Chat moderation does not support connection timing.");
            }
            ValidateRules(document.Rules, result);
        }
        return result;
    }

    public SettingsValidationResult ValidateServerOverride(ChatModerationRulesServerDocument? document)
    {
        var result = new RuleSetValidator().ValidateOverride(document, Options);
        if (document is not null && result.IsValid)
        {
            if (document.Timing is not null)
            {
                result.Errors.Add("Chat moderation does not support connection timing.");
            }
            ValidateRules(document.Rules, result);
            if (document.RuleOverrides is not null)
            {
                foreach (var item in document.RuleOverrides)
                {
                    if (item is null)
                    {
                        continue;
                    }
                    if (item.Timing is not null)
                    {
                        result.Errors.Add("Chat moderation rules do not support connection timing.");
                    }
                    ValidateCondition(item.Condition, 0, result);
                    ValidateActions(item.Actions, result);
                }
            }
        }
        return result;
    }

    private static RuleSetValidationOptions CreateOptions()
    {
        var catalogue = ChatModerationCatalogueV1.Staged;
        var feature = catalogue.Features.Single(item => item.FeatureId == ChatModerationRulesConstants.FeatureId);
        return new RuleSetValidationOptions
        {
            FeatureId = feature.FeatureId,
            AllowedConditionIds = feature.ConditionIds.ToHashSet(StringComparer.Ordinal),
            AllowedActionTypeIds = feature.ActionTypeIds.ToHashSet(StringComparer.Ordinal),
            ConditionDefinitions = catalogue.Conditions.ToDictionary(item => item.ConditionId, StringComparer.Ordinal),
            ActionDefinitions = catalogue.Actions.ToDictionary(item => item.ActionTypeId, StringComparer.Ordinal)
        };
    }

    private static void ValidateRules(IEnumerable<RuleDefinition>? rules, SettingsValidationResult result)
    {
        if (rules is null)
        {
            return;
        }
        foreach (var rule in rules)
        {
            if (rule is null)
            {
                continue;
            }
            if (rule.Timing is not null)
            {
                result.Errors.Add("Chat moderation rules do not support connection timing.");
            }
            ValidateCondition(rule.Condition, 0, result);
            ValidateActions(rule.Actions, result);
        }
    }

    private static void ValidateCondition(RuleConditionNode? condition, int depth, SettingsValidationResult result)
    {
        if (condition is null || depth > 8)
        {
            return;
        }
        if (condition.Conditions is not null)
        {
            foreach (var child in condition.Conditions)
            {
                ValidateCondition(child, depth + 1, result);
            }
        }
        if (condition.ConditionId == ChatModerationRulesConstants.PlayerTagName &&
            condition.Parameters?.TryGetValue("tagName", out var tagName) == true &&
            tagName.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(tagName.GetString()))
        {
            result.Errors.Add("A player tag name cannot be blank.");
        }
        if (condition.ConditionId is not (ChatModerationRulesConstants.BannedWords or ChatModerationRulesConstants.Hackusation) ||
            condition.Parameters?.TryGetValue("policy", out var value) != true || value.ValueKind != JsonValueKind.Object)
        {
            return;
        }
        try
        {
            var validation = new ChatLiteralPolicyValidator().Validate(value.Deserialize<ChatLiteralPolicy>(JsonOptions));
            result.Errors.AddRange(validation.Errors);
        }
        catch (JsonException)
        {
            result.Errors.Add("The literal policy is invalid.");
        }
    }

    private static void ValidateActions(IEnumerable<RuleActionDefinition>? actions, SettingsValidationResult result)
    {
        if (actions is null)
        {
            return;
        }
        foreach (var action in actions)
        {
            if (action?.TypeId != "message.private" ||
                action.Parameters?.TryGetValue("cooldown", out var value) != true ||
                value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }
            try
            {
                var cooldown = value.Deserialize<ChatModerationCooldown>(JsonOptions);
                if (cooldown is null || string.IsNullOrWhiteSpace(cooldown.PolicyId) ||
                    cooldown.PolicyId.Length > RuleSetConstants.MaxIdLength ||
                    !cooldown.PolicyId.All(static character => char.IsAsciiLetterOrDigit(character) ||
                        character is '.' or '_' or '-' or ':' or '+') || cooldown.DurationSeconds <= 0)
                {
                    result.Errors.Add("The private-message cooldown is invalid.");
                }
            }
            catch (JsonException)
            {
                result.Errors.Add("The private-message cooldown is invalid.");
            }
        }
    }
}
