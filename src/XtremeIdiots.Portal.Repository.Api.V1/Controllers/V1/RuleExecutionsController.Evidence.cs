using System.Text.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public sealed partial class RuleExecutionsController
{
    private static bool ValidEvaluationEvidence(RuleEvaluationEvidence? evidence)
    {
        if (evidence is null)
        {
            return true;
        }
        if (!evidence.HasSupportedSchema || evidence.Nodes is null || evidence.ActionDecisions is null ||
            evidence.Nodes.Length > RuleEvaluationEvidence.MaximumNodeCount ||
            evidence.ActionDecisions.Length > RuleEvaluationEvidence.MaximumActionDecisionCount ||
            evidence.OmittedNodeCount < 0 || evidence.OmittedActionDecisionCount < 0)
        {
            return false;
        }

        var nodeKeys = new HashSet<(int RuleIndex, RuleEvidenceNodeKind Kind, string? ConditionPath, string? CheckId)>();
        foreach (var node in evidence.Nodes)
        {
            if (node is null || node.RuleIndex < 0 || !ValidEvidenceId(node.RuleId) ||
                !Enum.IsDefined(node.Kind) || node.Kind == RuleEvidenceNodeKind.Unrecognized ||
                !Enum.IsDefined(node.Status) || node.Status == RuleEvaluationNodeStatus.Unrecognized ||
                !ValidEvidenceTimestamp(node.ObservedAtUtc) ||
                !ValidEvidenceMetadata(node.ReasonCode, RuleEvaluationEvidence.MaximumReasonCodeLength) ||
                !ValidEvidenceMetadata(node.Source, RuleEvaluationEvidence.MaximumMetadataLength) ||
                !ValidEvidenceMetadata(node.SourceVersion, RuleEvaluationEvidence.MaximumMetadataLength) ||
                node.ConditionId is not null && !ValidEvidenceId(node.ConditionId) ||
                node.ConditionPath is { } path &&
                    (path.Length > RuleEvaluationEvidence.MaximumConditionPathDepth || path.Any(index => index < 0)))
            {
                return false;
            }

            var validCoordinate = node.Kind switch
            {
                RuleEvidenceNodeKind.Rule when node.ConditionPath is null && node.ConditionId is null && node.CheckId is null => true,
                RuleEvidenceNodeKind.Condition when node.ConditionPath is not null && node.CheckId is null => true,
                RuleEvidenceNodeKind.Check when ValidEvidenceMetadata(node.CheckId, RuleEvaluationEvidence.MaximumMetadataLength) &&
                    node.CheckId is not null && (node.ConditionPath is not null || node.ConditionId is null) => true,
                _ => false
            };
            var conditionPath = node.ConditionPath is null ? null : string.Join(',', node.ConditionPath);
            if (!validCoordinate || !nodeKeys.Add((node.RuleIndex, node.Kind, conditionPath, node.CheckId)))
            {
                return false;
            }
        }

        var actionKeys = new HashSet<(int RuleIndex, int ActionIndex)>();
        return evidence.ActionDecisions.All(action =>
            action is not null && action.RuleIndex >= 0 && action.ActionIndex >= 0 &&
            ValidEvidenceId(action.RuleId) && ValidEvidenceId(action.ActionId) &&
            Enum.IsDefined(action.Status) && action.Status != RuleActionDecisionStatus.Unrecognized &&
            ValidEvidenceTimestamp(action.ObservedAtUtc) &&
            ValidEvidenceMetadata(action.ReasonCode, RuleEvaluationEvidence.MaximumReasonCodeLength) &&
            actionKeys.Add((action.RuleIndex, action.ActionIndex)));
    }

    private static bool EvidenceMatchesFrozenRules(RuleEvaluationEvidence evidence, string frozenRulesJson)
    {
        if (evidence.Nodes.Length == 0 && evidence.ActionDecisions.Length == 0)
        {
            return true;
        }

        var (status, rules) = ParseFlowRules(frozenRulesJson);
        if (status != RuleExecutionStructureStatus.Available)
        {
            return false;
        }

        foreach (var node in evidence.Nodes)
        {
            if (node.RuleIndex >= rules.Length || rules[node.RuleIndex].Id != node.RuleId)
            {
                return false;
            }
            if (node.ConditionPath is not { } path)
            {
                continue;
            }
            var condition = rules[node.RuleIndex].Condition;
            foreach (var index in path)
            {
                if (index >= condition.Conditions.Length)
                {
                    return false;
                }
                condition = condition.Conditions[index];
            }
            if (node.ConditionId is not null && node.ConditionId != condition.ConditionId)
            {
                return false;
            }
        }

        return evidence.ActionDecisions.All(action =>
            action.RuleIndex < rules.Length && rules[action.RuleIndex].Id == action.RuleId &&
            action.ActionIndex < rules[action.RuleIndex].Actions.Length &&
            rules[action.RuleIndex].Actions[action.ActionIndex].Id == action.ActionId);
    }

    private static string? RemoveClaimedActionDecision(string? json, int ruleIndex, int actionIndex)
    {
        if (json is null)
        {
            return null;
        }
        var evidence = Deserialize<RuleEvaluationEvidence>(json);
        if (!evidence.HasSupportedSchema)
        {
            return json;
        }
        var retained = evidence.ActionDecisions.Where(action =>
            action.RuleIndex != ruleIndex || action.ActionIndex != actionIndex).ToArray();
        return retained.Length == evidence.ActionDecisions.Length
            ? json
            : JsonSerializer.Serialize(evidence with { ActionDecisions = retained }, JsonOptions);
    }

    private static RuleEvaluationEvidence? ProjectEvidence(string? json, IEnumerable<RuleActionExecution> actions)
    {
        if (json is null)
        {
            return null;
        }
        var evidence = Deserialize<RuleEvaluationEvidence>(json);
        if (!evidence.HasSupportedSchema || evidence.ActionDecisions is not { Length: > 0 })
        {
            return evidence;
        }
        var claimed = actions.Select(action => (action.RuleIndex, action.ActionIndex)).ToHashSet();
        var retained = evidence.ActionDecisions.Where(decision =>
            decision is null || !claimed.Contains((decision.RuleIndex, decision.ActionIndex))).ToArray();
        return retained.Length == evidence.ActionDecisions.Length
            ? evidence : evidence with { ActionDecisions = retained };
    }

    private static bool ValidEvidenceId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= RuleSetConstants.MaxIdLength && value.Trim() == value;

    private static bool ValidEvidenceMetadata(string? value, int maximumLength) =>
        value is null || value.Length is > 0 && value.Length <= maximumLength &&
        value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-' or ':' or '+');

    private static bool ValidEvidenceTimestamp(DateTimeOffset value) =>
        value != default && value.Offset == TimeSpan.Zero && value <= DateTimeOffset.UtcNow.Add(NativeTimestampSkewAllowance);
}
