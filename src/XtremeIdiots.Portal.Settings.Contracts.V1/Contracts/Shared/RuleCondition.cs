using System.Text.Json;
using System.Text.Json.Serialization;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

public static class RuleConditionTreeConstants
{
    public const int MaxDepth = 8;
    public const int MaxConditionsPerRule = 100;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleConditionGroupOperator
{
    Unknown = 0,
    And,
    Or
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleComparisonOperator
{
    Unknown = 0,
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains
}

public class RuleCondition<TSignal, TCondition>
    where TSignal : struct, Enum
    where TCondition : RuleCondition<TSignal, TCondition>
{
    public RuleConditionGroupOperator? GroupOperator { get; set; }
    public List<TCondition>? Conditions { get; set; }
    public TSignal? Signal { get; set; }
    public RuleComparisonOperator? Operator { get; set; }
    public string? ExpectedValue { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public static class RuleConditionTreeValidator
{
    public static void Validate<TSignal, TCondition>(
        TCondition? condition,
        string path,
        SettingsValidationResult result,
        Action<TCondition, string, SettingsValidationResult> validateLeaf,
        Func<TCondition, bool>? hasFeatureLeafFields = null)
        where TSignal : struct, Enum
        where TCondition : RuleCondition<TSignal, TCondition>
    {
        var count = 0;
        Validate<TSignal, TCondition>(condition, path, 1, ref count, result, validateLeaf, hasFeatureLeafFields);
    }

    private static void Validate<TSignal, TCondition>(
        TCondition? condition,
        string path,
        int depth,
        ref int count,
        SettingsValidationResult result,
        Action<TCondition, string, SettingsValidationResult> validateLeaf,
        Func<TCondition, bool>? hasFeatureLeafFields)
        where TSignal : struct, Enum
        where TCondition : RuleCondition<TSignal, TCondition>
    {
        if (condition is null || depth > RuleConditionTreeConstants.MaxDepth ||
            ++count > RuleConditionTreeConstants.MaxConditionsPerRule)
        {
            result.Errors.Add($"{path} is missing or exceeds the condition limits.");
            return;
        }

        if (condition.GroupOperator is { } groupOperator)
        {
            if (groupOperator is not (RuleConditionGroupOperator.And or RuleConditionGroupOperator.Or) ||
                condition.Conditions is null || condition.Conditions.Count == 0 ||
                condition.Signal is not null || condition.Operator is not null ||
                condition.ExpectedValue is not null || hasFeatureLeafFields?.Invoke(condition) == true)
            {
                result.Errors.Add($"{path} must be a nonempty AND/OR group without leaf fields.");
                return;
            }

            foreach (var child in condition.Conditions)
            {
                Validate<TSignal, TCondition>(
                    child, path + ".conditions", depth + 1, ref count, result, validateLeaf, hasFeatureLeafFields);
            }

            return;
        }

        if (condition.Conditions is not null)
        {
            result.Errors.Add($"{path} must be a group or a leaf condition, not both.");
            return;
        }

        validateLeaf(condition, path, result);
    }
}
