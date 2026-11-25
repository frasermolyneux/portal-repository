using XtremeIdiots.Portal.RulesEngine.V1;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.RulesEngine.V1.Tests;

public sealed class RuleMatcherTests
{
    [Fact]
    public void AlwaysMatchesWithoutCallingResolverButMissingConditionsRemainUnknown()
    {
        Assert.Equal(RuleTruthValue.True, RuleMatcher.EvaluateCondition(Leaf("always"),
            _ => throw new InvalidOperationException()).Value);
        Assert.Equal(RuleTruthValue.Unknown, RuleMatcher.EvaluateCondition(null, _ => ConditionEvaluation.True).Value);
    }

    [Fact]
    public void FirstMatchSelectionWaitsForUnknownHigherPriorityRule()
    {
        var plan = RuleMatcher.Plan(
            [
                new() { Id = "member", Order = 0, Condition = Leaf("player.tags") },
                new() { Id = "general", Order = 1, Condition = Leaf("always") }
            ],
            _ => ConditionEvaluation.Unknown("Tags unavailable"), new HashSet<string>(), deferOnIndeterminate: true);
        Assert.Empty(plan.MatchedRules);
        Assert.Equal("member", Assert.Single(plan.IndeterminateRules).Rule.Id);
    }

    [Theory]
    [InlineData(RuleConditionGroupOperator.And, RuleTruthValue.False, RuleTruthValue.Unknown, RuleTruthValue.False)]
    [InlineData(RuleConditionGroupOperator.And, RuleTruthValue.True, RuleTruthValue.Unknown, RuleTruthValue.Unknown)]
    [InlineData(RuleConditionGroupOperator.Or, RuleTruthValue.True, RuleTruthValue.Unknown, RuleTruthValue.True)]
    [InlineData(RuleConditionGroupOperator.Or, RuleTruthValue.False, RuleTruthValue.Unknown, RuleTruthValue.Unknown)]
    public void EvaluateConditionUsesFalseDominantAndAndTrueDominantOr(
        RuleConditionGroupOperator groupOperator,
        RuleTruthValue first,
        RuleTruthValue second,
        RuleTruthValue expected)
    {
        var condition = new RuleConditionNode
        {
            GroupOperator = groupOperator,
            Conditions = [Leaf("first"), Leaf("second")]
        };

        var result = RuleMatcher.EvaluateCondition(condition, leaf =>
            leaf.ConditionId == "first" ? new ConditionEvaluation(first) : new ConditionEvaluation(second));

        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void PlanContinuesAfterUnsupportedActionsAndSkipsTheirDependents()
    {
        var firstRule = new RuleDefinition
        {
            Id = "first",
            Order = 1,
            Condition = Leaf("match"),
            AfterMatch = RuleAfterMatch.ContinueToNextRule,
            Actions =
            [
                new() { Id = "unsupported", TypeId = "unsupported" },
                new() { Id = "dependent", TypeId = "supported", DependsOnActionIds = ["unsupported"] },
                new() { Id = "independent", TypeId = "supported" }
            ]
        };
        var secondRule = new RuleDefinition
        {
            Id = "second",
            Order = 2,
            Condition = Leaf("match"),
            Actions = [new() { Id = "stop", TypeId = "supported" }]
        };

        var plan = RuleMatcher.Plan(
            [secondRule, firstRule],
            _ => ConditionEvaluation.True,
            new HashSet<string>(["supported"], StringComparer.Ordinal));

        Assert.Equal(["first", "second"], plan.MatchedRules.Select(static rule => rule.Rule.Id));
        Assert.Equal(
            [PlannedActionStatus.SkippedUnsupported, PlannedActionStatus.SkippedDependency, PlannedActionStatus.Planned],
            plan.MatchedRules[0].Actions.Select(static action => action.Status));
    }

    [Fact]
    public void StopProcessingRulesStopsAfterMatchedRuleEvenWhenActionsAreUnsupported()
    {
        var rules = new[]
        {
            new RuleDefinition
            {
                Id = "first",
                Order = 1,
                Condition = Leaf("match"),
                AfterMatch = RuleAfterMatch.StopProcessingRules,
                Actions = [new() { Id = "unknown", TypeId = "unsupported" }]
            },
            new RuleDefinition { Id = "second", Order = 2, Condition = Leaf("match"), Actions = [new() { Id = "known", TypeId = "supported" }] }
        };

        var plan = RuleMatcher.Plan(rules, _ => ConditionEvaluation.True, new HashSet<string>(["supported"]));

        Assert.Single(plan.MatchedRules);
        Assert.Equal(PlannedActionStatus.SkippedUnsupported, plan.MatchedRules[0].Actions[0].Status);
    }

    private static RuleConditionNode Leaf(string id) => new()
    {
        ConditionId = id,
        Parameters = []
    };
}
