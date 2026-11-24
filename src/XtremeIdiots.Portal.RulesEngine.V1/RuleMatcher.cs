using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.RulesEngine.V1;

public enum RuleTruthValue
{
    Unknown = 0,
    False,
    True
}

public sealed record ConditionEvaluation(RuleTruthValue Value, string? Diagnostic = null)
{
    public static ConditionEvaluation True { get; } = new(RuleTruthValue.True);
    public static ConditionEvaluation False { get; } = new(RuleTruthValue.False);
    public static ConditionEvaluation Unknown(string? diagnostic = null) => new(RuleTruthValue.Unknown, diagnostic);
}

public enum PlannedActionStatus
{
    Planned = 0,
    SkippedUnsupported,
    SkippedDependency
}

public sealed record PlannedRuleAction(RuleActionDefinition Action, PlannedActionStatus Status, string? Detail = null);

public sealed record PlannedRule(
    RuleDefinition Rule,
    ConditionEvaluation Condition,
    IReadOnlyList<PlannedRuleAction> Actions);

public sealed record RuleMatchPlan(
    IReadOnlyList<PlannedRule> MatchedRules,
    IReadOnlyList<PlannedRule> IndeterminateRules);

public sealed record RuleEvaluationContext(
    Guid GameServerId,
    int GameType,
    Guid ConnectionId,
    string FeatureId,
    Guid? SiegeSessionId,
    DateTimeOffset OccurredAtUtc,
    IReadOnlySet<string> RuntimeCapabilities);

public interface IRuleConditionResolver
{
    ConditionEvaluation Resolve(RuleConditionNode condition, RuleEvaluationContext context);
}

public interface IRuleSetProvider
{
    ValueTask<EffectiveFeatureRuleSet> GetEffectiveRuleSetAsync(
        RuleEvaluationContext context,
        CancellationToken cancellationToken = default);
}

public static class RuleMatcher
{
    public static ConditionEvaluation EvaluateCondition(
        RuleConditionNode? condition,
        Func<RuleConditionNode, ConditionEvaluation> evaluateLeaf)
    {
        ArgumentNullException.ThrowIfNull(evaluateLeaf);
        if (condition is null)
        {
            return ConditionEvaluation.Unknown("Condition is missing.");
        }

        if (condition.GroupOperator is { } groupOperator)
        {
            if (condition.Conditions is null || condition.Conditions.Count == 0)
            {
                return ConditionEvaluation.Unknown("Condition group must contain at least one child.");
            }
            var children = condition.Conditions.Select(child => EvaluateCondition(child, evaluateLeaf)).ToArray();
            return groupOperator switch
            {
                RuleConditionGroupOperator.And => EvaluateAnd(children),
                RuleConditionGroupOperator.Or => EvaluateOr(children),
                _ => ConditionEvaluation.Unknown($"Unsupported group operator '{groupOperator}'.")
            };
        }

        if (condition.Conditions is not null || string.IsNullOrWhiteSpace(condition.ConditionId))
        {
            return ConditionEvaluation.Unknown("Condition leaf is malformed.");
        }
        return evaluateLeaf(condition);
    }

    public static RuleMatchPlan Plan(
        IEnumerable<RuleDefinition> rules,
        Func<RuleConditionNode, ConditionEvaluation> evaluateLeaf,
        IReadOnlySet<string> supportedActionTypeIds)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(evaluateLeaf);
        ArgumentNullException.ThrowIfNull(supportedActionTypeIds);

        var matched = new List<PlannedRule>();
        var indeterminate = new List<PlannedRule>();
        foreach (var rule in rules.OrderBy(static rule => rule.Order))
        {
            if (!rule.Enabled)
            {
                continue;
            }

            var condition = EvaluateCondition(rule.Condition, evaluateLeaf);
            if (condition.Value == RuleTruthValue.Unknown)
            {
                indeterminate.Add(new PlannedRule(rule, condition, []));
                continue;
            }
            if (condition.Value != RuleTruthValue.True)
            {
                continue;
            }

            var actions = PlanActions(rule.Actions, supportedActionTypeIds);
            var plannedRule = new PlannedRule(rule, condition, actions);
            matched.Add(plannedRule);
            if (rule.AfterMatch == RuleAfterMatch.StopProcessingRules)
            {
                break;
            }
        }
        return new RuleMatchPlan(matched, indeterminate);
    }

    public static RuleMatchPlan Plan(
        IEnumerable<RuleDefinition> rules,
        IRuleConditionResolver conditionResolver,
        RuleEvaluationContext context,
        IReadOnlySet<string> supportedActionTypeIds)
    {
        ArgumentNullException.ThrowIfNull(conditionResolver);
        ArgumentNullException.ThrowIfNull(context);
        return Plan(
            rules,
            condition => conditionResolver.Resolve(condition, context),
            supportedActionTypeIds);
    }

    private static IReadOnlyList<PlannedRuleAction> PlanActions(
        IReadOnlyList<RuleActionDefinition> actions,
        IReadOnlySet<string> supportedActionTypeIds)
    {
        var statuses = new Dictionary<string, PlannedActionStatus>(StringComparer.Ordinal);
        var planned = new List<PlannedRuleAction>(actions.Count);
        foreach (var action in actions)
        {
            var status = !supportedActionTypeIds.Contains(action.TypeId)
                ? PlannedActionStatus.SkippedUnsupported
                : action.DependsOnActionIds.Any(dependency =>
                    !statuses.TryGetValue(dependency, out var dependencyStatus) ||
                    dependencyStatus != PlannedActionStatus.Planned)
                    ? PlannedActionStatus.SkippedDependency
                    : PlannedActionStatus.Planned;

            var detail = status switch
            {
                PlannedActionStatus.SkippedUnsupported => $"Action type '{action.TypeId}' is unsupported.",
                PlannedActionStatus.SkippedDependency => "A required action was not planned.",
                _ => null
            };
            planned.Add(new PlannedRuleAction(action, status, detail));
            statuses[action.Id] = status;
        }
        return planned;
    }

    private static ConditionEvaluation EvaluateAnd(IReadOnlyList<ConditionEvaluation> children)
    {
        if (children.Any(static child => child.Value == RuleTruthValue.False))
        {
            return ConditionEvaluation.False;
        }
        if (children.All(static child => child.Value == RuleTruthValue.True))
        {
            return ConditionEvaluation.True;
        }
        return ConditionEvaluation.Unknown(FirstDiagnostic(children));
    }

    private static ConditionEvaluation EvaluateOr(IReadOnlyList<ConditionEvaluation> children)
    {
        if (children.Any(static child => child.Value == RuleTruthValue.True))
        {
            return ConditionEvaluation.True;
        }
        if (children.All(static child => child.Value == RuleTruthValue.False))
        {
            return ConditionEvaluation.False;
        }
        return ConditionEvaluation.Unknown(FirstDiagnostic(children));
    }

    private static string? FirstDiagnostic(IEnumerable<ConditionEvaluation> children) =>
        children.FirstOrDefault(static child => child.Value == RuleTruthValue.Unknown)?.Diagnostic;
}
