using System.Net;
using System.Text.Json;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed partial class RuleExecutionsControllerTests
{
    [Fact]
    public async Task EvaluationEvidenceIsVersionedAndClaimedActionsSupersedeNotRunDecisions()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var occurrenceId = Guid.NewGuid();
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var start = StartRequest(serverId, Guid.NewGuid(), occurrenceId: occurrenceId) with
        {
            FrozenRulesJson = EvidenceFrozenRules
        };
        var execution = (await api.StartExecution(serverId, start)).Result!.Data!;
        Assert.Null(execution.EvaluationEvidence);

        var evidence = Evidence();
        var request = new UpdateRuleFeatureExecutionProgressDto
        {
            ExpectedVersion = execution.Version,
            State = RuleFeatureExecutionState.Evaluating,
            EvaluationEvidence = evidence
        };
        var updated = (await api.UpdateProgress(serverId, execution.ExecutionId, request)).Result!.Data!;
        Assert.Equivalent(evidence, updated.EvaluationEvidence);
        Assert.Equal(HttpStatusCode.Conflict, (await api.UpdateProgress(serverId, execution.ExecutionId, request)).StatusCode);

        var preserved = (await api.UpdateProgress(serverId, execution.ExecutionId,
            request with { ExpectedVersion = updated.Version, EvaluationEvidence = null })).Result!.Data!;
        Assert.Equivalent(evidence, preserved.EvaluationEvidence);
        Assert.Equivalent(evidence, (await api.GetExecution(serverId, execution.ExecutionId)).Result!.Data!.EvaluationEvidence);
        var audit = Assert.Single((await api.SearchRuleExecutionAudit(new()
        {
            GameServerId = serverId,
            TriggerOccurrenceId = occurrenceId
        })).Result!.Data!.Items!);
        Assert.Equivalent(evidence, audit.EvaluationEvidence);
        Assert.Equal(occurrenceId, audit.TriggerOccurrenceId);
        var flow = (await api.GetRuleExecutionFlow(execution.ExecutionId)).Result!.Data!;
        Assert.Equivalent(evidence, flow.Execution.EvaluationEvidence);
        Assert.Equal(occurrenceId, flow.Execution.TriggerOccurrenceId);

        var claim = await api.ClaimAction(serverId, execution.ExecutionId,
            ClaimRequest(serverId, preserved, new string('a', 64)));
        Assert.Equal(HttpStatusCode.Created, claim.StatusCode);
        var claimedExecution = claim.Result!.Data!.Execution;
        Assert.Empty(claimedExecution.EvaluationEvidence!.ActionDecisions);
        Assert.Single(claimedExecution.Actions);
        Assert.Equal(HttpStatusCode.Conflict, (await api.UpdateProgress(serverId, execution.ExecutionId,
            request with { ExpectedVersion = claimedExecution.Version })).StatusCode);
        var afterClaim = Assert.Single((await api.SearchRuleExecutionAudit(new() { GameServerId = serverId }))
            .Result!.Data!.Items!);
        Assert.Empty(afterClaim.EvaluationEvidence!.ActionDecisions);
        Assert.Single(afterClaim.Actions);
    }

    [Fact]
    public async Task EvaluationEvidenceRejectsInvalidCoordinatesAndUnboundedOrUnrecognizedValues()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid()) with { FrozenRulesJson = EvidenceFrozenRules })).Result!.Data!;
        var evidence = Evidence();
        var node = evidence.Nodes[0];
        var action = evidence.ActionDecisions[0];
        var invalid = new[]
        {
            evidence with { SchemaVersion = 2 },
            evidence with { OmittedNodeCount = -1 },
            evidence with { Nodes = Enumerable.Repeat(node, RuleEvaluationEvidence.MaximumNodeCount + 1).ToArray() },
            evidence with { Nodes = [node, node] },
            evidence with { Nodes = [node with { Status = RuleEvaluationNodeStatus.Unrecognized }] },
            evidence with { Nodes = [node with { ObservedAtUtc = default }] },
            evidence with { Nodes = [node with { ObservedAtUtc = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(1)) }] },
            evidence with { Nodes = [node with { ReasonCode = "contains private text" }] },
            evidence with { Nodes = [node with { RuleId = "other-rule" }] },
            evidence with { Nodes = [node with { ConditionPath = [1] }] },
            evidence with { Nodes = [node with { ConditionId = "other-condition" }] },
            evidence with { ActionDecisions = [action, action] },
            evidence with { ActionDecisions = [action with { ActionId = "other-action" }] },
            evidence with { ActionDecisions = [action with { Status = RuleActionDecisionStatus.Unrecognized }] }
        };
        foreach (var item in invalid)
        {
            var result = await api.UpdateProgress(serverId, execution.ExecutionId, new()
            {
                ExpectedVersion = execution.Version,
                State = RuleFeatureExecutionState.Evaluating,
                EvaluationEvidence = item
            });
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
        var unchanged = (await api.GetExecution(serverId, execution.ExecutionId)).Result!.Data!;
        Assert.Equal(execution.Version, unchanged.Version);
        Assert.Null(unchanged.EvaluationEvidence);
    }

    [Fact]
    public async Task CheckEvidenceIsUniquePerRuleAndConditionPath()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid()) with { FrozenRulesJson = TwoConditionEvidenceFrozenRules })).Result!.Data!;
        var check = new RuleEvaluationNodeEvidence
        {
            RuleId = "rule-1",
            Kind = RuleEvidenceNodeKind.Check,
            CheckId = "freshness",
            Status = RuleEvaluationNodeStatus.Matched,
            ObservedAtUtc = DateTimeOffset.UtcNow
        };
        var evidence = new RuleEvaluationEvidence
        {
            Nodes =
            [
                check,
                check with { ConditionPath = [] },
                check with { ConditionPath = [0], ConditionId = "always" },
                check with { ConditionPath = [1], ConditionId = "always" },
                check with { ConditionPath = [1], ConditionId = "always", CheckId = "other" }
            ]
        };
        var update = await api.UpdateProgress(serverId, execution.ExecutionId, new()
        {
            ExpectedVersion = execution.Version,
            State = RuleFeatureExecutionState.Evaluating,
            EvaluationEvidence = evidence
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(5, update.Result!.Data!.EvaluationEvidence!.Nodes.Length);
        var duplicate = await api.UpdateProgress(serverId, execution.ExecutionId, new()
        {
            ExpectedVersion = update.Result.Data.Version,
            State = RuleFeatureExecutionState.Evaluating,
            EvaluationEvidence = evidence with { Nodes = [check with { ConditionPath = [0] }, check with { ConditionPath = [0] }] }
        });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    [Fact]
    public async Task HistoricalAndUnknownSchemaEvidenceRemainDistinctAndOccurrenceFilterIsScoped()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var firstOccurrence = Guid.NewGuid();
        var secondOccurrence = Guid.NewGuid();
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var first = (await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), occurrenceId: firstOccurrence))).Result!.Data!;
        var second = (await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), occurrenceId: secondOccurrence))).Result!.Data!;
        context.RuleFeatureExecutions.Single(item => item.ExecutionId == second.ExecutionId).EvaluationEvidenceJson =
            JsonSerializer.Serialize(new { schemaVersion = 2, nodes = new { futureShape = true } });
        await context.SaveChangesAsync();

        Assert.Null((await api.GetExecution(serverId, first.ExecutionId)).Result!.Data!.EvaluationEvidence);
        var unsupported = (await api.GetExecution(serverId, second.ExecutionId)).Result!.Data!.EvaluationEvidence!;
        Assert.False(unsupported.HasSupportedSchema);
        Assert.Equal(2, unsupported.SchemaVersion);
        var matches = await api.SearchRuleExecutionAudit(new()
        {
            GameServerId = serverId,
            TriggerOccurrenceId = secondOccurrence
        });
        var match = Assert.Single(matches.Result!.Data!.Items!);
        Assert.Equal(second.ExecutionId, match.ExecutionId);
        Assert.False(match.EvaluationEvidence!.HasSupportedSchema);
        Assert.False((await api.GetRuleExecutionFlow(second.ExecutionId)).Result!.Data!.Execution.EvaluationEvidence!.HasSupportedSchema);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await api.SearchRuleExecutionAudit(new() { TriggerOccurrenceId = Guid.Empty })).StatusCode);
    }

    private const string EvidenceFrozenRules = /*lang=json,strict*/ """
        {"rules":[{"id":"rule-1","condition":{"groupOperator":"And","conditions":[{"conditionId":"always"}]},
          "actions":[{"id":"kick-player","typeId":"player.kick","parameters":{"reason":"test"}}]}]}
        """;

    private const string TwoConditionEvidenceFrozenRules = /*lang=json,strict*/ """
        {"rules":[{"id":"rule-1","condition":{"groupOperator":"And","conditions":[{"conditionId":"always"},{"conditionId":"always"}]},
          "actions":[{"id":"kick-player","typeId":"player.kick","parameters":{"reason":"test"}}]}]}
        """;

    private static RuleEvaluationEvidence Evidence() => new()
    {
        Nodes =
        [
            new()
            {
                RuleId = "rule-1",
                Kind = RuleEvidenceNodeKind.Condition,
                ConditionPath = [0],
                ConditionId = "always",
                Status = RuleEvaluationNodeStatus.Matched,
                ObservedAtUtc = DateTimeOffset.UtcNow
            }
        ],
        ActionDecisions =
        [
            new()
            {
                RuleId = "rule-1",
                ActionId = "kick-player",
                Status = RuleActionDecisionStatus.NotRun,
                ObservedAtUtc = DateTimeOffset.UtcNow
            }
        ]
    };
}
