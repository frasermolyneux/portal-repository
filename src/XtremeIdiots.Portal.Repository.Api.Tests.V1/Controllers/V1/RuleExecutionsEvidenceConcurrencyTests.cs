using System.Data.Common;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed partial class RuleExecutionsControllerTests
{
    [Collection("SqlServer")]
    [Trait("Category", "SqlIntegration")]
    public sealed class EvidenceReadConcurrencyTests
    {
        [Theory]
        [InlineData("audit", RuleActionDecisionStatus.NotRun)]
        [InlineData("flow", RuleActionDecisionStatus.Skipped)]
        [InlineData("executions", RuleActionDecisionStatus.NotRun)]
        [InlineData("connection", RuleActionDecisionStatus.Skipped)]
        public Task ConcurrentActionClaimSupersedesStaleDecisionInRead(string endpoint, RuleActionDecisionStatus claimedDecision) =>
            SqlServerTestDatabase.RunAsync(async connectionString =>
            {
                var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
                await using var setup = new PortalDbContext(options);
                var serverId = await AddServer(setup);
                var connectionId = Guid.NewGuid();
                var api = (IRuleExecutionsApi)new RuleExecutionsController(setup);
                var start = StartRequest(serverId, connectionId) with { FrozenRulesJson = TwoActionEvidenceFrozenRules };
                var execution = (await api.StartExecution(serverId, start)).Result!.Data!;
                var evidence = Evidence();
                var progress = await api.UpdateProgress(serverId, execution.ExecutionId, new()
                {
                    ExpectedVersion = execution.Version,
                    State = RuleFeatureExecutionState.Evaluating,
                    EvaluationEvidence = evidence with
                    {
                        ActionDecisions =
                        [
                            evidence.ActionDecisions[0] with { Status = claimedDecision },
                            evidence.ActionDecisions[0] with { ActionIndex = 1, ActionId = "other" }
                        ]
                    }
                });
                Assert.Equal(HttpStatusCode.OK, progress.StatusCode);

                var gate = new EvidenceActionReadGate();
                await using var readDb = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                    .UseSqlServer(connectionString).AddInterceptors(gate).Options);
                var reader = (IRuleExecutionsApi)new RuleExecutionsController(readDb);

                async Task<(RuleEvaluationEvidence? Evidence, int Actions)> ReadAsync()
                {
                    switch (endpoint)
                    {
                        case "audit":
                            var audit = await reader.SearchRuleExecutionAudit(new() { GameServerId = serverId });
                            Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
                            var auditItem = Assert.Single(audit.Result!.Data!.Items!);
                            return (auditItem.EvaluationEvidence, auditItem.Actions.Length);
                        case "flow":
                            var flow = await reader.GetRuleExecutionFlow(execution.ExecutionId);
                            Assert.Equal(HttpStatusCode.OK, flow.StatusCode);
                            return (flow.Result!.Data!.Execution.EvaluationEvidence, flow.Result.Data.Execution.Actions.Length);
                        case "executions":
                            var executions = await reader.SearchRuleExecutions(new() { GameServerId = serverId, FeatureId = "vpn-protection" });
                            Assert.Equal(HttpStatusCode.OK, executions.StatusCode);
                            var executionItem = Assert.Single(executions.Result!.Data!.Items!);
                            return (executionItem.EvaluationEvidence, executionItem.Actions.Length);
                        case "connection":
                            var connection = await reader.GetConnectionExecutions(serverId, connectionId);
                            Assert.Equal(HttpStatusCode.OK, connection.StatusCode);
                            var connectionItem = Assert.Single(connection.Result!.Data!);
                            return (connectionItem.EvaluationEvidence, connectionItem.Actions.Length);
                        default:
                            throw new ArgumentOutOfRangeException(nameof(endpoint));
                    }
                }

                var read = ReadAsync();
                try
                {
                    await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
                    await using var claimDb = new PortalDbContext(options);
                    var claim = await ((IRuleExecutionsApi)new RuleExecutionsController(claimDb)).ClaimAction(
                        serverId, execution.ExecutionId, ClaimRequest(serverId, progress.Result!.Data!, new string('a', 64)));
                    Assert.Equal(HttpStatusCode.Created, claim.StatusCode);
                }
                finally
                {
                    gate.Release.TrySetResult();
                }

                var result = await read;
                Assert.Equal(1, result.Actions);
                Assert.Equal("other", Assert.Single(result.Evidence!.ActionDecisions).ActionId);
            });

        private const string TwoActionEvidenceFrozenRules = /*lang=json,strict*/ """
            {"rules":[{"id":"rule-1","condition":{"groupOperator":"And","conditions":[{"conditionId":"always"}]},
              "actions":[{"id":"kick-player","typeId":"player.kick","parameters":{"reason":"test"}},
                         {"id":"other","typeId":"player.kick","parameters":{"reason":"test"}}]}]}
            """;

        private sealed class EvidenceActionReadGate : DbCommandInterceptor
        {
            public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
                DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
                CancellationToken cancellationToken = default)
            {
                if (command.CommandText.Contains("FROM [RuleActionExecutions]", StringComparison.Ordinal) && Entered.TrySetResult())
                {
                    await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                }
                return result;
            }
        }
    }
}
