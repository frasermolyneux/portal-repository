using System.Globalization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

public sealed class VpnProtectionSettingsValidator
{
    private static readonly IReadOnlySet<VpnProtectionSignal> BooleanSignals = new HashSet<VpnProtectionSignal>
    {
        VpnProtectionSignal.ProxyCheckIsProxy,
        VpnProtectionSignal.ProxyCheckIsVpn,
        VpnProtectionSignal.MaxMindIsAnonymous,
        VpnProtectionSignal.MaxMindIsAnonymousVpn,
        VpnProtectionSignal.MaxMindIsHostingProvider,
        VpnProtectionSignal.MaxMindIsPublicProxy,
        VpnProtectionSignal.MaxMindIsResidentialProxy,
        VpnProtectionSignal.MaxMindIsTorExitNode,
        VpnProtectionSignal.IsPartial
    };

    private static readonly IReadOnlySet<VpnProtectionSignal> NumericSignals = new HashSet<VpnProtectionSignal>
    {
        VpnProtectionSignal.ProxyCheckRiskScore,
        VpnProtectionSignal.MaxMindAnonymizerConfidence
    };

    private static readonly IReadOnlySet<VpnProtectionSignal> SourceStatusSignals = new HashSet<VpnProtectionSignal>
    {
        VpnProtectionSignal.MaxMindStatus,
        VpnProtectionSignal.ProxyCheckStatus
    };

    private static readonly IReadOnlySet<RuleComparisonOperator> EqualityOperators = new HashSet<RuleComparisonOperator>
    {
        RuleComparisonOperator.Equal,
        RuleComparisonOperator.NotEqual
    };

    private static readonly IReadOnlySet<RuleComparisonOperator> NumericOperators = new HashSet<RuleComparisonOperator>
    {
        RuleComparisonOperator.Equal,
        RuleComparisonOperator.NotEqual,
        RuleComparisonOperator.GreaterThan,
        RuleComparisonOperator.GreaterThanOrEqual,
        RuleComparisonOperator.LessThan,
        RuleComparisonOperator.LessThanOrEqual
    };

    private static readonly IReadOnlySet<RuleComparisonOperator> StringOperators = new HashSet<RuleComparisonOperator>
    {
        RuleComparisonOperator.Equal,
        RuleComparisonOperator.NotEqual,
        RuleComparisonOperator.Contains
    };

    private static readonly IReadOnlySet<string> SourceStatusValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Success",
        "Failed",
        "Unavailable"
    };

    public SettingsValidationResult Validate(VpnProtectionSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            result.Errors.Add("settings are required.");
            return result;
        }

        if (document.SchemaVersion != VpnProtectionSettingsConstants.SchemaVersion)
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{VpnProtectionSettingsConstants.Namespace}'.");
            return result;
        }

        ValidateRules(document.Rules, result);
        ValidateRuleOverrides(document.RuleOverrides, result);
        ValidateExcludedPlayerTags(document.ExcludedPlayerTags, result);
        return result;
    }

    private static void ValidateRules(IReadOnlyList<VpnProtectionRule>? rules, SettingsValidationResult result)
    {
        if (rules is null)
        {
            result.Errors.Add("rules is required.");
            return;
        }

        if (rules.Count > VpnProtectionSettingsConstants.MaxRules)
        {
            result.Errors.Add($"rules supports at most {VpnProtectionSettingsConstants.MaxRules} rules.");
        }

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < rules.Count; index++)
        {
            var rule = rules[index];
            var path = $"rules[{index}]";
            if (rule is null)
            {
                result.Errors.Add($"{path} must be an object.");
                continue;
            }

            ValidateRuleId(rule.Id, path, seenIds, result);
            ValidateLegacyConditionFields(rule.ExtensionData?.Keys, path, result);
            ValidateCondition(rule.Condition, path + ".condition", result);
            if (!Enum.IsDefined(rule.Action) || rule.Action == VpnProtectionAction.Unknown)
            {
                result.Errors.Add($"{path}.action must be specified.");
            }
            ValidateReasonTemplate(rule.ReasonTemplate, $"{path}.reasonTemplate", result);
        }
    }

    private static void ValidateRuleOverrides(
        IReadOnlyList<VpnProtectionRuleOverride>? ruleOverrides,
        SettingsValidationResult result)
    {
        if (ruleOverrides is null)
        {
            result.Errors.Add("ruleOverrides is required.");
            return;
        }

        if (ruleOverrides.Count > VpnProtectionSettingsConstants.MaxRules)
        {
            result.Errors.Add($"ruleOverrides supports at most {VpnProtectionSettingsConstants.MaxRules} overrides.");
        }

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < ruleOverrides.Count; index++)
        {
            var ruleOverride = ruleOverrides[index];
            var path = $"ruleOverrides[{index}]";
            if (ruleOverride is null)
            {
                result.Errors.Add($"{path} must be an object.");
                continue;
            }

            ValidateRuleId(ruleOverride.Id, path, seenIds, result);
            ValidateLegacyConditionFields(ruleOverride.ExtensionData?.Keys, path, result);
            if (ruleOverride.Condition is not null)
            {
                ValidateCondition(ruleOverride.Condition, path + ".condition", result);
            }
            if (ruleOverride.Action is { } action && (!Enum.IsDefined(action) || action == VpnProtectionAction.Unknown))
            {
                result.Errors.Add($"{path}.action must not be Unknown when provided.");
            }
            ValidateReasonTemplate(ruleOverride.ReasonTemplate, path + ".reasonTemplate", result);
        }
    }

    private static void ValidateLegacyConditionFields(
        IEnumerable<string>? extensionKeys,
        string path,
        SettingsValidationResult result)
    {
        if (extensionKeys?.Any(static key =>
            key.Equals("signal", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("operator", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("expectedValue", StringComparison.OrdinalIgnoreCase)) == true)
        {
            result.Errors.Add($"{path} contains legacy flat condition fields; use condition instead.");
        }
    }

    private static void ValidateRuleId(
        string? ruleId,
        string path,
        HashSet<string> seenIds,
        SettingsValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            result.Errors.Add($"{path}.id is required.");
            return;
        }

        var normalizedRuleId = ruleId.Trim();
        if (normalizedRuleId.Length > VpnProtectionSettingsConstants.MaxRuleIdLength)
        {
            result.Errors.Add($"{path}.id must be <= {VpnProtectionSettingsConstants.MaxRuleIdLength} characters.");
        }
        if (!seenIds.Add(normalizedRuleId))
        {
            result.Errors.Add($"{path}.id '{ruleId}' must be unique.");
        }
    }

    private static void ValidateCondition(
        VpnProtectionCondition? condition,
        string path,
        SettingsValidationResult result)
    {
        RuleConditionTreeValidator.Validate<VpnProtectionSignal, VpnProtectionCondition>(
            condition, path, result, ValidateConditionLeaf);
    }

    private static void ValidateConditionLeaf(
        VpnProtectionCondition condition,
        string path,
        SettingsValidationResult result)
    {
        if (condition.Signal is null || condition.Operator is null)
        {
            result.Errors.Add($"{path} must contain a signal and operator.");
            return;
        }

        ValidateCondition(condition.Signal.Value, condition.Operator.Value, condition.ExpectedValue, path, result);
    }

    internal static void ValidateCondition(
        VpnProtectionSignal signal,
        RuleComparisonOperator comparisonOperator,
        string? expectedValue,
        string path,
        SettingsValidationResult result)
    {
        if (signal == VpnProtectionSignal.Unknown || !Enum.IsDefined(signal))
        {
            result.Errors.Add($"{path}.signal must be specified.");
            return;
        }

        if (comparisonOperator == RuleComparisonOperator.Unknown || !Enum.IsDefined(comparisonOperator))
        {
            result.Errors.Add($"{path}.operator must be specified.");
            return;
        }

        if (string.IsNullOrWhiteSpace(expectedValue))
        {
            result.Errors.Add($"{path}.expectedValue is required.");
            return;
        }

        ValidateExpectedValueLength(expectedValue, $"{path}.expectedValue", result);

        if (BooleanSignals.Contains(signal))
        {
            if (!EqualityOperators.Contains(comparisonOperator))
            {
                result.Errors.Add($"{path}.operator '{comparisonOperator}' is not supported for boolean signal '{signal}'.");
            }
            if (!bool.TryParse(expectedValue, out _))
            {
                result.Errors.Add($"{path}.expectedValue must be 'true' or 'false' for signal '{signal}'.");
            }
            return;
        }

        if (NumericSignals.Contains(signal))
        {
            if (!NumericOperators.Contains(comparisonOperator))
            {
                result.Errors.Add($"{path}.operator '{comparisonOperator}' is not supported for numeric signal '{signal}'.");
            }
            if (!int.TryParse(expectedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericValue) ||
                numericValue < VpnProtectionSettingsConstants.MinNumericValue ||
                numericValue > VpnProtectionSettingsConstants.MaxNumericValue)
            {
                result.Errors.Add($"{path}.expectedValue must be an integer between {VpnProtectionSettingsConstants.MinNumericValue} and {VpnProtectionSettingsConstants.MaxNumericValue} for signal '{signal}'.");
            }
            return;
        }

        if (SourceStatusSignals.Contains(signal))
        {
            if (!EqualityOperators.Contains(comparisonOperator))
            {
                result.Errors.Add($"{path}.operator '{comparisonOperator}' is not supported for source status signal '{signal}'.");
            }
            if (!SourceStatusValues.Contains(expectedValue.Trim()))
            {
                result.Errors.Add($"{path}.expectedValue must be Success, Failed, or Unavailable for signal '{signal}'.");
            }
            return;
        }

        if (!StringOperators.Contains(comparisonOperator))
        {
            result.Errors.Add($"{path}.operator '{comparisonOperator}' is not supported for string signal '{signal}'.");
        }
    }

    private static void ValidateExpectedValueLength(
        string expectedValue,
        string path,
        SettingsValidationResult result)
    {
        if (expectedValue.Trim().Length > VpnProtectionSettingsConstants.MaxExpectedValueLength)
        {
            result.Errors.Add($"{path} must be <= {VpnProtectionSettingsConstants.MaxExpectedValueLength} characters.");
        }
    }

    private static void ValidateExcludedPlayerTags(
        IReadOnlyList<string>? excludedPlayerTags,
        SettingsValidationResult result)
    {
        if (excludedPlayerTags is null)
        {
            result.Errors.Add("excludedPlayerTags is required.");
            return;
        }

        if (excludedPlayerTags.Count > VpnProtectionSettingsConstants.MaxExcludedPlayerTags)
        {
            result.Errors.Add($"excludedPlayerTags supports at most {VpnProtectionSettingsConstants.MaxExcludedPlayerTags} tags.");
        }

        var seenTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < excludedPlayerTags.Count; index++)
        {
            var tag = excludedPlayerTags[index];
            var path = $"excludedPlayerTags[{index}]";
            if (string.IsNullOrWhiteSpace(tag))
            {
                result.Errors.Add($"{path} cannot be empty.");
                continue;
            }

            var normalizedTag = tag.Trim();
            if (normalizedTag.Length > VpnProtectionSettingsConstants.MaxPlayerTagLength)
            {
                result.Errors.Add($"{path} must be <= {VpnProtectionSettingsConstants.MaxPlayerTagLength} characters.");
            }
            if (!seenTags.Add(normalizedTag))
            {
                result.Errors.Add($"{path} '{tag}' must be unique.");
            }
        }
    }

    private static void ValidateReasonTemplate(
        string? reasonTemplate,
        string path,
        SettingsValidationResult result)
    {
        if (reasonTemplate is null)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(reasonTemplate))
        {
            result.Errors.Add($"{path} cannot be empty when provided.");
            return;
        }
        if (reasonTemplate.Length > VpnProtectionSettingsConstants.MaxReasonTemplateLength)
        {
            result.Errors.Add($"{path} must be <= {VpnProtectionSettingsConstants.MaxReasonTemplateLength} characters.");
        }
        if (reasonTemplate.IndexOfAny([';', '\r', '\n']) >= 0)
        {
            result.Errors.Add($"{path} cannot contain command separators or new lines.");
        }

        ValidateReasonTemplatePlaceholders(reasonTemplate, path, result);
    }

    private static void ValidateReasonTemplatePlaceholders(
        string reasonTemplate,
        string path,
        SettingsValidationResult result)
    {
        for (var index = 0; index < reasonTemplate.Length; index++)
        {
            if (reasonTemplate[index] == '}')
            {
                result.Errors.Add($"{path} contains an unmatched closing brace.");
                return;
            }
            if (reasonTemplate[index] != '{')
            {
                continue;
            }

            var closingBraceIndex = reasonTemplate.IndexOf('}', index + 1);
            if (closingBraceIndex < 0)
            {
                result.Errors.Add($"{path} contains an unmatched opening brace.");
                return;
            }

            var placeholder = reasonTemplate[index..(closingBraceIndex + 1)];
            if (!VpnProtectionSettingsConstants.AllowedReasonTemplatePlaceholders.Contains(placeholder))
            {
                result.Errors.Add($"{path} contains unsupported placeholder '{placeholder}'.");
            }
            index = closingBraceIndex;
        }
    }
}
