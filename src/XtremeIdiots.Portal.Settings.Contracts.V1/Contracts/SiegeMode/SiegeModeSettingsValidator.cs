using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.SiegeMode;

public sealed class SiegeModeSettingsValidator
{
    public SettingsValidationResult Validate(SiegeModeSettingsDocument? document)
    {
        var result = new SettingsValidationResult();
        if (document is null)
        {
            result.Errors.Add("settings are required.");
            return result;
        }

        if (!SchemaVersionSupport.IsSupported(document.SchemaVersion))
        {
            result.Errors.Add($"Unsupported schemaVersion '{document.SchemaVersion}' for namespace '{SiegeModeSettingsConstants.Namespace}'.");
        }
        if (document.DurationMinutes <= 0)
        {
            result.Errors.Add("durationMinutes must be positive.");
        }
        if (document.Rules is null)
        {
            result.Errors.Add("rules is required.");
            return result;
        }
        if (document.Rules.Count > VpnProtectionSettingsConstants.MaxRules)
        {
            result.Errors.Add($"rules supports at most {VpnProtectionSettingsConstants.MaxRules} rules.");
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < document.Rules.Count; i++)
        {
            var rule = document.Rules[i];
            var path = $"rules[{i}]";
            if (rule is null)
            {
                result.Errors.Add($"{path} must be an object.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(rule.Id) || rule.Id.Trim().Length > VpnProtectionSettingsConstants.MaxRuleIdLength || !ids.Add(rule.Id.Trim()))
            {
                result.Errors.Add($"{path}.id must be nonempty, unique, and at most {VpnProtectionSettingsConstants.MaxRuleIdLength} characters.");
            }
            var count = 0;
            ValidateCondition(rule.Condition, path + ".condition", 1, ref count, result);
        }
        return result;
    }

    private static void ValidateCondition(SiegeModeCondition? condition, string path, int depth, ref int count, SettingsValidationResult result)
    {
        if (condition is null || depth > SiegeModeSettingsConstants.MaxConditionDepth || ++count > SiegeModeSettingsConstants.MaxConditionsPerRule)
        {
            result.Errors.Add($"{path} is missing or exceeds the condition limits.");
            return;
        }
        if (condition.GroupOperator is not null)
        {
            if (condition.GroupOperator is not (SiegeConditionGroupOperator.And or SiegeConditionGroupOperator.Or) ||
                condition.Conditions is null || condition.Conditions.Count == 0 ||
                condition.Signal is not null || condition.PlayerSignal is not null ||
                condition.Operator is not null || condition.ExpectedValue is not null)
            {
                result.Errors.Add($"{path} must be a nonempty AND/OR group without leaf fields.");
                return;
            }
            foreach (var child in condition.Conditions)
            {
                ValidateCondition(child, path + ".conditions", depth + 1, ref count, result);
            }
            return;
        }
        if (condition.Conditions is not null || (condition.Signal is null) == (condition.PlayerSignal is null) ||
            condition.Operator is null || !Enum.IsDefined(condition.Operator.Value))
        {
            result.Errors.Add($"{path} must contain exactly one signal and an operator.");
            return;
        }
        if (condition.Signal is { } signal)
        {
            if (!Enum.IsDefined(signal))
            {
                result.Errors.Add($"{path}.signal is invalid.");
            }
            else
            {
                VpnProtectionSettingsValidator.ValidateCondition(signal, condition.Operator.Value, condition.ExpectedValue, path, result);
            }
            return;
        }
        if (condition.PlayerSignal is not (SiegePlayerSignal.NewGuid or SiegePlayerSignal.NewIp or SiegePlayerSignal.PlayerTag) ||
            condition.Operator is not (VpnProtectionComparisonOperator.Equal or VpnProtectionComparisonOperator.NotEqual) ||
            string.IsNullOrWhiteSpace(condition.ExpectedValue) ||
            condition.ExpectedValue.Trim().Length > VpnProtectionSettingsConstants.MaxExpectedValueLength)
        {
            result.Errors.Add($"{path} has an invalid player condition.");
            return;
        }
        if (condition.PlayerSignal != SiegePlayerSignal.PlayerTag && !bool.TryParse(condition.ExpectedValue, out _))
        {
            result.Errors.Add($"{path}.expectedValue must be true or false.");
        }
        if (condition.PlayerSignal == SiegePlayerSignal.PlayerTag && condition.ExpectedValue.Trim().Length > VpnProtectionSettingsConstants.MaxPlayerTagLength)
        {
            result.Errors.Add($"{path}.expectedValue is too long for a player tag.");
        }
    }
}
