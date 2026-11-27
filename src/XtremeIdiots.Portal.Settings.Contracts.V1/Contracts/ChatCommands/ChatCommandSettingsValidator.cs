using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.ChatCommands;

public sealed class ChatCommandSettingsValidator
{
    private static readonly IReadOnlyDictionary<string, ConditionDefinition> Conditions =
        RulesEngineCatalogueV1.Current.Conditions.ToDictionary(item => item.ConditionId, StringComparer.Ordinal);

    private static readonly RuleSetValidationOptions PolicyValidationOptions = new()
    {
        FeatureId = "chat-commands",
        AllowedConditionIds = Conditions.Keys.ToHashSet(StringComparer.Ordinal),
        AllowedActionTypeIds = new HashSet<string>(StringComparer.Ordinal),
        ConditionDefinitions = Conditions
    };

    public SettingsValidationResult Validate(ChatCommandSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{ChatCommandSettingsConstants.Namespace}'.");
            return result;
        }

        ValidateDefaults(document.Defaults, result);

        if (document.Commands is null)
        {
            result.Errors.Add("commands must be an object.");
            return result;
        }

        foreach (var pair in document.Commands)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                result.Errors.Add("Command key cannot be empty.");
                continue;
            }

            if (!string.Equals(pair.Key, pair.Key.Trim(), StringComparison.Ordinal))
            {
                result.Errors.Add($"Command key '{pair.Key}' cannot contain leading or trailing whitespace.");
                continue;
            }

            if (pair.Value is null)
            {
                result.Errors.Add($"commands.{pair.Key} must be an object.");
                continue;
            }

            ValidateEntry($"commands.{pair.Key}", pair.Key, pair.Value, result);
        }

        return result;
    }

    private static void ValidateDefaults(ChatCommandSettingsDefaults? defaults, SettingsValidationResult result)
    {
        if (defaults is null)
        {
            return;
        }

        if (defaults.FreshnessSeconds is not null)
        {
            ValidateNonNegative(defaults.FreshnessSeconds.Default, "defaults.freshnessSeconds.default", result);
            ValidateNonNegative(defaults.FreshnessSeconds.ReadOnly, "defaults.freshnessSeconds.readOnly", result);
            ValidateNonNegative(defaults.FreshnessSeconds.Mutating, "defaults.freshnessSeconds.mutating", result);
        }

        ValidateStringArray(defaults.RequiredTags, "defaults.requiredTags", result);
    }

    private static void ValidateEntry(string path, string commandName, ChatCommandSettingsEntry entry, SettingsValidationResult result)
    {
        ValidateNonNegative(entry.FreshnessSeconds, $"{path}.freshnessSeconds", result);
        ValidateStringArray(entry.RequiredTags, $"{path}.requiredTags", result);
        ValidatePolicy(entry.Policy, $"{path}.policy", result);
        ValidateReplies(commandName, entry.Replies, $"{path}.replies", result);
    }

    private static void ValidatePolicy(ChatCommandPolicy? policy, string path, SettingsValidationResult result)
    {
        if (policy is null)
        {
            return;
        }
        if (policy.DefaultDecision is not (ChatCommandPolicyDecision.Allow or ChatCommandPolicyDecision.Deny))
        {
            result.Errors.Add($"{path}.defaultDecision must be Allow or Deny.");
        }
        if (policy.Rules is null)
        {
            result.Errors.Add($"{path}.rules is required.");
            return;
        }
        if (policy.Rules.Count > RuleSetConstants.MaxRules)
        {
            result.Errors.Add($"{path}.rules supports at most {RuleSetConstants.MaxRules} rules.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < policy.Rules.Count; index++)
        {
            var rule = policy.Rules[index];
            var rulePath = $"{path}.rules[{index}]";
            if (rule is null)
            {
                result.Errors.Add($"{rulePath} must be an object.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(rule.Id) || rule.Id.Length > RuleSetConstants.MaxIdLength ||
                rule.Id.Trim() != rule.Id || !ids.Add(rule.Id))
            {
                result.Errors.Add($"{rulePath}.id must be nonempty, unique, and at most {RuleSetConstants.MaxIdLength} characters.");
            }
            if (rule.Decision is not (ChatCommandPolicyDecision.Allow or ChatCommandPolicyDecision.Deny))
            {
                result.Errors.Add($"{rulePath}.decision must be Allow or Deny.");
            }
            var condition = new RuleSetValidator().ValidateConditionTree(rule.Condition, PolicyValidationOptions);
            result.Errors.AddRange(condition.Errors.Select(error => $"{rulePath}.{error}"));
        }
    }

    private static void ValidateReplies(
        string commandName, Dictionary<string, string[]?>? replies, string path, SettingsValidationResult result)
    {
        if (replies is null)
        {
            return;
        }
        var definitions = ChatCommandCatalogueV1.Find(commandName)?.Replies
            .ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, templates) in replies)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Trim() != key || !keys.Add(key))
            {
                result.Errors.Add($"{path} contains an empty or duplicate result key.");
                continue;
            }
            if (templates is null)
            {
                continue;
            }
            var definition = definitions?.GetValueOrDefault(key);
            if (templates.Length == 0 && definition?.Required == true)
            {
                result.Errors.Add($"{path}.{key} cannot suppress a required reply.");
            }
            for (var index = 0; index < templates.Length; index++)
            {
                var template = templates[index];
                if (string.IsNullOrWhiteSpace(template) || template.Length > ChatCommandSettingsConstants.MaxReplyTemplateLength ||
                    template.Any(char.IsControl))
                {
                    result.Errors.Add($"{path}.{key}[{index}] must be nonempty, bounded, and contain no control characters.");
                }
                else if (definition is not null && !HasOnlyAllowedTokens(template, definition.AllowedTokens))
                {
                    result.Errors.Add($"{path}.{key}[{index}] contains an unsupported or malformed token.");
                }
            }
        }
    }

    private static bool HasOnlyAllowedTokens(string template, IReadOnlyList<string> allowed)
    {
        for (var index = 0; index < template.Length; index++)
        {
            if (template[index] == '}')
            {
                return false;
            }
            if (template[index] != '{')
            {
                continue;
            }
            var end = template.IndexOf('}', index + 1);
            if (end < 0 || !allowed.Contains(template[(index + 1)..end], StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
            index = end;
        }
        return true;
    }

    private static void ValidateNonNegative(int? value, string path, SettingsValidationResult result)
    {
        if (value.HasValue && value.Value < 0)
        {
            result.Errors.Add($"{path} must be >= 0.");
        }
    }

    private static void ValidateStringArray(string[]? values, string path, SettingsValidationResult result)
    {
        if (values is null)
        {
            return;
        }

        for (var i = 0; i < values.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(values[i]))
            {
                result.Errors.Add($"{path}[{i}] cannot be empty.");
            }
        }
    }
}
