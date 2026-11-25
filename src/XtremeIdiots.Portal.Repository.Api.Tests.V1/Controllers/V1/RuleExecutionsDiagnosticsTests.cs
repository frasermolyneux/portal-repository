using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed partial class RuleExecutionsControllerTests
{
    [Fact]
    public async Task GenericAuditFiltersAndPaginatesAcrossServersAndFeatures()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var firstServer = await AddServer(context);
        var secondServer = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var baseline = DateTime.UtcNow.AddHours(-1);
        var ids = new List<Guid>();
        foreach (var (server, feature) in new[]
        {
            (firstServer, "welcome-messages"), (secondServer, "welcome-messages"),
            (firstServer, "other-feature")
        })
        {
            var start = StartRequest(server, Guid.NewGuid()) with { FeatureId = feature };
            var execution = (await api.StartExecution(server, start)).Result!.Data!;
            var updated = await api.UpdateProgress(server, execution.ExecutionId, new()
            {
                ExpectedVersion = execution.Version,
                State = RuleFeatureExecutionState.Waiting,
                Coordination = new()
                {
                    DueAtUtc = start.Trigger.OccurredAtUtc,
                    ExpiresAtUtc = start.Trigger.OccurredAtUtc.AddMinutes(2),
                    WaitingReason = "Waiting for context."
                },
                Diagnostics = [new() { Code = RuleExecutionDiagnosticCode.WaitingForContext }]
            });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            context.RuleFeatureExecutions.Single(item => item.ExecutionId == execution.ExecutionId).CreatedAtUtc =
                baseline.AddMinutes(ids.Count);
            await context.SaveChangesAsync();
            ids.Add(execution.ExecutionId);
        }

        var result = await api.SearchRuleExecutionAudit(new()
        {
            FromUtc = baseline,
            ToUtc = baseline.AddMinutes(2),
            SkipEntries = 1,
            TakeEntries = 1
        });
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(3, result.Result!.Pagination!.TotalCount);
        Assert.Equal(2, result.Result.Pagination.FilteredCount);
        var item = Assert.Single(result.Result.Data!.Items!);
        Assert.Equal(ids[0], item.ExecutionId);
        Assert.Equal(RuleFeatureExecutionState.Waiting, item.State);
        Assert.Equal("Waiting for context.", item.Coordination!.WaitingReason);
        Assert.True(item.Coordination.ExpiresAtUtc > item.Coordination.DueAtUtc);
        Assert.Equal(RuleExecutionDiagnosticCode.WaitingForContext, Assert.Single(item.Diagnostics).Code);
        Assert.Empty(item.Actions);
        Assert.Empty(item.PreparedActions);
        var current = context.RuleFeatureExecutions.Single(x => x.ExecutionId == ids[0]);
        current.State = nameof(RuleFeatureExecutionState.Completed);
        await context.SaveChangesAsync();
        var completed = await api.SearchRuleExecutionAudit(new() { GameServerId = firstServer, FeatureId = "welcome-messages" });
        Assert.Null(Assert.Single(completed.Result!.Data!.Items!).Coordination!.WaitingReason);
        current.CoordinationJson = null;
        await context.SaveChangesAsync();
        var historical = await api.SearchRuleExecutionAudit(new() { GameServerId = firstServer, FeatureId = "welcome-messages" });
        Assert.Null(Assert.Single(historical.Result!.Data!.Items!).Coordination);

        var serverResults = await api.SearchRuleExecutionAudit(new() { GameServerId = firstServer });
        Assert.Equal(new[] { ids[2], ids[0] }, serverResults.Result!.Data!.Items!.Select(x => x.ExecutionId));
        var featureResults = await api.SearchRuleExecutionAudit(new() { FeatureId = "welcome-messages" });
        Assert.Equal(new[] { ids[1], ids[0] }, featureResults.Result!.Data!.Items!.Select(x => x.ExecutionId));
        var both = await api.SearchRuleExecutionAudit(new() { GameServerId = secondServer, FeatureId = "welcome-messages" });
        Assert.Equal(ids[1], Assert.Single(both.Result!.Data!.Items!).ExecutionId);
        var tied = context.RuleFeatureExecutions.Where(x => x.ExecutionId == ids[0] || x.ExecutionId == ids[1]).ToArray();
        foreach (var row in tied)
        {
            row.CreatedAtUtc = baseline;
        }
        await context.SaveChangesAsync();
        var ties = await api.SearchRuleExecutionAudit(new() { FeatureId = "welcome-messages", TakeEntries = 1 });
        var next = await api.SearchRuleExecutionAudit(new() { FeatureId = "welcome-messages", TakeEntries = 1, SkipEntries = 1 });
        Assert.Equal(ids.Take(2).Order().ToArray(),
            new[] { Assert.Single(ties.Result!.Data!.Items!).ExecutionId, Assert.Single(next.Result!.Data!.Items!).ExecutionId });

        Assert.Equal(HttpStatusCode.BadRequest, (await api.SearchRuleExecutionAudit(new() { GameServerId = Guid.Empty })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SearchRuleExecutionAudit(new() { FeatureId = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SearchRuleExecutionAudit(new() { TakeEntries = 101 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SearchRuleExecutionAudit(new() { SkipEntries = -1 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SearchRuleExecutionAudit(new() { FromUtc = baseline, ToUtc = baseline })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SearchRuleExecutionAudit(new() { FromUtc = DateTime.SpecifyKind(baseline, DateTimeKind.Unspecified) })).StatusCode);
    }

    [Fact]
    public async Task ExecutionSearchIncludesNoActionStatesWithinFeatureServerAndExclusiveTimeRange()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var otherServer = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var baseline = DateTime.UtcNow.AddHours(-1);
        var diagnostics = new[] { new RuleExecutionDiagnostic
        {
            Code = RuleExecutionDiagnosticCode.FallbackUsed, TokenKey = "context.server.Name",
            Source = "ServerMetadata", Detail = "Display value unavailable."
        } };
        var states = new[] { RuleFeatureExecutionState.Waiting, RuleFeatureExecutionState.Stopped, RuleFeatureExecutionState.Evaluating };
        Guid waitingId = default;
        for (var index = 0; index < states.Length; index++)
        {
            var start = StartRequest(serverId, Guid.NewGuid()) with { FeatureId = "welcome-messages" };
            var execution = (await api.StartExecution(serverId, start)).Result!.Data!;
            var progress = await api.UpdateProgress(serverId, execution.ExecutionId, new()
            {
                ExpectedVersion = execution.Version,
                State = states[index],
                Diagnostics = diagnostics,
                Coordination = new()
                {
                    DueAtUtc = start.Trigger.OccurredAtUtc,
                    ExpiresAtUtc = start.Trigger.OccurredAtUtc.AddSeconds(120),
                    WaitingReason = "Waiting for context."
                }
            });
            Assert.Equal(HttpStatusCode.OK, progress.StatusCode);
            context.RuleFeatureExecutions.Single(item => item.ExecutionId == execution.ExecutionId).CreatedAtUtc = baseline.AddMinutes(index);
            await context.SaveChangesAsync();
            if (index == 0)
            {
                waitingId = execution.ExecutionId;
            }
        }
        await api.StartExecution(serverId, StartRequest(serverId, Guid.NewGuid()));
        await api.StartExecution(otherServer, StartRequest(otherServer, Guid.NewGuid()) with { FeatureId = "welcome-messages" });
        await context.SaveChangesAsync();
        var query = new RuleFeatureExecutionQueryDto
        {
            GameServerId = serverId,
            FeatureId = "welcome-messages",
            FromUtc = baseline,
            ToUtc = baseline.AddMinutes(2),
            SkipEntries = 1,
            TakeEntries = 1
        };
        var result = await api.SearchRuleExecutions(query);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(3, result.Result!.Pagination!.TotalCount);
        Assert.Equal(2, result.Result.Pagination.FilteredCount);
        var waiting = Assert.Single(result.Result.Data!.Items!);
        Assert.Equal(waitingId, waiting.ExecutionId);
        Assert.Equal(RuleFeatureExecutionState.Waiting, waiting.State);
        Assert.Equal("Waiting for context.", waiting.Coordination!.WaitingReason);
        Assert.Equal(diagnostics, waiting.Diagnostics);
        Assert.Empty(waiting.Actions);
        Assert.Empty(waiting.PreparedActions);
        Assert.Empty((await api.SearchRuleExecutions(query with { SkipEntries = 2 })).Result!.Data!.Items!);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SearchRuleExecutions(query with { TakeEntries = 101 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SearchRuleExecutions(query with { GameServerId = Guid.Empty })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SearchRuleExecutions(query with { FromUtc = query.ToUtc })).StatusCode);
    }

    [Fact]
    public async Task ProgressDiagnosticsReplacePreserveAndClearWithOptimisticConcurrency()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId, StartRequest(serverId, Guid.NewGuid()))).Result!.Data!;
        Assert.Empty(execution.Diagnostics);
        var request = new UpdateRuleFeatureExecutionProgressDto
        {
            ExpectedVersion = execution.Version,
            Diagnostics = [new()
            {
                Code = RuleExecutionDiagnosticCode.SourceUnavailable, Source = "ServerMetadata",
                RuleId = "rule-1", ActionId = "kick-player", Detail = "Source unavailable."
            }]
        };
        var updated = (await api.UpdateProgress(serverId, execution.ExecutionId, request)).Result!.Data!;
        Assert.Equal(request.Diagnostics, updated.Diagnostics);
        Assert.Equal(HttpStatusCode.Conflict, (await api.UpdateProgress(serverId, execution.ExecutionId, request with { Diagnostics = [] })).StatusCode);
        var preserved = (await api.UpdateProgress(serverId, execution.ExecutionId,
            request with { ExpectedVersion = updated.Version, Diagnostics = null })).Result!.Data!;
        Assert.Equal(request.Diagnostics, preserved.Diagnostics);
        var cleared = (await api.UpdateProgress(serverId, execution.ExecutionId,
            request with { ExpectedVersion = preserved.Version, Diagnostics = [] })).Result!.Data!;
        Assert.Empty(cleared.Diagnostics);
        request = request with { ExpectedVersion = cleared.Version };
        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateProgress(serverId, execution.ExecutionId, request with
        {
            Diagnostics = Enumerable.Repeat(request.Diagnostics![0], 257).ToArray()
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateProgress(serverId, execution.ExecutionId, request with
        {
            Diagnostics = [request.Diagnostics![0] with { Detail = new string('x', 1025) }]
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateProgress(serverId, execution.ExecutionId, request with
        {
            Diagnostics = [new() { Code = RuleExecutionDiagnosticCode.DiagnosticsOmitted }]
        })).StatusCode);
        var bounded = Enumerable.Repeat(request.Diagnostics![0], 255).Append(new()
        {
            Code = RuleExecutionDiagnosticCode.DiagnosticsOmitted,
            OmittedCount = 37,
            Detail = "37 additional diagnostics omitted."
        }).ToArray();
        var accepted = await api.UpdateProgress(serverId, execution.ExecutionId, request with { Diagnostics = bounded });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(37, accepted.Result!.Data!.Diagnostics[^1].OmittedCount);
        Assert.Equal(bounded, (await api.GetExecution(serverId, execution.ExecutionId)).Result!.Data!.Diagnostics);
    }
}
