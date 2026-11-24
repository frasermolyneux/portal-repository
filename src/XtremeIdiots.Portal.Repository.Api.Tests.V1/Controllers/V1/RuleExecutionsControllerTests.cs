using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed class RuleExecutionsControllerTests
{
    [Fact]
    public async Task SearchRuleActions_FiltersWithinAuthorizedServerScopeAndReturnsAuditFields()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var firstServerId = await AddServer(context);
        var secondServerId = await AddServer(context);
        var sessionId = Guid.NewGuid();
        var otherSessionId = Guid.NewGuid();
        var otherServerSessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        context.SiegeSessions.AddRange(
            CreateSiegeSession(firstServerId, sessionId),
            CreateSiegeSession(firstServerId, otherSessionId),
            CreateSiegeSession(secondServerId, otherServerSessionId));

        var target = CreateAuditAction(firstServerId, "siege-mode", sessionId, now, RuleExecutionMode.DryRun);
        var otherSession = CreateAuditAction(firstServerId, "siege-mode", otherSessionId, now.AddMinutes(-1), RuleExecutionMode.Enforce);
        var otherFeature = CreateAuditAction(firstServerId, "vpn-protection", null, now.AddMinutes(-2), RuleExecutionMode.Enforce);
        var unauthorizedServer = CreateAuditAction(secondServerId, "siege-mode", otherServerSessionId, now.AddMinutes(-3), RuleExecutionMode.Enforce);
        context.RuleFeatureExecutions.AddRange(target.Execution, otherSession.Execution, otherFeature.Execution, unauthorizedServer.Execution);
        context.RuleActionExecutions.AddRange(target.Action, otherSession.Action, otherFeature.Action, unauthorizedServer.Action);
        await context.SaveChangesAsync();

        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var result = await api.SearchRuleActions(new RuleActionAuditQueryDto
        {
            GameServerIds = [firstServerId],
            FeatureId = "siege-mode"
        });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(3, result.Result!.Pagination!.TotalCount);
        Assert.Equal(2, result.Result.Pagination.FilteredCount);
        Assert.All(result.Result.Data!.Items!, action => Assert.Equal(firstServerId, action.GameServerId));

        var sessionResult = await api.SearchRuleActions(new RuleActionAuditQueryDto
        {
            GameServerIds = [firstServerId],
            SiegeSessionId = sessionId,
            FeatureId = "siege-mode"
        });
        Assert.Equal(1, sessionResult.Result!.Pagination!.FilteredCount);
        var audited = Assert.Single(sessionResult.Result!.Data!.Items!);
        Assert.Equal(target.Execution.ExecutionId, audited.ExecutionId);
        Assert.Equal("siege-mode", audited.FeatureId);
        Assert.Equal("rule-audit", audited.RuleId);
        Assert.Equal("action-audit", audited.ActionId);
        Assert.Equal(0, audited.RuleIndex);
        Assert.Equal(1, audited.ActionIndex);
        Assert.Equal(sessionId, audited.SiegeSessionId);
        Assert.Equal(firstServerId, audited.GameServerId);
        Assert.Equal(target.Execution.ConnectionId, audited.ConnectionId);
        Assert.Equal((ulong)7, audited.ConnectionGeneration);
        Assert.Equal("PLAYER-AUDIT", audited.PlayerGuid);
        Assert.Equal(5, audited.SlotId);
        Assert.Equal("moderation.observation", audited.ActionTypeId);
        Assert.Equal(RuleActionExecutionOutcome.WouldExecute, audited.Outcome);
        Assert.Equal(RuleExecutionMode.DryRun, audited.Mode);
        Assert.Equal(target.Action.OutcomeAtUtc, audited.OccurredAtUtc!.Value.UtcDateTime);
        Assert.Equal(target.Action.UpdatedAtUtc, audited.UpdatedAtUtc.UtcDateTime);
    }

    [Fact]
    public async Task SearchRuleActions_RequiresAnAuthorizedServerScope()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);

        var result = await api.SearchRuleActions(new RuleActionAuditQueryDto());

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task EnsureRuleActionAdminAction_SupportsObservationWithoutNativeDispatch()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Guid = "PLAYER1",
            FirstSeen = DateTime.UtcNow.AddDays(-1),
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var started = await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), actionTypeId: "moderation.observation"));
        var execution = started.Result!.Data!;
        const string logicalEffectId = "7123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var claimRequest = ClaimRequest(serverId, execution, logicalEffectId, RuleNativeEffectType.Unknown);
        var claimed = await api.ClaimAction(serverId, execution.ExecutionId, claimRequest);
        Assert.False(claimed.Result!.Data!.ShouldDispatch);

        var ensured = await api.EnsureRuleActionAdminAction(serverId, execution.ExecutionId, "kick-player",
            new EnsureRuleActionAdminActionDto
            {
                ExpectedVersion = claimed.Result.Data.Execution.Version,
                LogicalEffectId = logicalEffectId,
                Type = AdminActionType.Observation,
                Text = "Observed by rule"
            });
        Assert.Equal(HttpStatusCode.Created, ensured.StatusCode);

        var action = claimed.Result.Data.Action;
        var result = new RuleActionResult
        {
            GameServerId = serverId,
            ExecutionId = execution.ExecutionId,
            ActionId = action.Correlation.ActionId,
            LogicalEffectId = logicalEffectId,
            Correlation = action.Correlation,
            Outcome = RuleActionExecutionOutcome.Applied,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        var completed = await api.UpdateActionOutcome(serverId, execution.ExecutionId, action.Correlation.ActionId,
            new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = claimed.Result.Data.Execution.Version,
                Result = result,
                AdminActionId = ensured.Result!.Data!.AdminAction.AdminActionId,
                NextRuleIndex = 1,
                NextActionIndex = 0,
                NextState = RuleFeatureExecutionState.Completed
            });

        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal(RuleActionExecutionOutcome.Applied, completed.Result!.Data!.Action.Outcome);
        Assert.Equal((int)AdminActionType.Observation, (await context.AdminActions.SingleAsync()).Type);
        Assert.Equal(RuleFeatureExecutionState.Completed, completed.Result.Data.Execution.State);
    }

    [Fact]
    public async Task EnsureRuleActionAdminAction_LinksRecordAtomicallyWithoutAdvancingExecution()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Guid = "PLAYER1",
            FirstSeen = DateTime.UtcNow.AddDays(-1),
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var started = await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), actionTypeId: "moderation.ban"));
        var execution = started.Result!.Data!;
        const string logicalEffectId = "5123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var claimed = await api.ClaimAction(serverId, execution.ExecutionId, ClaimRequest(serverId, execution, logicalEffectId));
        var command = claimed.Result!.Data!.Action.Command;
        Assert.Equal("moderation.ban", claimed.Result.Data.Action.ActionTypeId);

        var request = new EnsureRuleActionAdminActionDto
        {
            ExpectedVersion = claimed.Result.Data.Execution.Version,
            LogicalEffectId = logicalEffectId,
            Type = AdminActionType.Ban,
            Text = "Rule-engine ban"
        };
        var created = await api.EnsureRuleActionAdminAction(
            serverId, execution.ExecutionId, command.ActionId, request);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.True(created.Result!.Data!.Created);
        Assert.NotEqual(Guid.Empty, created.Result.Data.AdminAction.AdminActionId);
        var savedAction = await context.RuleActionExecutions.SingleAsync();
        var savedExecution = await context.RuleFeatureExecutions.SingleAsync();
        Assert.Equal(created.Result.Data.AdminAction.AdminActionId, savedAction.AdminActionId);
        Assert.Equal(RuleActionExecutionOutcome.Claimed.ToString(), savedAction.Outcome);
        Assert.Equal(claimed.Result.Data.Execution.Version, savedExecution.Version);
        Assert.Equal(RuleFeatureExecutionState.AwaitingActionOutcome.ToString(), savedExecution.State);

        savedAction.CommandJson = JsonSerializer.Serialize(command with
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await context.SaveChangesAsync();

        var replay = await api.EnsureRuleActionAdminAction(
            serverId, execution.ExecutionId, command.ActionId, request);

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.False(replay.Result!.Data!.Created);
        Assert.Equal(created.Result.Data.AdminAction.AdminActionId, replay.Result.Data.AdminAction.AdminActionId);
        Assert.Single(await context.AdminActions.ToListAsync());
        Assert.Equal(claimed.Result.Data.Execution.Version,
            (await context.RuleFeatureExecutions.SingleAsync()).Version);
    }

    [Fact]
    public async Task EnsureRuleActionAdminAction_RejectsExpiredUnlinkedDecision()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        context.Players.Add(new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Guid = "PLAYER1",
            FirstSeen = DateTime.UtcNow.AddDays(-1),
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var started = await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), actionTypeId: "moderation.ban"));
        var execution = started.Result!.Data!;
        const string logicalEffectId = "6123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var claimRequest = ClaimRequest(serverId, execution, logicalEffectId);
        claimRequest = claimRequest with
        {
            Command = claimRequest.Command with { ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1) }
        };
        var claimed = await api.ClaimAction(serverId, execution.ExecutionId, claimRequest);
        var command = claimed.Result!.Data!.Action.Command;

        var result = await api.EnsureRuleActionAdminAction(
            serverId,
            execution.ExecutionId,
            command.ActionId,
            new EnsureRuleActionAdminActionDto
            {
                ExpectedVersion = claimed.Result.Data.Execution.Version,
                LogicalEffectId = logicalEffectId,
                Type = AdminActionType.Ban,
                Text = "Expired rule decision"
            });

        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        Assert.Empty(await context.AdminActions.ToListAsync());
        Assert.Null((await context.RuleActionExecutions.SingleAsync()).AdminActionId);
    }

    [Fact]
    public async Task ClaimAction_DeduplicatesExplicitLogicalEffectAcrossTriggersAndRuleRevisions()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var connectionId = Guid.NewGuid();
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var start = StartRequest(serverId, connectionId, "player.connected", Guid.NewGuid(), "revision-one");
        var first = await api.StartExecution(serverId, start);
        var secondStart = StartRequest(serverId, connectionId, "player.ip-resolved", Guid.NewGuid(), "revision-two",
            runtime: start.Trigger.Runtime);
        var second = await api.StartExecution(serverId, secondStart);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(first.Result!.Data!.ExecutionId, second.Result!.Data!.ExecutionId);
        Assert.Equal(first.Result.Data.RuleSetRevision, second.Result.Data.RuleSetRevision);
        Assert.Equal(first.Result.Data.Trigger.TriggerOccurrenceId, second.Result.Data.Trigger.TriggerOccurrenceId);
        Assert.Equal(first.Result.Data.FrozenRulesJson, second.Result.Data.FrozenRulesJson);

        const string logicalEffectId = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var firstClaim = await api.ClaimAction(serverId, first.Result!.Data!.ExecutionId,
            ClaimRequest(serverId, first.Result.Data, logicalEffectId));
        var secondClaim = await api.ClaimAction(serverId, second.Result!.Data!.ExecutionId,
            ClaimRequest(serverId, second.Result.Data, logicalEffectId));

        Assert.Equal(HttpStatusCode.OK, firstClaim.StatusCode);
        Assert.True(firstClaim.Result!.Data!.ShouldDispatch);
        Assert.Equal(HttpStatusCode.OK, secondClaim.StatusCode);
        Assert.False(secondClaim.Result!.Data!.ShouldDispatch);
        Assert.Equal(logicalEffectId, secondClaim.Result.Data.Action.LogicalEffectId);
        Assert.Single(await context.RuleActionExecutions.ToListAsync());
    }

    [Fact]
    public async Task ClaimAction_UsesPersistedIpContextForNextActionWithoutChangingOriginalTrigger()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var connectionId = Guid.NewGuid();
        var runtime = new RuleRuntimeDescriptor
        {
            Id = "plugin-host",
            Version = "1.0",
            IdentityModel = RuleRuntimeIdentityModel.PluginRuntimeGeneration,
            InstanceId = Guid.NewGuid(),
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            Capabilities = ["drop-player"]
        };
        var originalTrigger = new RuleTriggerEnvelope
        {
            ObservationId = Guid.NewGuid(),
            TriggerId = "player.connected",
            TriggerOccurrenceId = Guid.NewGuid(),
            OccurredAtUtc = DateTimeOffset.UtcNow.AddSeconds(-2),
            GameServerId = serverId,
            GameType = GameType.CallOfDuty4,
            ConnectionId = connectionId,
            ConnectionGeneration = 12,
            PlayerGuid = "PLAYER1",
            SlotId = 4,
            Runtime = runtime
        };
        const string frozenRulesJson = /*lang=json,strict*/ """{"rules":[{"id":"rule-1","actions":[{"id":"kick-player","typeId":"player.kick","parameters":{"reason":"test"}},{"id":"kick-player-2","typeId":"player.kick","parameters":{"reason":"test"}}]}]}""";
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var started = await api.StartExecution(serverId, new StartRuleFeatureExecutionDto
        {
            Trigger = originalTrigger,
            FeatureId = "vpn-protection",
            RuleSetRevision = "revision-one",
            FrozenRulesJson = frozenRulesJson
        });
        var execution = started.Result!.Data!;
        var contextTrigger = originalTrigger with
        {
            ObservationId = Guid.NewGuid(),
            TriggerId = "player.ip-resolved",
            TriggerOccurrenceId = Guid.NewGuid(),
            OccurredAtUtc = DateTimeOffset.UtcNow,
            PlayerIpAddress = "203.0.113.10",
            Runtime = runtime with { Capabilities = ["drop-player", "public-message"] }
        };
        var decisionSnapshotJson = JsonSerializer.Serialize(new
        {
            contextTrigger,
            contextObservedAtUtc = contextTrigger.OccurredAtUtc
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var updated = await api.UpdateProgress(serverId, execution.ExecutionId, new UpdateRuleFeatureExecutionProgressDto
        {
            ExpectedVersion = execution.Version,
            State = RuleFeatureExecutionState.Evaluating,
            DecisionSnapshotJson = decisionSnapshotJson
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var invalidContextTrigger = contextTrigger with { ConnectionGeneration = 11 };
        var invalidSnapshotJson = JsonSerializer.Serialize(new
        {
            contextTrigger = invalidContextTrigger,
            contextObservedAtUtc = invalidContextTrigger.OccurredAtUtc
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var invalidContext = await api.UpdateProgress(serverId, execution.ExecutionId,
            new UpdateRuleFeatureExecutionProgressDto
            {
                ExpectedVersion = updated.Result!.Data!.Version,
                State = RuleFeatureExecutionState.Evaluating,
                DecisionSnapshotJson = invalidSnapshotJson
            });
        Assert.Equal(HttpStatusCode.OK, invalidContext.StatusCode);
        const string firstEffectId = "8123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var rejectedClaim = await api.ClaimAction(serverId, execution.ExecutionId,
            ClaimRequestWithTrigger(serverId, invalidContext.Result!.Data!, firstEffectId, contextTrigger, "kick-player", 0));
        Assert.Equal(HttpStatusCode.BadRequest, rejectedClaim.StatusCode);
        var restoredContext = await api.UpdateProgress(serverId, execution.ExecutionId,
            new UpdateRuleFeatureExecutionProgressDto
            {
                ExpectedVersion = invalidContext.Result!.Data!.Version,
                State = RuleFeatureExecutionState.Evaluating,
                DecisionSnapshotJson = decisionSnapshotJson
            });
        Assert.Equal(HttpStatusCode.OK, restoredContext.StatusCode);

        var firstClaim = await api.ClaimAction(serverId, execution.ExecutionId,
            ClaimRequestWithTrigger(serverId, restoredContext.Result!.Data!, firstEffectId, contextTrigger, "kick-player", 0));
        Assert.Equal(HttpStatusCode.OK, firstClaim.StatusCode);
        Assert.True(firstClaim.Result!.Data!.ShouldDispatch);
        Assert.Equal(contextTrigger.TriggerOccurrenceId, firstClaim.Result.Data.Action.Correlation.TriggerOccurrenceId);
        Assert.Equal(originalTrigger.TriggerOccurrenceId, firstClaim.Result.Data.Execution.Trigger.TriggerOccurrenceId);
        Assert.Equal("player.kick", firstClaim.Result.Data.Action.ActionTypeId);
        Assert.Equal(/*lang=json,strict*/ """{"reason":"test"}""", firstClaim.Result.Data.Action.ParametersJson);

        var firstOutcome = new RuleActionResult
        {
            GameServerId = serverId,
            ExecutionId = execution.ExecutionId,
            ActionId = firstClaim.Result.Data.Action.Correlation.ActionId,
            LogicalEffectId = firstEffectId,
            Correlation = firstClaim.Result.Data.Action.Correlation,
            Outcome = RuleActionExecutionOutcome.Applied,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        var completedFirstAction = await api.UpdateActionOutcome(serverId, execution.ExecutionId,
            firstOutcome.ActionId, new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = firstClaim.Result.Data.Execution.Version,
                Result = firstOutcome,
                NextRuleIndex = 0,
                NextActionIndex = 1,
                NextState = RuleFeatureExecutionState.Evaluating,
                DecisionSnapshotJson = decisionSnapshotJson
            });
        Assert.Equal(HttpStatusCode.OK, completedFirstAction.StatusCode);

        const string secondEffectId = "9123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var secondClaim = await api.ClaimAction(serverId, execution.ExecutionId,
            ClaimRequestWithTrigger(serverId, completedFirstAction.Result!.Data!.Execution,
                secondEffectId, contextTrigger, "kick-player-2", 1));

        Assert.Equal(HttpStatusCode.OK, secondClaim.StatusCode);
        Assert.True(secondClaim.Result!.Data!.ShouldDispatch);
        Assert.Equal(contextTrigger.TriggerOccurrenceId, secondClaim.Result.Data.Action.Correlation.TriggerOccurrenceId);
        Assert.Equal("kick-player-2", secondClaim.Result.Data.Action.Command.ActionId);

        var repeatedFirstAction = await api.ClaimAction(serverId, execution.ExecutionId,
            ClaimRequestWithTrigger(serverId, secondClaim.Result.Data.Execution,
                firstEffectId, contextTrigger, "kick-player", 0));
        Assert.Equal(HttpStatusCode.OK, repeatedFirstAction.StatusCode);
        Assert.False(repeatedFirstAction.Result!.Data!.ShouldDispatch);
        Assert.Equal(RuleActionExecutionOutcome.Applied, repeatedFirstAction.Result.Data.Action.Outcome);
    }

    [Fact]
    public async Task UpdateProgressAndActionOutcome_RejectStaleVersionsAndInvalidOutcomeEchoes()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var started = await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), actionTypeId: "moderation.ban"));
        var execution = started.Result!.Data!;

        var staleProgress = await api.UpdateProgress(serverId, execution.ExecutionId, new UpdateRuleFeatureExecutionProgressDto
        {
            ExpectedVersion = execution.Version + 1,
            State = RuleFeatureExecutionState.Evaluating
        });
        Assert.Equal(HttpStatusCode.Conflict, staleProgress.StatusCode);

        const string logicalEffectId = "1123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var claimed = await api.ClaimAction(serverId, execution.ExecutionId, ClaimRequest(serverId, execution, logicalEffectId));
        Assert.Equal(HttpStatusCode.OK, claimed.StatusCode);
        var skippedPendingOutcome = await api.UpdateProgress(serverId, execution.ExecutionId, new UpdateRuleFeatureExecutionProgressDto
        {
            ExpectedVersion = claimed.Result!.Data!.Execution.Version,
            State = RuleFeatureExecutionState.Completed
        });
        Assert.Equal(HttpStatusCode.Conflict, skippedPendingOutcome.StatusCode);
        var action = claimed.Result!.Data!.Action;
        var correlation = action.Correlation;
        var result = new RuleActionResult
        {
            GameServerId = serverId,
            ExecutionId = execution.ExecutionId,
            ActionId = correlation.ActionId,
            LogicalEffectId = logicalEffectId,
            Correlation = correlation,
            Outcome = RuleActionExecutionOutcome.Dispatched,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        var dispatched = await api.UpdateActionOutcome(serverId, execution.ExecutionId, correlation.ActionId,
            new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = claimed.Result.Data.Execution.Version,
                Result = result,
                NextRuleIndex = 0,
                NextActionIndex = 0,
                NextState = RuleFeatureExecutionState.AwaitingActionOutcome
            });
        Assert.Equal(HttpStatusCode.OK, dispatched.StatusCode);
        var progressWhileDispatched = await api.UpdateProgress(serverId, execution.ExecutionId,
            new UpdateRuleFeatureExecutionProgressDto
            {
                ExpectedVersion = dispatched.Result!.Data!.Execution.Version,
                State = RuleFeatureExecutionState.Completed
            });
        Assert.Equal(HttpStatusCode.Conflict, progressWhileDispatched.StatusCode);

        var appliedResult = result with
        {
            Outcome = RuleActionExecutionOutcome.Applied,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        var update = new UpdateRuleActionOutcomeDto
        {
            ExpectedVersion = dispatched.Result.Data.Execution.Version,
            Result = appliedResult,
            NextRuleIndex = 1,
            NextActionIndex = 0,
            NextState = RuleFeatureExecutionState.Completed
        };

        var staleOutcome = await api.UpdateActionOutcome(serverId, execution.ExecutionId, correlation.ActionId,
            update with { ExpectedVersion = 1 });
        var invalidEcho = await api.UpdateActionOutcome(serverId, execution.ExecutionId, correlation.ActionId,
            update with { Result = appliedResult with { GameServerId = Guid.NewGuid() } });
        var applied = await api.UpdateActionOutcome(serverId, execution.ExecutionId, correlation.ActionId, update);

        Assert.Equal(HttpStatusCode.Conflict, staleOutcome.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidEcho.StatusCode);
        Assert.Equal(HttpStatusCode.OK, applied.StatusCode);
        Assert.Equal(RuleActionExecutionOutcome.Applied, applied.Result!.Data!.Action.Outcome);
        Assert.Equal(RuleFeatureExecutionState.Completed, applied.Result.Data.Execution.State);
    }

    [Fact]
    public async Task UpdateActionOutcome_AcceptsLateAppliedEvidenceWithoutReopeningExecution()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var started = await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), actionTypeId: "moderation.ban"));
        var execution = started.Result!.Data!;
        const string logicalEffectId = "3123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var claimed = await api.ClaimAction(serverId, execution.ExecutionId, ClaimRequest(serverId, execution, logicalEffectId));
        var action = claimed.Result!.Data!.Action;
        var dispatchedResult = new RuleActionResult
        {
            GameServerId = serverId,
            ExecutionId = execution.ExecutionId,
            ActionId = action.Correlation.ActionId,
            LogicalEffectId = logicalEffectId,
            Correlation = action.Correlation,
            Outcome = RuleActionExecutionOutcome.Dispatched,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        var dispatched = await api.UpdateActionOutcome(serverId, execution.ExecutionId, action.Correlation.ActionId,
            new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = claimed.Result.Data.Execution.Version,
                Result = dispatchedResult,
                NextRuleIndex = 0,
                NextActionIndex = 0,
                NextState = RuleFeatureExecutionState.AwaitingActionOutcome
            });
        Assert.Equal(HttpStatusCode.OK, dispatched.StatusCode);

        var unknownAt = DateTimeOffset.UtcNow;
        var timeoutResult = new RuleActionResult
        {
            GameServerId = serverId,
            ExecutionId = execution.ExecutionId,
            ActionId = action.Correlation.ActionId,
            LogicalEffectId = logicalEffectId,
            Correlation = action.Correlation,
            Outcome = RuleActionExecutionOutcome.OutcomeUnknown,
            OccurredAtUtc = unknownAt
        };
        var timedOut = await api.UpdateActionOutcome(serverId, execution.ExecutionId, action.Correlation.ActionId,
            new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = dispatched.Result!.Data!.Execution.Version,
                Result = timeoutResult,
                NextRuleIndex = 1,
                NextActionIndex = 0,
                NextState = RuleFeatureExecutionState.Completed
            });

        Assert.Equal(HttpStatusCode.OK, timedOut.StatusCode);

        var actionEntity = await context.RuleActionExecutions.SingleAsync();
        var command = JsonSerializer.Deserialize<RuleActionCommand>(
            actionEntity.CommandJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        actionEntity.ClaimedAtUtc = unknownAt.AddMinutes(-2).UtcDateTime;
        actionEntity.CommandJson = JsonSerializer.Serialize(command with
        {
            ExpiresAtUtc = unknownAt.AddMinutes(-1)
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await context.SaveChangesAsync();

        var tooLateResult = timeoutResult with
        {
            Outcome = RuleActionExecutionOutcome.Applied,
            OccurredAtUtc = unknownAt.AddSeconds(-29)
        };
        var tooLate = await api.UpdateActionOutcome(serverId, execution.ExecutionId, action.Correlation.ActionId,
            new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = dispatched.Result.Data.Execution.Version,
                Result = tooLateResult,
                NextRuleIndex = 0,
                NextActionIndex = 0,
                NextState = RuleFeatureExecutionState.Evaluating
            });

        Assert.Equal(HttpStatusCode.Conflict, tooLate.StatusCode);

        var lateResult = tooLateResult with { OccurredAtUtc = unknownAt.AddSeconds(-90) };
        var late = await api.UpdateActionOutcome(serverId, execution.ExecutionId, action.Correlation.ActionId,
            new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = dispatched.Result.Data.Execution.Version,
                Result = lateResult,
                NextRuleIndex = 0,
                NextActionIndex = 0,
                NextState = RuleFeatureExecutionState.Evaluating
            });

        Assert.Equal(HttpStatusCode.OK, late.StatusCode);
        Assert.False(late.Result!.Data!.ShouldDispatch);
        Assert.Equal(RuleActionExecutionOutcome.Applied, late.Result.Data.Action.Outcome);
        Assert.Equal(RuleFeatureExecutionState.Completed, late.Result.Data.Execution.State);
        Assert.Equal(timedOut.Result!.Data!.Execution.Version, late.Result.Data.Execution.Version);
        Assert.Equal(timedOut.Result.Data.Execution.RuleIndex, late.Result.Data.Execution.RuleIndex);
        Assert.Equal(timedOut.Result.Data.Execution.ActionIndex, late.Result.Data.Execution.ActionIndex);

        var repeatedClaim = await api.ClaimAction(serverId, execution.ExecutionId,
            ClaimRequest(serverId, execution, logicalEffectId));
        Assert.Equal(HttpStatusCode.OK, repeatedClaim.StatusCode);
        Assert.False(repeatedClaim.Result!.Data!.ShouldDispatch);
        Assert.Equal(RuleActionExecutionOutcome.Applied, repeatedClaim.Result.Data.Action.Outcome);
    }

    [Fact]
    public async Task UpdateActionOutcome_AdvancesDispatchedBanWithNativeEvidence()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Guid = "PLAYER1",
            FirstSeen = DateTime.UtcNow.AddDays(-1),
            LastSeen = DateTime.UtcNow
        });
        var adminActionId = Guid.NewGuid();
        context.AdminActions.Add(new AdminAction
        {
            AdminActionId = adminActionId,
            PlayerId = playerId,
            Type = (int)AdminActionType.Ban,
            Text = "Rule-engine ban",
            Created = DateTime.UtcNow,
            Source = (byte)ActionSource.Automation,
            AutomationFeature = (int)AutomationFeature.VpnProtection,
            AutomationRuleId = "rule-1"
        });
        await context.SaveChangesAsync();

        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var started = await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), actionTypeId: "moderation.ban"));
        var execution = started.Result!.Data!;
        const string logicalEffectId = "4123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var claimed = await api.ClaimAction(serverId, execution.ExecutionId, ClaimRequest(serverId, execution, logicalEffectId));
        var action = claimed.Result!.Data!.Action;
        var dispatchedResult = new RuleActionResult
        {
            GameServerId = serverId,
            ExecutionId = execution.ExecutionId,
            ActionId = action.Correlation.ActionId,
            LogicalEffectId = logicalEffectId,
            Correlation = action.Correlation,
            Outcome = RuleActionExecutionOutcome.Dispatched,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        var dispatched = await api.UpdateActionOutcome(serverId, execution.ExecutionId, action.Correlation.ActionId,
            new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = claimed.Result.Data.Execution.Version,
                Result = dispatchedResult,
                AdminActionId = adminActionId,
                NextRuleIndex = 0,
                NextActionIndex = 0,
                NextState = RuleFeatureExecutionState.AwaitingActionOutcome
            });
        Assert.Equal(HttpStatusCode.OK, dispatched.StatusCode);

        var unknownAt = DateTimeOffset.UtcNow;
        var unknownResult = dispatchedResult with
        {
            Outcome = RuleActionExecutionOutcome.OutcomeUnknown,
            OccurredAtUtc = unknownAt
        };
        var nativeEvidence = unknownResult with
        {
            Outcome = RuleActionExecutionOutcome.Applied,
            OccurredAtUtc = unknownAt.AddSeconds(-20)
        };
        var completed = await api.UpdateActionOutcome(serverId, execution.ExecutionId, action.Correlation.ActionId,
            new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = dispatched.Result!.Data!.Execution.Version,
                Result = unknownResult,
                AdminActionId = adminActionId,
                NativeEvidence = nativeEvidence,
                NextRuleIndex = 1,
                NextActionIndex = 0,
                NextState = RuleFeatureExecutionState.Completed
            });

        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal(RuleActionExecutionOutcome.OutcomeUnknown, completed.Result!.Data!.Action.Outcome);
        Assert.Equal(RuleActionExecutionOutcome.Applied, completed.Result.Data.Action.NativeEvidence!.Outcome);
        Assert.Equal(RuleFeatureExecutionState.Completed, completed.Result.Data.Execution.State);
        Assert.Equal(dispatched.Result.Data.Execution.Version + 1, completed.Result.Data.Execution.Version);
    }

    [Fact]
    public async Task UpdateActionOutcome_StoresNativeEvidenceWithoutCompletingLinkedBan()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Guid = "PLAYER1",
            FirstSeen = DateTime.UtcNow.AddDays(-1),
            LastSeen = DateTime.UtcNow
        });
        var adminActionId = Guid.NewGuid();
        context.AdminActions.Add(new AdminAction
        {
            AdminActionId = adminActionId,
            PlayerId = playerId,
            Type = (int)AdminActionType.Ban,
            Text = "Rule-engine ban",
            Created = DateTime.UtcNow,
            Source = (byte)ActionSource.Automation,
            AutomationFeature = (int)AutomationFeature.VpnProtection,
            AutomationRuleId = "rule-1"
        });
        await context.SaveChangesAsync();

        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var started = await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), actionTypeId: "moderation.ban"));
        var execution = started.Result!.Data!;
        const string logicalEffectId = "4123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var claimed = await api.ClaimAction(serverId, execution.ExecutionId, ClaimRequest(serverId, execution, logicalEffectId));
        var action = claimed.Result!.Data!.Action;
        var unknownAt = DateTimeOffset.UtcNow;
        var unknownResult = new RuleActionResult
        {
            GameServerId = serverId,
            ExecutionId = execution.ExecutionId,
            ActionId = action.Correlation.ActionId,
            LogicalEffectId = logicalEffectId,
            Correlation = action.Correlation,
            Outcome = RuleActionExecutionOutcome.OutcomeUnknown,
            OccurredAtUtc = unknownAt
        };
        var timedOut = await api.UpdateActionOutcome(serverId, execution.ExecutionId, action.Correlation.ActionId,
            new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = claimed.Result.Data.Execution.Version,
                Result = unknownResult,
                AdminActionId = adminActionId,
                NextRuleIndex = 1,
                NextActionIndex = 0,
                NextState = RuleFeatureExecutionState.Completed
            });

        Assert.Equal(HttpStatusCode.OK, timedOut.StatusCode);
        Assert.Equal(RuleActionExecutionOutcome.OutcomeUnknown, timedOut.Result!.Data!.Action.Outcome);
        Assert.Equal(RuleFeatureExecutionState.Completed, timedOut.Result.Data.Execution.State);

        var actionEntity = await context.RuleActionExecutions.SingleAsync();
        var command = JsonSerializer.Deserialize<RuleActionCommand>(
            actionEntity.CommandJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        actionEntity.ClaimedAtUtc = unknownAt.AddMinutes(-2).UtcDateTime;
        actionEntity.CommandJson = JsonSerializer.Serialize(command with
        {
            ExpiresAtUtc = unknownAt.AddMinutes(-1)
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await context.SaveChangesAsync();

        var appliedNativeResult = unknownResult with
        {
            Outcome = RuleActionExecutionOutcome.Applied,
            OccurredAtUtc = unknownAt.AddSeconds(-90)
        };
        var lateEvidence = await api.UpdateActionOutcome(serverId, execution.ExecutionId, action.Correlation.ActionId,
            new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = claimed.Result.Data.Execution.Version,
                Result = unknownResult,
                AdminActionId = adminActionId,
                NativeEvidence = appliedNativeResult,
                NextRuleIndex = 0,
                NextActionIndex = 0,
                NextState = RuleFeatureExecutionState.Evaluating
            });

        Assert.Equal(HttpStatusCode.OK, lateEvidence.StatusCode);
        Assert.False(lateEvidence.Result!.Data!.ShouldDispatch);
        Assert.Equal(RuleActionExecutionOutcome.OutcomeUnknown, lateEvidence.Result.Data.Action.Outcome);
        Assert.Equal(adminActionId, lateEvidence.Result.Data.Action.AdminActionId);
        Assert.Equal(RuleActionExecutionOutcome.Applied, lateEvidence.Result.Data.Action.NativeEvidence!.Outcome);
        Assert.Equal(RuleFeatureExecutionState.Completed, lateEvidence.Result.Data.Execution.State);
        Assert.Equal(timedOut.Result!.Data!.Execution.Version, lateEvidence.Result.Data.Execution.Version);
        Assert.Equal(timedOut.Result.Data.Execution.RuleIndex, lateEvidence.Result.Data.Execution.RuleIndex);

        var hostConfirmedResult = unknownResult with
        {
            Outcome = RuleActionExecutionOutcome.Applied,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        var hostConfirmed = await api.UpdateActionOutcome(serverId, execution.ExecutionId, action.Correlation.ActionId,
            new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = claimed.Result.Data.Execution.Version,
                Result = hostConfirmedResult,
                AdminActionId = adminActionId,
                NextRuleIndex = 0,
                NextActionIndex = 0,
                NextState = RuleFeatureExecutionState.Evaluating
            });

        Assert.Equal(HttpStatusCode.OK, hostConfirmed.StatusCode);
        Assert.Equal(RuleActionExecutionOutcome.Applied, hostConfirmed.Result!.Data!.Action.Outcome);
        Assert.Equal(RuleActionExecutionOutcome.Applied, hostConfirmed.Result.Data.Action.NativeEvidence!.Outcome);
        Assert.Equal(RuleFeatureExecutionState.Completed, hostConfirmed.Result.Data.Execution.State);
        Assert.Equal(timedOut.Result.Data.Execution.Version, hostConfirmed.Result.Data.Execution.Version);
    }

    [Fact]
    public async Task ClaimAction_DryRunNeverReturnsDispatchPermissionAndRequiresWouldExecuteOutcome()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var started = await api.StartExecution(serverId, StartRequest(serverId, Guid.NewGuid()));
        var execution = started.Result!.Data!;
        const string logicalEffectId = "2123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var request = ClaimRequest(serverId, execution, logicalEffectId);
        request = request with { Command = request.Command with { Mode = RuleExecutionMode.DryRun } };

        var claimed = await api.ClaimAction(serverId, execution.ExecutionId, request);
        var correlation = claimed.Result!.Data!.Action.Correlation;
        var result = new RuleActionResult
        {
            GameServerId = serverId,
            ExecutionId = execution.ExecutionId,
            ActionId = correlation.ActionId,
            LogicalEffectId = logicalEffectId,
            Correlation = correlation,
            Outcome = RuleActionExecutionOutcome.Applied,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        var unsafeOutcome = await api.UpdateActionOutcome(serverId, execution.ExecutionId, correlation.ActionId,
            new UpdateRuleActionOutcomeDto
            {
                ExpectedVersion = claimed.Result.Data.Execution.Version,
                Result = result,
                NextRuleIndex = 0,
                NextActionIndex = 1,
                NextState = RuleFeatureExecutionState.Completed
            });

        Assert.False(claimed.Result.Data.ShouldDispatch);
        Assert.Equal(HttpStatusCode.BadRequest, unsafeOutcome.StatusCode);
    }

    [Fact]
    public async Task CreateFactSnapshot_ExcludesCurrentObservationAndKeepsFirstSnapshotImmutable()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var oldSeenAt = DateTime.UtcNow.AddDays(-10);
        var observedAt = DateTime.UtcNow;
        var playerId = Guid.NewGuid();
        context.Players.AddRange(
            new Player { PlayerId = playerId, GameType = (int)GameType.CallOfDuty4, Guid = "AbC123", FirstSeen = oldSeenAt, LastSeen = oldSeenAt },
            new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4, Guid = "ABC123", FirstSeen = observedAt.AddMilliseconds(-2), LastSeen = observedAt });
        context.PlayerIpAddresses.AddRange(
            new PlayerIpAddress
            {
                PlayerIpAddressId = Guid.NewGuid(),
                PlayerId = playerId,
                Address = "::ffff:203.0.113.1",
                Added = oldSeenAt,
                LastUsed = oldSeenAt,
                ConfidenceScore = 100
            },
            new PlayerIpAddress
            {
                PlayerIpAddressId = Guid.NewGuid(),
                PlayerId = playerId,
                Address = "203.0.113.1",
                Added = observedAt.AddMilliseconds(-2),
                LastUsed = observedAt,
                ConfidenceScore = 100
            });
        await context.SaveChangesAsync();

        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var request = FactRequest(serverId, Guid.NewGuid(), observedAt, "ABC123", "203.0.113.1");
        var first = await api.CreateFactSnapshot(serverId, request);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(oldSeenAt, first.Result!.Data!.GuidFirstSeenAtUtc!.Value.UtcDateTime);
        Assert.Equal(oldSeenAt, first.Result.Data.IpFirstSeenAtUtc!.Value.UtcDateTime);

        context.Players.Add(new Player
        {
            PlayerId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Guid = "ABC123",
            FirstSeen = oldSeenAt.AddDays(-2),
            LastSeen = oldSeenAt
        });
        await context.SaveChangesAsync();
        var retry = await api.CreateFactSnapshot(serverId, request with { ObservationId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(first.Result.Data.SnapshotId, retry.Result!.Data!.SnapshotId);
        Assert.Equal(oldSeenAt, retry.Result.Data.GuidFirstSeenAtUtc!.Value.UtcDateTime);
        Assert.Equal(oldSeenAt, retry.Result.Data.IpFirstSeenAtUtc!.Value.UtcDateTime);
    }

    [Fact]
    public async Task CreateFactSnapshot_EnrichesIpOnceAndReturnsTimestampedSiegeBaselineFacts()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var sessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var baselineAt = now.AddDays(-30);
        context.SiegeSessions.Add(new SiegeSession
        {
            SessionId = sessionId,
            GameServerId = serverId,
            GameType = (int)GameType.CallOfDuty4,
            StartedAtUtc = now.AddMinutes(-5),
            ExpiresAtUtc = now.AddHours(1),
            StartedBy = "test",
            Mode = "Enforce",
            SettingsJson = "{}",
            BaselineRevision = 1
        });
        context.SiegeBaselineGuids.Add(new SiegeBaselineGuid
        {
            SessionId = sessionId,
            PlayerGuid = "baseline-guid",
            FirstSeenAtUtc = baselineAt
        });
        context.SiegeBaselineIps.Add(new SiegeBaselineIp
        {
            SessionId = sessionId,
            Address = "203.0.113.2",
            FirstSeenAtUtc = baselineAt,
            HasUnownedSource = false
        });
        await context.SaveChangesAsync();
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var connectionId = Guid.NewGuid();
        var initial = await api.CreateFactSnapshot(serverId, FactRequest(serverId, connectionId, now, "baseline-guid"));
        context.PlayerIpAddresses.Add(new PlayerIpAddress
        {
            PlayerIpAddressId = Guid.NewGuid(),
            Address = "203.0.113.2",
            Added = baselineAt,
            LastUsed = baselineAt,
            ConfidenceScore = 100
        });
        await context.SaveChangesAsync();

        var enriched = await api.CreateFactSnapshot(serverId,
            FactRequest(serverId, connectionId, now, "different-guid", "203.0.113.2", sessionId));
        var secondIp = await api.CreateFactSnapshot(serverId,
            FactRequest(serverId, connectionId, now, "different-guid", "198.51.100.10", sessionId));

        Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        Assert.Equal(HttpStatusCode.OK, enriched.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondIp.StatusCode);
        Assert.Equal(baselineAt, enriched.Result!.Data!.GuidBaselineFirstSeenAtUtc!.Value.UtcDateTime);
        Assert.Equal(baselineAt, enriched.Result.Data.IpBaselineFirstSeenAtUtc!.Value.UtcDateTime);
        Assert.True(enriched.Result.Data.GuidBaselineAvailable);
        Assert.True(enriched.Result.Data.IpBaselineAvailable);
        Assert.Equal("203.0.113.2", secondIp.Result!.Data!.PlayerIpAddress);
        Assert.Equal(baselineAt, secondIp.Result.Data.IpFirstSeenAtUtc!.Value.UtcDateTime);
    }

    private static StartRuleFeatureExecutionDto StartRequest(
        Guid serverId,
        Guid connectionId,
        string triggerId = "player.connected",
        Guid? occurrenceId = null,
        string revision = "revision",
        string actionTypeId = "player.kick",
        string parametersJson = /*lang=json,strict*/ """{"reason":"test"}""",
        RuleRuntimeDescriptor? runtime = null) =>
        new()
        {
            Trigger = new RuleTriggerEnvelope
            {
                ObservationId = Guid.NewGuid(),
                TriggerId = triggerId,
                TriggerOccurrenceId = occurrenceId ?? Guid.NewGuid(),
                OccurredAtUtc = DateTimeOffset.UtcNow,
                GameServerId = serverId,
                GameType = GameType.CallOfDuty4,
                ConnectionId = connectionId,
                PlayerGuid = "PLAYER1",
                Runtime = runtime ?? new RuleRuntimeDescriptor
                {
                    Id = "server-agent",
                    Version = "1.0",
                    IdentityModel = RuleRuntimeIdentityModel.AgentObservedConnection,
                    InstanceId = Guid.NewGuid(),
                    StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
                }
            },
            FeatureId = "vpn-protection",
            RuleSetRevision = revision,
            FrozenRulesJson = $$"""{"rules":[{"id":"rule-1","actions":[{"id":"kick-player","typeId":"{{actionTypeId}}","parameters":{{parametersJson}}}]}]}"""
        };

    private static ClaimRuleActionDto ClaimRequest(
        Guid serverId,
        RuleFeatureExecutionDto execution,
        string logicalEffectId,
        RuleNativeEffectType effectType = RuleNativeEffectType.DropPlayer)
    {
        var trigger = execution.Trigger;
        var actionId = "kick-player";
        var correlation = new RuleActionCorrelation
        {
            TriggerId = trigger.TriggerId,
            TriggerOccurrenceId = trigger.TriggerOccurrenceId,
            ConnectionId = trigger.ConnectionId,
            FeatureId = execution.FeatureId,
            RuleId = "rule-1",
            RuleSetRevision = execution.RuleSetRevision,
            ActionId = actionId
        };
        return new ClaimRuleActionDto
        {
            ExpectedVersion = execution.Version,
            RuleIndex = 0,
            ActionIndex = 0,
            LogicalEffectId = logicalEffectId,
            Command = new RuleActionCommand
            {
                ExecutionId = execution.ExecutionId,
                GameServerId = serverId,
                ActionId = actionId,
                LogicalEffectId = logicalEffectId,
                Correlation = correlation,
                ConnectionId = trigger.ConnectionId,
                PlayerGuid = trigger.PlayerGuid ?? "PLAYER1",
                SlotId = trigger.SlotId ?? 0,
                ConnectionGeneration = trigger.ConnectionGeneration,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(1),
                Runtime = trigger.Runtime!,
                Mode = RuleExecutionMode.Enforce,
                Effect = new RuleNativeEffect { Type = effectType, Reason = effectType == RuleNativeEffectType.DropPlayer ? "test" : null }
            }
        };
    }

    private static ClaimRuleActionDto ClaimRequestWithTrigger(
        Guid serverId,
        RuleFeatureExecutionDto execution,
        string logicalEffectId,
        RuleTriggerEnvelope trigger,
        string actionId,
        int actionIndex)
    {
        var request = ClaimRequest(serverId, execution, logicalEffectId);
        var correlation = request.Command.Correlation with
        {
            TriggerId = trigger.TriggerId,
            TriggerOccurrenceId = trigger.TriggerOccurrenceId,
            ActionId = actionId
        };
        return request with
        {
            ActionIndex = actionIndex,
            Command = request.Command with
            {
                ActionId = actionId,
                LogicalEffectId = logicalEffectId,
                Correlation = correlation,
                ConnectionId = trigger.ConnectionId,
                ConnectionGeneration = trigger.ConnectionGeneration,
                PlayerGuid = trigger.PlayerGuid ?? string.Empty,
                SlotId = trigger.SlotId ?? 0,
                Runtime = trigger.Runtime!
            }
        };
    }

    private static RuleFactSnapshotRequestDto FactRequest(
        Guid serverId, Guid connectionId, DateTime observedAt, string guid, string? ip = null, Guid? sessionId = null) =>
        new()
        {
            ObservationId = Guid.NewGuid(),
            ConnectionId = connectionId,
            SiegeSessionId = sessionId,
            GameType = GameType.CallOfDuty4,
            PlayerGuid = guid,
            PlayerIpAddress = ip,
            ObservedAtUtc = observedAt
        };

    private static async Task<Guid> AddServer(PortalDbContext context)
    {
        var id = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = id,
            Title = "Rules persistence test server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960,
            Deleted = false
        });
        await context.SaveChangesAsync();
        return id;
    }

    private static SiegeSession CreateSiegeSession(Guid gameServerId, Guid sessionId) =>
        new()
        {
            SessionId = sessionId,
            GameServerId = gameServerId,
            GameType = (int)GameType.CallOfDuty4,
            StartedAtUtc = DateTime.UtcNow.AddHours(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            StartedBy = "rules-audit-test",
            Mode = "DryRun",
            SettingsJson = "{}",
            BaselineRevision = 0
        };

    private static (RuleFeatureExecution Execution, RuleActionExecution Action) CreateAuditAction(
        Guid gameServerId,
        string featureId,
        Guid? siegeSessionId,
        DateTime claimedAtUtc,
        RuleExecutionMode mode)
    {
        var executionId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var actionId = "action-audit";
        var logicalEffectId = Guid.NewGuid().ToString("N").PadRight(64, 'a');
        var trigger = new RuleTriggerEnvelope
        {
            TriggerId = "player.connected",
            TriggerOccurrenceId = Guid.NewGuid(),
            GameServerId = gameServerId,
            GameType = GameType.CallOfDuty4,
            ConnectionId = connectionId,
            PlayerGuid = "PLAYER-AUDIT",
            SlotId = 5
        };
        var correlation = new RuleActionCorrelation
        {
            TriggerId = trigger.TriggerId,
            TriggerOccurrenceId = trigger.TriggerOccurrenceId,
            ConnectionId = connectionId,
            FeatureId = featureId,
            RuleId = "rule-audit",
            RuleSetRevision = "revision-audit",
            SiegeSessionId = siegeSessionId,
            ActionId = actionId
        };
        var command = new RuleActionCommand
        {
            ExecutionId = executionId,
            ActionId = actionId,
            LogicalEffectId = logicalEffectId,
            Correlation = correlation,
            GameServerId = gameServerId,
            SlotId = 5,
            PlayerGuid = "PLAYER-AUDIT",
            ConnectionId = connectionId,
            ConnectionGeneration = 7,
            Mode = mode
        };
        var occurredAtUtc = new DateTimeOffset(claimedAtUtc.AddSeconds(5), TimeSpan.Zero);
        var result = new RuleActionResult
        {
            GameServerId = gameServerId,
            ExecutionId = executionId,
            ActionId = actionId,
            LogicalEffectId = logicalEffectId,
            Correlation = correlation,
            Outcome = RuleActionExecutionOutcome.WouldExecute,
            OccurredAtUtc = occurredAtUtc
        };
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var execution = new RuleFeatureExecution
        {
            ExecutionId = executionId,
            GameServerId = gameServerId,
            ConnectionId = connectionId,
            TriggerOccurrenceId = trigger.TriggerOccurrenceId,
            FeatureId = featureId,
            SiegeSessionId = siegeSessionId,
            RuleSetRevision = "revision-audit",
            FrozenRulesJson = "{}",
            TriggerJson = JsonSerializer.Serialize(trigger, jsonOptions),
            State = RuleFeatureExecutionState.Completed.ToString(),
            CreatedAtUtc = claimedAtUtc,
            UpdatedAtUtc = claimedAtUtc.AddMinutes(1)
        };
        var action = new RuleActionExecution
        {
            ActionExecutionId = Guid.NewGuid(),
            ExecutionId = executionId,
            Execution = execution,
            GameServerId = gameServerId,
            SiegeSessionId = siegeSessionId,
            LogicalEffectId = logicalEffectId,
            ActionId = actionId,
            ActionTypeId = "moderation.observation",
            ParametersJson = "{}",
            CorrelationJson = JsonSerializer.Serialize(correlation, jsonOptions),
            CommandJson = JsonSerializer.Serialize(command, jsonOptions),
            Mode = mode.ToString(),
            Outcome = RuleActionExecutionOutcome.WouldExecute.ToString(),
            RuleIndex = 0,
            ActionIndex = 1,
            ClaimedAtUtc = claimedAtUtc,
            OutcomeAtUtc = occurredAtUtc.UtcDateTime,
            UpdatedAtUtc = claimedAtUtc.AddMinutes(1),
            ResultJson = JsonSerializer.Serialize(result, jsonOptions)
        };
        return (execution, action);
    }
}
