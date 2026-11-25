using System.Net;
using System.Text.Json;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed partial class RuleExecutionsControllerTests
{
    [Fact]
    public async Task PreparedFragmentsFreezeWinnerResumeIndependentlyAndEnforceOrder()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), actionTypeId: "message.private", parametersJson: """{"messages":["first","second"]}""")))
            .Result!.Data!;
        var first = DeliveryCommand(serverId, execution, 0, "first");
        var second = DeliveryCommand(serverId, execution, 1, "second");
        var prepare = new PrepareRuleActionDto
        {
            ExpectedVersion = execution.Version,
            ResolvedParametersJson = """{"messages":["first","second"]}""",
            Commands = [first, second]
        };
        var prepared = await api.PrepareAction(serverId, execution.ExecutionId, prepare);
        Assert.Equal(HttpStatusCode.Created, prepared.StatusCode);
        var loser = await api.PrepareAction(serverId, execution.ExecutionId, prepare with
        {
            Commands = [first with { Effect = first.Effect with { Message = "changed" } }]
        });
        Assert.Equal("first", loser.Result!.Data!.Commands[0].Effect.Message);

        var outOfOrder = await api.ClaimAction(serverId, execution.ExecutionId, ChildClaim(execution, second));
        Assert.Equal(HttpStatusCode.Conflict, outOfOrder.StatusCode);
        var claimed = await api.ClaimAction(serverId, execution.ExecutionId, ChildClaim(execution, first));
        Assert.True(claimed.Result!.Data!.ShouldDispatch);
        var outcome = await api.UpdateActionOutcome(serverId, execution.ExecutionId, first.ActionId, new()
        {
            ExpectedVersion = claimed.Result.Data.Execution.Version,
            Result = ChildResult(first, RuleActionExecutionOutcome.Applied),
            NextState = RuleFeatureExecutionState.Evaluating
        });
        Assert.Equal(HttpStatusCode.OK, outcome.StatusCode);
        execution = outcome.Result!.Data!.Execution;
        var replay = await api.ClaimAction(serverId, execution.ExecutionId, ChildClaim(execution, first));
        Assert.False(replay.Result!.Data!.ShouldDispatch);
        var next = await api.ClaimAction(serverId, execution.ExecutionId, ChildClaim(execution, second));
        Assert.True(next.Result!.Data!.ShouldDispatch);
        var loaded = (await api.GetExecution(serverId, execution.ExecutionId)).Result!.Data!;
        Assert.Equal(1, Assert.Single(loaded.PreparedActions).CompletedFragmentCount);
        Assert.Equal(2, loaded.Actions.Length);
    }

    [Fact]
    public async Task PreparedPlanRejectsChangedUnapprovedParametersAndRecipientOnModeration()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), actionTypeId: "message.private", parametersJson: """{"message":"literal"}""")))
            .Result!.Data!;
        var command = DeliveryCommand(serverId, execution, 0, "literal");
        var invalid = await api.PrepareAction(serverId, execution.ExecutionId, new()
        {
            ExpectedVersion = execution.Version,
            ResolvedParametersJson = """{"message":"changed"}""",
            Commands = [command]
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var kick = (await api.StartExecution(serverId, StartRequest(serverId, Guid.NewGuid()))).Result!.Data!;
        var claim = ClaimRequest(serverId, kick, new string('b', 64));
        claim = claim with
        {
            Command = claim.Command with
            {
                RecipientTarget = new()
                {
                    GameServerId = serverId,
                    Runtime = kick.Trigger.Runtime!,
                    PlayerGuid = "OTHER",
                    ConnectionId = Guid.NewGuid(),
                    SlotId = 2
                }
            }
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ClaimAction(serverId, kick.ExecutionId, claim)).StatusCode);
    }

    [Fact]
    public async Task TaggedEmptyAudienceIsSuccessOnlyWhenCompleteAndAvailable()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId, StartRequest(serverId, Guid.NewGuid(),
            actionTypeId: "message.private-tagged", parametersJson: """{"message":"alert"}"""))).Result!.Data!;
        var request = new PrepareRuleActionDto
        {
            ExpectedVersion = execution.Version,
            ResolvedParametersJson = """{"message":"alert"}""",
            AudienceSnapshot = new() { GameServerId = serverId, Available = true, Partial = true }
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await api.PrepareAction(serverId, execution.ExecutionId, request)).StatusCode);
        var accepted = await api.PrepareAction(serverId, execution.ExecutionId, request with
        {
            AudienceSnapshot = request.AudienceSnapshot with { Partial = false }
        });
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        var plan = Assert.Single((await api.GetExecution(serverId, execution.ExecutionId)).Result!.Data!.PreparedActions);
        Assert.Equal(RuleActionExecutionOutcome.Applied, plan.Outcome);
        Assert.Equal("No recipients.", plan.Detail);
        Assert.Equal(0, plan.TotalFragmentCount);
    }

    [Fact]
    public async Task CoordinationIsImmutableAndProtectiveEnforcementBlocksOnlyTriggerMessages()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var connectionId = Guid.NewGuid();
        var start = StartRequest(serverId, connectionId);
        var protection = (await api.StartExecution(serverId, start)).Result!.Data!;
        var enforcement = await api.ClaimAction(serverId, protection.ExecutionId, ClaimRequest(serverId, protection, new string('d', 64)));
        Assert.Equal(HttpStatusCode.Created, enforcement.StatusCode);
        var welcomeStart = start with
        {
            FeatureId = "welcome-messages",
            FrozenRulesJson = """{"rules":[{"id":"rule-1","actions":[{"id":"kick-player","typeId":"message.private","parameters":{"message":"hello"}}]}]}""",
            Coordination = new()
            {
                DueAtUtc = start.Trigger.OccurredAtUtc,
                ExpiresAtUtc = start.Trigger.OccurredAtUtc.AddSeconds(120),
                PredecessorExecutionIds = [protection.ExecutionId]
            }
        };
        var welcome = (await api.StartExecution(serverId, welcomeStart)).Result!.Data!;
        var changed = await api.UpdateProgress(serverId, welcome.ExecutionId, new()
        {
            ExpectedVersion = welcome.Version,
            State = RuleFeatureExecutionState.Waiting,
            Coordination = welcome.Coordination! with { DueAtUtc = welcome.Coordination.DueAtUtc.AddSeconds(1) }
        });
        Assert.Equal(HttpStatusCode.BadRequest, changed.StatusCode);
        var command = ClaimRequest(serverId, welcome, new string('e', 64), RuleNativeEffectType.PrivateMessage);
        Assert.Equal(HttpStatusCode.Conflict, (await api.ClaimAction(serverId, welcome.ExecutionId, command)).StatusCode);
        Assert.Equal(2, (await api.GetConnectionExecutions(serverId, connectionId)).Result!.Data!.Length);
    }

    [Fact]
    public async Task ResolvedReasonIsFrozenAndRequiredForTemplatedModerationRecord()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var start = StartRequest(serverId, Guid.NewGuid(), actionTypeId: "moderation.observation");
        start = start with
        {
            FrozenRulesJson = """{"rules":[{"id":"rule-1","actions":[{"id":"kick-player","typeId":"moderation.observation","parameters":{"reason":"Hello {context.player.Name}"},"templateParameters":["reason"]}]}]}"""
        };
        var execution = (await api.StartExecution(serverId, start)).Result!.Data!;
        var request = ClaimRequest(serverId, execution, new string('f', 64), RuleNativeEffectType.Unknown);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ClaimAction(serverId, execution.ExecutionId, request)).StatusCode);
        request = request with { ResolvedParametersJson = """{"reason":"Hello Player"}""" };
        var claim = await api.ClaimAction(serverId, execution.ExecutionId, request);
        Assert.Equal(HttpStatusCode.Created, claim.StatusCode);
        Assert.Equal(request.ResolvedParametersJson, claim.Result!.Data!.Action.ParametersJson);
        var mismatch = await api.EnsureRuleActionAdminAction(serverId, execution.ExecutionId, request.Command.ActionId, new()
        {
            ExpectedVersion = claim.Result.Data.Execution.Version,
            LogicalEffectId = request.LogicalEffectId,
            Type = AdminActionType.Observation,
            Text = "Hello {context.player.Name}"
        });
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
    }

    [Fact]
    public async Task TaggedAudienceReportsMissingOrPartialRosterRatherThanEmptySuccess()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var request = new RuleTaggedAudienceRequestDto { TagIds = [Guid.NewGuid()] };
        Assert.False((await api.GetTaggedAudience(serverId, request)).Result!.Data!.Available);
        var snapshot = new RuleConnectionStatusSnapshotDto
        {
            GameServerId = serverId,
            SnapshotId = Guid.NewGuid(),
            ObservedAtUtc = DateTimeOffset.UtcNow,
            Players = [new() { State = RuleConnectionState.Unknown }]
        };
        context.RuleConnectionStatusSnapshots.Add(new()
        {
            GameServerId = serverId,
            SnapshotId = snapshot.SnapshotId,
            SnapshotJson = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(1),
            IntegrationCapabilitiesJson = "[]",
            RuntimeCapabilitiesJson = "[]",
            RuntimeId = "agent",
            RuntimeVersion = "1"
        });
        await context.SaveChangesAsync();
        var partial = (await api.GetTaggedAudience(serverId, request)).Result!.Data!;
        Assert.True(partial.Available);
        Assert.True(partial.Partial);
    }

    private static RuleActionCommand DeliveryCommand(Guid serverId, RuleFeatureExecutionDto execution, int index, string text)
    {
        var request = ClaimRequest(serverId, execution, new string('a', 64), RuleNativeEffectType.PrivateMessage);
        var identity = new RuleDeliveryIdentity { ParentLogicalEffectId = new string('a', 64), FragmentIndex = index };
        return request.Command with
        {
            LogicalEffectId = identity.ToLogicalEffectId(),
            DeliveryIdentity = identity,
            Effect = new() { Type = RuleNativeEffectType.PrivateMessage, Message = text }
        };
    }

    private static ClaimRuleActionDto ChildClaim(RuleFeatureExecutionDto execution, RuleActionCommand command) => new()
    {
        ExpectedVersion = execution.Version,
        LogicalEffectId = command.LogicalEffectId,
        Command = command
    };

    private static RuleActionResult ChildResult(RuleActionCommand command, RuleActionExecutionOutcome outcome) => new()
    {
        GameServerId = command.GameServerId,
        ExecutionId = command.ExecutionId,
        ActionId = command.ActionId,
        LogicalEffectId = command.LogicalEffectId,
        Correlation = command.Correlation,
        Outcome = outcome,
        OccurredAtUtc = DateTimeOffset.UtcNow
    };
}
