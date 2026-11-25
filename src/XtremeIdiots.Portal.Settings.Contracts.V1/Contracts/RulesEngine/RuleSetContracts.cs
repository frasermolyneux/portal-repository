using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

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
    public RuleTimingSettings? Timing { get; set; }
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
    public RuleTimingOverrides? Timing { get; set; }
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
    public RuleTimingOverrides? Timing { get; set; }
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
    public RuleTimingOverrides? Timing { get; set; }
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
    public List<string> TemplateParameters { get; set; } = [];
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
    public bool AllowUnknownActionTypeIds { get; init; } = true;

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
    private static readonly IReadOnlySet<VpnProtectionSignal> BooleanVpnSignals = new HashSet<VpnProtectionSignal>
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

    private static readonly IReadOnlySet<VpnProtectionSignal> NumericVpnSignals = new HashSet<VpnProtectionSignal>
    {
        VpnProtectionSignal.ProxyCheckRiskScore,
        VpnProtectionSignal.MaxMindAnonymizerConfidence
    };

    private static readonly IReadOnlySet<VpnProtectionSignal> VpnStatusSignals = new HashSet<VpnProtectionSignal>
    {
        VpnProtectionSignal.MaxMindStatus,
        VpnProtectionSignal.ProxyCheckStatus
    };

    private static readonly IReadOnlySet<string> EqualityOperators = new HashSet<string>(StringComparer.Ordinal)
    {
        "Equal", "NotEqual"
    };

    private static readonly IReadOnlySet<string> NumericOperators = new HashSet<string>(StringComparer.Ordinal)
    {
        "Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual"
    };

    private static readonly IReadOnlySet<string> StringOperators = new HashSet<string>(StringComparer.Ordinal)
    {
        "Equal", "NotEqual", "Contains"
    };

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
        ValidateTiming(document.Timing ?? new(), "timing", result);
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
        ValidateTimingOverrides(document.Timing, "timing", result);
        if (document.ExtensionData?.Keys.Any(key =>
                string.Equals(key, "enabled", StringComparison.OrdinalIgnoreCase)) == true)
        {
            result.Errors.Add("enabled is not valid on a server override; use enabledOverride.");
        }
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
            ValidateTimingOverrides(item.Timing, $"ruleOverrides['{item.RuleId}'].timing", result);
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
            ValidateTimingOverrides(rule.Timing, rulePath + ".timing", result);
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
            ValidateConditionSemantics(
                condition.ConditionId!,
                condition.Parameters,
                options.FeatureId,
                path + ".parameters",
                result);
        }
    }

    private static void ValidateConditionSemantics(
        string conditionId,
        IReadOnlyDictionary<string, JsonElement> parameters,
        string featureId,
        string path,
        SettingsValidationResult result)
    {
        if (conditionId == RulesEngineCatalogueV1.AlwaysCondition && parameters.Count != 0)
        {
            result.Errors.Add($"{path} must be empty for the Always condition.");
            return;
        }
        if (conditionId == "player.tags")
        {
            if (!parameters.TryGetValue("tags", out var tags) || tags.ValueKind != JsonValueKind.Array ||
                tags.GetArrayLength() == 0)
            {
                result.Errors.Add($"{path}.tags must contain at least one tag ID.");
            }
            else
            {
                var ids = new HashSet<Guid>();
                foreach (var tag in tags.EnumerateArray())
                {
                    if (tag.ValueKind != JsonValueKind.String ||
                        !Guid.TryParse(tag.GetString(), out var tagId) || tagId == Guid.Empty || !ids.Add(tagId))
                    {
                        result.Errors.Add($"{path}.tags must contain unique, nonempty GUID IDs.");
                        break;
                    }
                }
            }
            return;
        }

        if (conditionId is "player.first-seen-guid" or "player.first-seen-ip")
        {
            if (TryGetString(parameters, "referenceScope", out var referenceScope) &&
                referenceScope == "SiegeBaseline" &&
                featureId != RulesEngineCatalogueV1.SiegeModeFeature)
            {
                result.Errors.Add($"{path}.referenceScope 'SiegeBaseline' is only valid for Siege Mode rules.");
            }
            ValidateFirstSeenValue(parameters, path, result);
            return;
        }

        if (conditionId == "player.profile-age")
        {
            if (TryGetString(parameters, "duration", out var durationText) &&
                (!TimeSpan.TryParse(durationText, CultureInfo.InvariantCulture, out var duration) ||
                    duration <= TimeSpan.Zero))
            {
                result.Errors.Add($"{path}.duration must be a positive duration.");
            }
            return;
        }

        if (conditionId == "vpn.intelligence")
        {
            ValidateVpnCondition(parameters, path, result);
        }
    }

    private static void ValidateFirstSeenValue(
        IReadOnlyDictionary<string, JsonElement> parameters,
        string path,
        SettingsValidationResult result)
    {
        if (!TryGetString(parameters, "operator", out var operation))
        {
            return;
        }

        if (operation is "IsNew" or "IsKnown")
        {
            if (parameters.ContainsKey("value"))
            {
                result.Errors.Add($"{path}.value is not used by the {operation} operator.");
            }
            return;
        }

        if (!TryGetString(parameters, "value", out var value))
        {
            result.Errors.Add($"{path}.value is required for the {operation} operator.");
            return;
        }

        if (operation is "Before" or "After")
        {
            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _))
            {
                result.Errors.Add($"{path}.value must be a date-time for the {operation} operator.");
            }
        }
        else if (operation == "WithinAge" &&
            (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var duration) || duration <= TimeSpan.Zero))
        {
            result.Errors.Add($"{path}.value must be a positive duration for the WithinAge operator.");
        }
    }

    private static void ValidateVpnCondition(
        IReadOnlyDictionary<string, JsonElement> parameters,
        string path,
        SettingsValidationResult result)
    {
        if (!TryGetString(parameters, "signal", out var signalText) ||
            !Enum.TryParse<VpnProtectionSignal>(signalText, false, out var signal) ||
            signal == VpnProtectionSignal.Unknown || !Enum.IsDefined(signal) ||
            !TryGetString(parameters, "operator", out var operation) ||
            !TryGetString(parameters, "expectedValue", out var expectedValue))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(expectedValue))
        {
            result.Errors.Add($"{path}.expectedValue cannot be empty.");
            return;
        }

        if (BooleanVpnSignals.Contains(signal))
        {
            if (!EqualityOperators.Contains(operation))
            {
                result.Errors.Add($"{path}.operator '{operation}' is not supported for boolean signal '{signal}'.");
            }
            if (!bool.TryParse(expectedValue, out _))
            {
                result.Errors.Add($"{path}.expectedValue must be true or false for signal '{signal}'.");
            }
        }
        else if (NumericVpnSignals.Contains(signal))
        {
            if (!NumericOperators.Contains(operation))
            {
                result.Errors.Add($"{path}.operator '{operation}' is not supported for numeric signal '{signal}'.");
            }
            if (!int.TryParse(expectedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ||
                value is < 0 or > 100)
            {
                result.Errors.Add($"{path}.expectedValue must be an integer between 0 and 100 for signal '{signal}'.");
            }
        }
        else if (VpnStatusSignals.Contains(signal))
        {
            if (!EqualityOperators.Contains(operation))
            {
                result.Errors.Add($"{path}.operator '{operation}' is not supported for source status signal '{signal}'.");
            }
            if (!new[] { "Success", "Failed", "Unavailable" }.Contains(expectedValue.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                result.Errors.Add($"{path}.expectedValue must be Success, Failed, or Unavailable for signal '{signal}'.");
            }
        }
        else if (!StringOperators.Contains(operation))
        {
            result.Errors.Add($"{path}.operator '{operation}' is not supported for string signal '{signal}'.");
        }
    }

    private static bool TryGetString(
        IReadOnlyDictionary<string, JsonElement> parameters,
        string name,
        out string value)
    {
        if (parameters.TryGetValue(name, out var element) && element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString()!;
            return true;
        }
        value = string.Empty;
        return false;
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
            var knownActionType = options.ActionDefinitions.ContainsKey(action.TypeId);
            if (!ValidId(action.TypeId) ||
                (knownActionType && !options.AllowedActionTypeIds.Contains(action.TypeId)) ||
                (!knownActionType && !options.AllowUnknownActionTypeIds))
            {
                result.Errors.Add($"{actionPath}.typeId is not supported by this feature.");
            }
            if (action.Parameters is null)
            {
                result.Errors.Add($"{actionPath}.parameters is required.");
            }
            else if (options.ActionDefinitions.TryGetValue(action.TypeId, out var definition))
            {
                ValidateActionParameters(action, definition, actionPath, result);
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

    private static void ValidateTiming(RuleTimingSettings timing, string path, SettingsValidationResult result)
    {
        if (!timing.IsValid)
        {
            result.Errors.Add($"{path} requires a delay from 0 to 60 seconds and a freshness window greater than the delay and at most 900 seconds.");
        }
    }

    private static void ValidateTimingOverrides(RuleTimingOverrides? timing, string path, SettingsValidationResult result)
    {
        if (timing is null)
        {
            return;
        }
        if (timing.ConnectionDelaySeconds is < 0 or > 60 || timing.FreshnessSeconds is <= 0 or > 900 ||
            timing.ConnectionDelaySeconds is { } delay && timing.FreshnessSeconds is { } freshness && delay >= freshness)
        {
            result.Errors.Add($"{path} contains an invalid delay or freshness override.");
        }
    }

    private static void ValidateActionParameters(
        RuleActionDefinition action, ActionTypeDefinition definition, string path, SettingsValidationResult result)
    {
        if (action.TemplateParameters is null ||
            action.TemplateParameters.Distinct(StringComparer.Ordinal).Count() != action.TemplateParameters.Count ||
            action.TemplateParameters.Any(name => !definition.Parameters.Any(parameter =>
                parameter.Name == name && parameter.SupportsTemplates) || !action.Parameters.ContainsKey(name)))
        {
            result.Errors.Add($"{path}.templateParameters must name unique, present, template-capable text parameters.");
            return;
        }
        var schema = definition.Parameters.Select(parameter =>
            action.TemplateParameters.Contains(parameter.Name, StringComparer.Ordinal)
                ? parameter with { MaxLength = 16384, MaxUtf8Bytes = null }
                : parameter).ToArray();
        ValidateParameters(action.Parameters, schema, path + ".parameters", result);
        foreach (var name in action.TemplateParameters)
        {
            var value = action.Parameters[name];
            var items = value.ValueKind == JsonValueKind.Array
                ? value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)
                : value.ValueKind == JsonValueKind.String ? [value.GetString()!] : Array.Empty<string>();
            foreach (var text in items)
            {
                foreach (var error in RuleTextRenderer.Validate(text))
                {
                    result.Errors.Add($"{path}.parameters.{name}: {error}");
                }
            }
        }
        if (definition.EffectClass == "communication")
        {
            var hasSingle = action.Parameters.TryGetValue("message", out var message) &&
                message.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(message.GetString());
            var hasList = action.Parameters.TryGetValue("messages", out var messages) &&
                messages.ValueKind == JsonValueKind.Array && messages.GetArrayLength() > 0;
            if (!hasSingle && !hasList)
            {
                result.Errors.Add($"{path}.parameters requires message or a nonempty messages list.");
            }
            if (messages.ValueKind == JsonValueKind.Array && (messages.GetArrayLength() > 100 ||
                messages.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(item.GetString()))))
            {
                result.Errors.Add($"{path}.parameters.messages must contain 1 to 100 nonempty strings.");
            }
        }
        if (action.TypeId == RulesEngineCatalogueV1.PrivateTaggedMessageAction)
        {
            ValidateConditionSemantics("player.tags",
                new Dictionary<string, JsonElement> { ["tags"] = action.Parameters.GetValueOrDefault("recipientTags") },
                string.Empty, path + ".parameters.recipientTags", result);
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
                    (schema.MaxLength is null || item.GetString()!.Length <= schema.MaxLength) &&
                    (schema.MaxUtf8Bytes is null || Encoding.UTF8.GetByteCount(item.GetString()!) <= schema.MaxUtf8Bytes) &&
                    (!schema.RejectControlCharacters || !item.GetString()!.Any(static character => character < ' ' || character == '\u007f'))),
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

public sealed record EffectiveFeatureRuleSet(string FeatureId, bool Enabled, IReadOnlyList<RuleDefinition> Rules)
{
    public RuleTimingSettings Timing { get; init; } = new();
}

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
        var timing = (global.Timing ?? new()).Apply(serverOverride?.Timing);
        if (!timing.IsValid || rules.Any(rule => !timing.Apply(rule.Timing).IsValid))
        {
            throw new ArgumentException("The merged timing must leave a delivery window.", nameof(serverOverride));
        }
        return new EffectiveFeatureRuleSet(global.FeatureId, enabled, rules) { Timing = timing };
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
        Timing = item.Timing is null ? rule.Timing : new RuleTimingOverrides
        {
            ConnectionDelaySeconds = item.Timing.ConnectionDelaySeconds ?? rule.Timing?.ConnectionDelaySeconds,
            FreshnessSeconds = item.Timing.FreshnessSeconds ?? rule.Timing?.FreshnessSeconds
        },
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
        Timing = rule.Timing,
        ExtensionData = rule.ExtensionData
    };
}
