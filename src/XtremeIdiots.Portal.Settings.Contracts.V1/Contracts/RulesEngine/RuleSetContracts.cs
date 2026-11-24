using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

public static class RuleSetConstants
{
    public const int SchemaVersion = 1;
    public const int MaxRules = 100;
    public const int MaxIdLength = 128;
    public const int MaxActionsPerRule = 32;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleAfterMatch
{
    Unknown = 0,
    StopProcessingRules,
    ContinueToNextRule
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureEnablementOverride
{
    Unknown = 0,
    Inherit,
    Enabled,
    Disabled
}

public class FeatureRuleSetDocument
{
    public int SchemaVersion { get; set; } = RuleSetConstants.SchemaVersion;
    public string FeatureId { get; set; } = string.Empty;
    public bool? Enabled { get; set; }
    public List<RuleDefinition> Rules { get; set; } = [];
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public class FeatureRuleSetOverrideDocument
{
    public int SchemaVersion { get; set; } = RuleSetConstants.SchemaVersion;
    public string FeatureId { get; set; } = string.Empty;
    public FeatureEnablementOverride EnabledOverride { get; set; }
    public bool InheritGlobalRules { get; set; } = true;
    public List<RuleDefinition> Rules { get; set; } = [];
    public List<RuleDefinitionOverride> RuleOverrides { get; set; } = [];
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class RuleDefinition
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
    public RuleConditionNode? Condition { get; set; }
    public List<RuleActionDefinition> Actions { get; set; } = [];
    public RuleAfterMatch AfterMatch { get; set; } = RuleAfterMatch.StopProcessingRules;
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class RuleDefinitionOverride
{
    public string RuleId { get; set; } = string.Empty;
    public bool? Enabled { get; set; }
    public RuleConditionNode? Condition { get; set; }
    public List<RuleActionDefinition>? Actions { get; set; }
    public RuleAfterMatch? AfterMatch { get; set; }
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class RuleConditionNode
{
    public RuleConditionGroupOperator? GroupOperator { get; set; }
    public List<RuleConditionNode>? Conditions { get; set; }
    public string? ConditionId { get; set; }
    public Dictionary<string, JsonElement>? Parameters { get; set; }
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class RuleActionDefinition
{
    public string Id { get; set; } = string.Empty;
    public string TypeId { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Parameters { get; set; } = [];
    public List<string> DependsOnActionIds { get; set; } = [];
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class RuleSetValidationOptions
{
    public required string FeatureId { get; init; }
    public required IReadOnlySet<string> AllowedConditionIds { get; init; }
    public required IReadOnlySet<string> AllowedActionTypeIds { get; init; }
    public IReadOnlyDictionary<string, ConditionDefinition> ConditionDefinitions { get; init; } =
        new Dictionary<string, ConditionDefinition>(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, ActionTypeDefinition> ActionDefinitions { get; init; } =
        new Dictionary<string, ActionTypeDefinition>(StringComparer.Ordinal);
    public bool AllowUnknownConditionIds { get; init; } = true;

    public static RuleSetValidationOptions ForFeature(string featureId)
    {
        var feature = RulesEngineCatalogueV1.Current.Features.SingleOrDefault(item =>
            string.Equals(item.FeatureId, featureId, StringComparison.Ordinal))
            ?? throw new ArgumentOutOfRangeException(nameof(featureId), featureId, "The feature is not registered.");
        var conditions = RulesEngineCatalogueV1.Current.Conditions.ToDictionary(
            static item => item.ConditionId, StringComparer.Ordinal);
        var actions = RulesEngineCatalogueV1.Current.Actions.ToDictionary(
            static item => item.ActionTypeId, StringComparer.Ordinal);
        return new RuleSetValidationOptions
        {
            FeatureId = feature.FeatureId,
            AllowedConditionIds = new HashSet<string>(feature.ConditionIds, StringComparer.Ordinal),
            AllowedActionTypeIds = new HashSet<string>(feature.ActionTypeIds, StringComparer.Ordinal),
            ConditionDefinitions = conditions,
            ActionDefinitions = actions
        };
    }
}

public sealed class RuleSetValidator
{
    public SettingsValidationResult Validate(FeatureRuleSetDocument? document, RuleSetValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var result = new SettingsValidationResult();
        if (document is null)
        {
            result.Errors.Add("settings are required.");
            return result;
        }

        ValidateCommon(document.SchemaVersion, document.FeatureId, options.FeatureId, result);
        if (document.Enabled is null)
        {
            result.Errors.Add("enabled is required.");
        }
        ValidateRules(document.Rules, "rules", options, result);
        return result;
    }

    public SettingsValidationResult ValidateOverride(
        FeatureRuleSetOverrideDocument? document,
        RuleSetValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var result = new SettingsValidationResult();
        if (document is null)
        {
            result.Errors.Add("settings are required.");
            return result;
        }

        ValidateCommon(document.SchemaVersion, document.FeatureId, options.FeatureId, result);
        if (!Enum.IsDefined(document.EnabledOverride) || document.EnabledOverride == FeatureEnablementOverride.Unknown)
        {
            result.Errors.Add("enabledOverride must be Inherit, Enabled, or Disabled.");
        }
        ValidateRules(document.Rules, "rules", options, result);

        if (document.RuleOverrides is null)
        {
            result.Errors.Add("ruleOverrides is required.");
            return result;
        }
        if (document.RuleOverrides.Count > RuleSetConstants.MaxRules)
        {
            result.Errors.Add($"ruleOverrides supports at most {RuleSetConstants.MaxRules} entries.");
        }
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in document.RuleOverrides)
        {
            if (item is null || !ValidId(item.RuleId) || !ids.Add(item.RuleId))
            {
                result.Errors.Add("ruleOverrides contains an empty or duplicate ruleId.");
                continue;
            }
            if (item.AfterMatch is { } afterMatch &&
                (!Enum.IsDefined(afterMatch) || afterMatch == RuleAfterMatch.Unknown))
            {
                result.Errors.Add($"ruleOverrides['{item.RuleId}'].afterMatch is invalid.");
            }
            if (item.Actions is not null)
            {
                ValidateActions(item.Actions, $"ruleOverrides['{item.RuleId}'].actions", options, result);
            }
            if (item.Condition is not null)
            {
                ValidateCondition(item.Condition, $"ruleOverrides['{item.RuleId}'].condition", 1, new Count(), options, result);
            }
        }
        return result;
    }

    private static void ValidateCommon(int schemaVersion, string featureId, string expectedFeatureId, SettingsValidationResult result)
    {
        if (schemaVersion != RuleSetConstants.SchemaVersion)
        {
            result.Errors.Add($"Unsupported schemaVersion '{schemaVersion}'.");
        }
        if (!string.Equals(featureId, expectedFeatureId, StringComparison.Ordinal))
        {
            result.Errors.Add($"featureId must be '{expectedFeatureId}'.");
        }
    }

    private static void ValidateRules(
        IReadOnlyList<RuleDefinition>? rules,
        string path,
        RuleSetValidationOptions options,
        SettingsValidationResult result)
    {
        if (rules is null)
        {
            result.Errors.Add($"{path} is required.");
            return;
        }
        if (rules.Count > RuleSetConstants.MaxRules)
        {
            result.Errors.Add($"{path} supports at most {RuleSetConstants.MaxRules} rules.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < rules.Count; index++)
        {
            var rule = rules[index];
            var rulePath = $"{path}[{index}]";
            if (rule is null)
            {
                result.Errors.Add($"{rulePath} must be an object.");
                continue;
            }
            if (!ValidId(rule.Id) || !ids.Add(rule.Id))
            {
                result.Errors.Add($"{rulePath}.id must be nonempty, unique, and at most {RuleSetConstants.MaxIdLength} characters.");
            }
            if (!Enum.IsDefined(rule.AfterMatch) || rule.AfterMatch == RuleAfterMatch.Unknown)
            {
                result.Errors.Add($"{rulePath}.afterMatch must be specified.");
            }
            ValidateCondition(rule.Condition, rulePath + ".condition", 1, new Count(), options, result);
            ValidateActions(rule.Actions, rulePath + ".actions", options, result);
        }
    }

    private static void ValidateCondition(
        RuleConditionNode? condition,
        string path,
        int depth,
        Count count,
        RuleSetValidationOptions options,
        SettingsValidationResult result)
    {
        if (condition is null || depth > RuleConditionTreeConstants.MaxDepth ||
            ++count.Value > RuleConditionTreeConstants.MaxConditionsPerRule)
        {
            result.Errors.Add($"{path} is missing or exceeds the condition limits.");
            return;
        }

        if (condition.GroupOperator is { } groupOperator)
        {
            if (groupOperator is not (RuleConditionGroupOperator.And or RuleConditionGroupOperator.Or) ||
                condition.Conditions is null || condition.Conditions.Count == 0 ||
                condition.ConditionId is not null || condition.Parameters is not null)
            {
                result.Errors.Add($"{path} must be a nonempty AND/OR group without leaf fields.");
                return;
            }
            for (var index = 0; index < condition.Conditions.Count; index++)
            {
                ValidateCondition(condition.Conditions[index], $"{path}.conditions[{index}]", depth + 1, count, options, result);
            }
            return;
        }

        if (condition.Conditions is not null || !ValidId(condition.ConditionId))
        {
            result.Errors.Add($"{path} must be a condition leaf with an identifier.");
            return;
        }
        if (!options.AllowedConditionIds.Contains(condition.ConditionId!) &&
            (options.ConditionDefinitions.ContainsKey(condition.ConditionId!) || !options.AllowUnknownConditionIds))
        {
            result.Errors.Add($"{path}.conditionId '{condition.ConditionId}' is not supported by this feature.");
        }
        if (condition.Parameters is null)
        {
            result.Errors.Add($"{path}.parameters is required.");
        }
        else if (options.ConditionDefinitions.TryGetValue(condition.ConditionId!, out var definition))
        {
            ValidateParameters(condition.Parameters, definition.Parameters, path + ".parameters", result);
        }
    }

    private static void ValidateActions(
        IReadOnlyList<RuleActionDefinition>? actions,
        string path,
        RuleSetValidationOptions options,
        SettingsValidationResult result)
    {
        if (actions is null || actions.Count == 0 || actions.Count > RuleSetConstants.MaxActionsPerRule)
        {
            result.Errors.Add($"{path} must contain between 1 and {RuleSetConstants.MaxActionsPerRule} actions.");
            return;
        }

        var actionPositions = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < actions.Count; index++)
        {
            var action = actions[index];
            var actionPath = $"{path}[{index}]";
            if (action is null)
            {
                result.Errors.Add($"{actionPath} must be an object.");
                continue;
            }
            if (!ValidId(action.Id) || !actionPositions.TryAdd(action.Id, index))
            {
                result.Errors.Add($"{actionPath}.id must be nonempty, unique, and at most {RuleSetConstants.MaxIdLength} characters.");
            }
            if (!ValidId(action.TypeId) || !options.AllowedActionTypeIds.Contains(action.TypeId))
            {
                result.Errors.Add($"{actionPath}.typeId is not supported by this feature.");
            }
            if (action.Parameters is null)
            {
                result.Errors.Add($"{actionPath}.parameters is required.");
            }
            else if (options.ActionDefinitions.TryGetValue(action.TypeId, out var definition))
            {
                ValidateParameters(action.Parameters, definition.Parameters, actionPath + ".parameters", result);
            }
            if (action.DependsOnActionIds is null || action.DependsOnActionIds.Any(id => !ValidId(id)))
            {
                result.Errors.Add($"{actionPath}.dependsOnActionIds contains an invalid identifier.");
                continue;
            }
            foreach (var dependency in action.DependsOnActionIds)
            {
                if (!actionPositions.TryGetValue(dependency, out var dependencyIndex) || dependencyIndex >= index)
                {
                    result.Errors.Add($"{actionPath} must depend only on an earlier action in the same rule.");
                }
            }
        }
    }

    private static void ValidateParameters(
        IReadOnlyDictionary<string, JsonElement> parameters,
        IReadOnlyList<RuleParameterDefinition> schema,
        string path,
        SettingsValidationResult result)
    {
        foreach (var parameter in schema)
        {
            if (!parameters.TryGetValue(parameter.Name, out var value))
            {
                if (parameter.Required)
                {
                    result.Errors.Add($"{path}.{parameter.Name} is required.");
                }
                continue;
            }
            if (!ValidParameter(value, parameter))
            {
                result.Errors.Add($"{path}.{parameter.Name} must be a valid {parameter.Type} value.");
            }
        }
    }

    private static bool ValidParameter(JsonElement value, RuleParameterDefinition schema) =>
        schema.Type switch
        {
            "string" => value.ValueKind == JsonValueKind.String &&
                (schema.MaxLength is null || value.GetString()!.Length <= schema.MaxLength) &&
                (schema.MaxUtf8Bytes is null || Encoding.UTF8.GetByteCount(value.GetString()!) <= schema.MaxUtf8Bytes) &&
                (!schema.RejectControlCharacters || !value.GetString()!.Any(static character => character < ' ' || character == '\u007f')) &&
                (schema.AllowedValues.Count == 0 || schema.AllowedValues.Contains(value.GetString()!, StringComparer.Ordinal)),
            "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var integer) &&
                (schema.Minimum is null || integer >= schema.Minimum) &&
                (schema.Maximum is null || integer <= schema.Maximum),
            "number" => value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) &&
                (schema.Minimum is null || number >= schema.Minimum) &&
                (schema.Maximum is null || number <= schema.Maximum),
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "string[]" => value.ValueKind == JsonValueKind.Array &&
                value.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String &&
                    (schema.MaxLength is null || item.GetString()!.Length <= schema.MaxLength)),
            "date-time" => value.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _),
            "duration" => value.ValueKind == JsonValueKind.String && TimeSpan.TryParse(value.GetString(), CultureInfo.InvariantCulture, out _),
            "date-time-or-duration" => value.ValueKind == JsonValueKind.String &&
                (DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _) ||
                 TimeSpan.TryParse(value.GetString(), CultureInfo.InvariantCulture, out _)),
            _ => false
        };

    private static bool ValidId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= RuleSetConstants.MaxIdLength && value.Trim() == value;

    private sealed class Count
    {
        public int Value { get; set; }
    }
}

public sealed record EffectiveFeatureRuleSet(string FeatureId, bool Enabled, IReadOnlyList<RuleDefinition> Rules);

public static class FeatureRuleSetMerger
{
    public static EffectiveFeatureRuleSet Merge(
        FeatureRuleSetDocument global,
        FeatureRuleSetOverrideDocument? serverOverride)
    {
        ArgumentNullException.ThrowIfNull(global);
        if (serverOverride is not null &&
            !string.Equals(global.FeatureId, serverOverride.FeatureId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Global and server feature IDs must match.", nameof(serverOverride));
        }

        var enabled = serverOverride?.EnabledOverride switch
        {
            FeatureEnablementOverride.Enabled => true,
            FeatureEnablementOverride.Disabled => false,
            _ => global.Enabled == true
        };
        var orderedRules = serverOverride is null || serverOverride.InheritGlobalRules
            ? MergeInheritedRules(global.Rules, serverOverride)
            : serverOverride.Rules.ToArray();
        var rules = orderedRules.Select((rule, index) => WithOrder(rule, index)).ToArray();
        return new EffectiveFeatureRuleSet(global.FeatureId, enabled, rules);
    }

    private static IReadOnlyList<RuleDefinition> MergeInheritedRules(
        IReadOnlyList<RuleDefinition> globalRules,
        FeatureRuleSetOverrideDocument? serverOverride)
    {
        if (serverOverride is null)
        {
            return globalRules;
        }
        var overrides = serverOverride.RuleOverrides.ToDictionary(static item => item.RuleId, StringComparer.OrdinalIgnoreCase);
        return globalRules.Select(rule => overrides.TryGetValue(rule.Id, out var item)
                ? ApplyOverride(rule, item)
                : rule)
            .Concat(serverOverride.Rules)
            .ToArray();
    }

    private static RuleDefinition ApplyOverride(RuleDefinition rule, RuleDefinitionOverride item) => new()
    {
        Id = rule.Id,
        Enabled = item.Enabled ?? rule.Enabled,
        Order = rule.Order,
        Condition = item.Condition ?? rule.Condition,
        Actions = item.Actions ?? rule.Actions,
        AfterMatch = item.AfterMatch ?? rule.AfterMatch,
        ExtensionData = rule.ExtensionData
    };

    private static RuleDefinition WithOrder(RuleDefinition rule, int order) => new()
    {
        Id = rule.Id,
        Enabled = rule.Enabled,
        Order = order,
        Condition = rule.Condition,
        Actions = rule.Actions,
        AfterMatch = rule.AfterMatch,
        ExtensionData = rule.ExtensionData
    };
}
