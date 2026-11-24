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

            RuleConditionTreeValidator.Validate<VpnProtectionSignal, SiegeModeCondition>(
                rule.Condition,
                path + ".condition",
                result,
                ValidateLeafCondition,
                static condition => condition.PlayerSignal is not null);
        }
        return result;
    }

    private static void ValidateLeafCondition(
        SiegeModeCondition condition,
        string path,
        SettingsValidationResult result)
    {
        if ((condition.Signal is null) == (condition.PlayerSignal is null) ||
            condition.Operator is null || !Enum.IsDefined(condition.Operator.Value) ||
            string.IsNullOrWhiteSpace(condition.ExpectedValue) ||
            condition.ExpectedValue.Trim().Length > VpnProtectionSettingsConstants.MaxExpectedValueLength)
        {
            result.Errors.Add($"{path} must contain exactly one signal and a valid operator and expected value.");
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
                VpnProtectionSettingsValidator.ValidateCondition(
                    signal, condition.Operator.Value, condition.ExpectedValue, path, result);
            }
            return;
        }

        if (condition.PlayerSignal is not (SiegePlayerSignal.NewGuid or SiegePlayerSignal.NewIp or SiegePlayerSignal.PlayerTag) ||
            condition.Operator is not (RuleComparisonOperator.Equal or RuleComparisonOperator.NotEqual))
        {
            result.Errors.Add($"{path} has an invalid player condition.");
            return;
        }
        if (condition.PlayerSignal != SiegePlayerSignal.PlayerTag && !bool.TryParse(condition.ExpectedValue, out _))
        {
            result.Errors.Add($"{path}.expectedValue must be true or false.");
        }
        if (condition.PlayerSignal == SiegePlayerSignal.PlayerTag &&
            condition.ExpectedValue.Trim().Length > VpnProtectionSettingsConstants.MaxPlayerTagLength)
        {
            result.Errors.Add($"{path}.expectedValue is too long for a player tag.");
        }
    }
}
