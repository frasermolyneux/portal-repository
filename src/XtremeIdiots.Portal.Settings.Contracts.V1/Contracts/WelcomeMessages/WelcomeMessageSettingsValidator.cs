using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.WelcomeMessages;

public sealed class WelcomeMessageSettingsValidator
{
    public SettingsValidationResult Validate(WelcomeMessageSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{WelcomeMessageSettingsConstants.Namespace}'.");
            return result;
        }

        ValidateDefaults(document.Defaults, result);
        ValidateRules(document.Rules, result, "rules");
        ValidateRuleOverrides(document.RuleOverrides, result);

        return result;
    }

    private static void ValidateDefaults(WelcomeMessageDefaults? defaults, SettingsValidationResult result)
    {
        if (defaults is null)
        {
            return;
        }

        if (defaults.ConnectionDelaySeconds is int delay)
        {
            ValidateRange(delay,
                WelcomeMessageSettingsConstants.MinConnectionDelaySeconds,
                WelcomeMessageSettingsConstants.MaxConnectionDelaySeconds,
                "defaults.connectionDelaySeconds",
                result);
        }

        if (defaults.StaleThresholdSeconds is int staleThreshold)
        {
            ValidateRange(staleThreshold,
                WelcomeMessageSettingsConstants.MinStaleThresholdSeconds,
                WelcomeMessageSettingsConstants.MaxStaleThresholdSeconds,
                "defaults.staleThresholdSeconds",
                result);
        }
    }

    private static void ValidateRules(
        IReadOnlyList<WelcomeMessageRule>? rules,
        SettingsValidationResult result,
        string pathPrefix)
    {
        if (rules is null)
        {
            return;
        }

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            var path = $"{pathPrefix}[{i}]";

            if (rule is null)
            {
                result.Errors.Add($"{path} must be an object.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                result.Errors.Add($"{path}.id is required.");
            }
            else if (!seenIds.Add(rule.Id.Trim()))
            {
                result.Errors.Add($"{path}.id '{rule.Id}' must be unique.");
            }

            if (rule.Priority is int priority)
            {
                ValidateRange(priority,
                    WelcomeMessageSettingsConstants.MinPriority,
                    WelcomeMessageSettingsConstants.MaxPriority,
                    $"{path}.priority",
                    result);
            }

            if (string.IsNullOrWhiteSpace(rule.MessageTemplate))
            {
                result.Errors.Add($"{path}.messageTemplate is required.");
            }
            else if (rule.MessageTemplate.Trim().Length > WelcomeMessageSettingsConstants.MaxMessageTemplateLength)
            {
                result.Errors.Add($"{path}.messageTemplate must be <= {WelcomeMessageSettingsConstants.MaxMessageTemplateLength} characters.");
            }

            if (rule.RequiredTags is null)
            {
                result.Errors.Add($"{path}.requiredTags is required.");
                continue;
            }

            if (rule.RequiredTags.Length > WelcomeMessageSettingsConstants.MaxRequiredTags)
            {
                result.Errors.Add($"{path}.requiredTags supports at most {WelcomeMessageSettingsConstants.MaxRequiredTags} tags.");
            }

            for (var tagIndex = 0; tagIndex < rule.RequiredTags.Length; tagIndex++)
            {
                if (string.IsNullOrWhiteSpace(rule.RequiredTags[tagIndex]))
                {
                    result.Errors.Add($"{path}.requiredTags[{tagIndex}] cannot be empty.");
                }
            }

            if (rule.ConnectionDelaySeconds is int delay)
            {
                ValidateRange(delay,
                    WelcomeMessageSettingsConstants.MinConnectionDelaySeconds,
                    WelcomeMessageSettingsConstants.MaxConnectionDelaySeconds,
                    $"{path}.connectionDelaySeconds",
                    result);
            }
        }
    }

    private static void ValidateRuleOverrides(
        IReadOnlyList<WelcomeMessageRuleOverride>? overrides,
        SettingsValidationResult result)
    {
        if (overrides is null)
        {
            return;
        }

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < overrides.Count; i++)
        {
            var overrideRule = overrides[i];
            var path = $"ruleOverrides[{i}]";

            if (overrideRule is null)
            {
                result.Errors.Add($"{path} must be an object.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(overrideRule.Id))
            {
                result.Errors.Add($"{path}.id is required.");
                continue;
            }

            if (!seenIds.Add(overrideRule.Id.Trim()))
            {
                result.Errors.Add($"{path}.id '{overrideRule.Id}' must be unique within ruleOverrides.");
            }

            if (overrideRule.Priority is int priority)
            {
                ValidateRange(priority,
                    WelcomeMessageSettingsConstants.MinPriority,
                    WelcomeMessageSettingsConstants.MaxPriority,
                    $"{path}.priority",
                    result);
            }

            if (overrideRule.MessageTemplate is not null &&
                overrideRule.MessageTemplate.Trim().Length > WelcomeMessageSettingsConstants.MaxMessageTemplateLength)
            {
                result.Errors.Add($"{path}.messageTemplate must be <= {WelcomeMessageSettingsConstants.MaxMessageTemplateLength} characters.");
            }

            if (overrideRule.RequiredTags is not null)
            {
                if (overrideRule.RequiredTags.Length > WelcomeMessageSettingsConstants.MaxRequiredTags)
                {
                    result.Errors.Add($"{path}.requiredTags supports at most {WelcomeMessageSettingsConstants.MaxRequiredTags} tags.");
                }

                for (var tagIndex = 0; tagIndex < overrideRule.RequiredTags.Length; tagIndex++)
                {
                    if (string.IsNullOrWhiteSpace(overrideRule.RequiredTags[tagIndex]))
                    {
                        result.Errors.Add($"{path}.requiredTags[{tagIndex}] cannot be empty.");
                    }
                }
            }

            if (overrideRule.ConnectionDelaySeconds is int delay)
            {
                ValidateRange(delay,
                    WelcomeMessageSettingsConstants.MinConnectionDelaySeconds,
                    WelcomeMessageSettingsConstants.MaxConnectionDelaySeconds,
                    $"{path}.connectionDelaySeconds",
                    result);
            }
        }
    }

    private static void ValidateRange(
        int value,
        int min,
        int max,
        string path,
        SettingsValidationResult result)
    {
        if (value < min || value > max)
        {
            result.Errors.Add($"{path} must be between {min} and {max}.");
        }
    }
}
