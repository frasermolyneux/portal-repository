using System.Net;
using System.Text.Json;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed partial class RuleExecutionsControllerTests
{
    [Fact]
    public async Task PreparedWelcomeUsesFrozenRuleWindowRatherThanExecutionDefaultWindow()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var start = StartRequest(serverId, Guid.NewGuid(), actionTypeId: "message.private", parametersJson: /*lang=json,strict*/ """{"message":"hello"}""");
        start = start with
        {
            FeatureId = "welcome-messages",
            FrozenRulesJson = /*lang=json,strict*/ """
                {"timing":{"connectionDelaySeconds":5,"freshnessSeconds":120},"rules":[
                {"id":"rule-1","timing":{"connectionDelaySeconds":0,"freshnessSeconds":180},"actions":[
                {"id":"kick-player","typeId":"message.private","parameters":{"message":"hello"}}]}]}
                """,
            Coordination = new()
            {
                DueAtUtc = start.Trigger.OccurredAtUtc.AddSeconds(5),
                ExpiresAtUtc = start.Trigger.OccurredAtUtc.AddSeconds(120)
            }
        };
        var execution = (await api.StartExecution(serverId, start)).Result!.Data!;
        var command = DeliveryCommand(serverId, execution, 0, "hello") with
        {
            ExpiresAtUtc = start.Trigger.OccurredAtUtc.AddSeconds(170)
        };
        var prepared = await api.PrepareAction(serverId, execution.ExecutionId, new()
        {
            ExpectedVersion = execution.Version,
            ResolvedParametersJson = /*lang=json,strict*/ """{"message":"hello"}""",
            Commands = [command]
        });
        Assert.Equal(HttpStatusCode.Created, prepared.StatusCode);
        Assert.Equal(start.Trigger.OccurredAtUtc, prepared.Result!.Data!.RuleCoordination!.DueAtUtc);
        Assert.Equal(start.Trigger.OccurredAtUtc.AddSeconds(180), prepared.Result.Data.RuleCoordination.ExpiresAtUtc);
    }

    [Fact]
    public async Task TaggedAudienceMatchesAnyOrAllTagsOnceWithinSameServer()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var runtime = StartRequest(serverId, Guid.NewGuid()).Trigger.Runtime!;
        var firstTag = Guid.NewGuid();
        var secondTag = Guid.NewGuid();
        var firstPlayer = new Player { PlayerId = Guid.NewGuid(), Guid = "FIRST", GameType = (int)GameType.CallOfDuty4 };
        var secondPlayer = new Player { PlayerId = Guid.NewGuid(), Guid = "SECOND", GameType = (int)GameType.CallOfDuty4 };
        context.Players.AddRange(firstPlayer, secondPlayer);
        context.PlayerTags.AddRange(
            new() { PlayerTagId = Guid.NewGuid(), PlayerId = firstPlayer.PlayerId, TagId = firstTag },
            new() { PlayerTagId = Guid.NewGuid(), PlayerId = firstPlayer.PlayerId, TagId = secondTag },
            new() { PlayerTagId = Guid.NewGuid(), PlayerId = secondPlayer.PlayerId, TagId = secondTag });
        await context.SaveChangesAsync();
        var bindings = (IRuleConnectionBindingsApi)new RuleConnectionBindingsController(context);
        var observed = await bindings.ObserveSnapshot(serverId, new()
        {
            GameServerId = serverId,
            GameType = GameType.CallOfDuty4,
            SnapshotId = Guid.NewGuid(),
            SequenceId = 1,
            ObservedAtUtc = DateTimeOffset.UtcNow,
            Runtime = runtime,
            Players =
            [
                new() { PlayerGuid = "FIRST", SlotId = 0, ConnectionId = Guid.NewGuid(), State = RuleConnectionState.Connected },
                new() { PlayerGuid = "SECOND", SlotId = 1, ConnectionId = Guid.NewGuid(), State = RuleConnectionState.Connected }
            ]
        });
        Assert.Equal(HttpStatusCode.OK, observed.StatusCode);
        var request = new RuleTaggedAudienceRequestDto { TagIds = [firstTag, secondTag] };
        var any = (await api.GetTaggedAudience(serverId, request)).Result!.Data!;
        Assert.True(any.Available);
        Assert.False(any.Partial);
        Assert.Equal(2, any.Recipients.Length);
        var all = (await api.GetTaggedAudience(serverId, request with { Match = RuleRecipientTagMatch.All })).Result!.Data!;
        Assert.Equal("FIRST", Assert.Single(all.Recipients).PlayerGuid);
        Assert.All(any.Recipients, item => Assert.Equal(serverId, item.GameServerId));
        var server = context.GameServers.Single(item => item.GameServerId == serverId);
        server.GameType = (int)GameType.CallOfDuty2;
        await context.SaveChangesAsync();
        Assert.False((await api.GetTaggedAudience(serverId, request)).Result!.Data!.Available);
        server.Deleted = true;
        await context.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetTaggedAudience(serverId, request)).StatusCode);
    }

    [Fact]
    public async Task PreparedFragmentsFreezeWinnerResumeIndependentlyAndEnforceOrder()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), actionTypeId: "message.private", parametersJson: /*lang=json,strict*/ """{"messages":["first","second"]}""")))
            .Result!.Data!;
        var first = DeliveryCommand(serverId, execution, 0, "first");
        var second = DeliveryCommand(serverId, execution, 1, "second");
        var prepare = new PrepareRuleActionDto
        {
            ExpectedVersion = execution.Version,
            ResolvedParametersJson = /*lang=json,strict*/ """{"messages":["first","second"]}""",
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
        var discovered = await api.SearchRuleExecutions(new() { GameServerId = serverId, FeatureId = execution.FeatureId });
        var discoveredPlan = Assert.Single(Assert.Single(discovered.Result!.Data!.Items!).PreparedActions);
        Assert.Equal(1, discoveredPlan.CompletedFragmentCount);
        Assert.Equal(2, discoveredPlan.Commands.Length);
    }

    [Theory]
    [InlineData(RuleActionExecutionOutcome.OutcomeUnknown)]
    [InlineData(RuleActionExecutionOutcome.SkippedUnsupported)]
    public async Task PluginTerminalReceiptPreservesHonestOutcomeAndControlsNextFragment(RuleActionExecutionOutcome receipt)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var start = StartRequest(serverId, Guid.NewGuid(), actionTypeId: "message.private", parametersJson: /*lang=json,strict*/ """{"message":"hello"}""");
        start = start with
        {
            Trigger = start.Trigger with
            {
                ConnectionGeneration = 1,
                Runtime = start.Trigger.Runtime! with
                {
                    Id = "cod4x-plugin",
                    IdentityModel = RuleRuntimeIdentityModel.PluginRuntimeGeneration,
                    Capabilities = ["cod4x.chat.fragments"]
                }
            }
        };
        var execution = (await api.StartExecution(serverId, start)).Result!.Data!;
        var first = DeliveryCommand(serverId, execution, 0, "hel");
        var second = DeliveryCommand(serverId, execution, 1, "lo");
        await api.PrepareAction(serverId, execution.ExecutionId, new()
        {
            ExpectedVersion = execution.Version,
            ResolvedParametersJson = /*lang=json,strict*/ """{"message":"hello"}""",
            Commands = [first, second]
        });
        var claimed = (await api.ClaimAction(serverId, execution.ExecutionId, ChildClaim(execution, first))).Result!.Data!;
        var dispatched = await api.UpdateActionOutcome(serverId, execution.ExecutionId, first.ActionId, new()
        {
            ExpectedVersion = claimed.Execution.Version,
            Result = ChildResult(first, RuleActionExecutionOutcome.Dispatched),
            NextState = RuleFeatureExecutionState.AwaitingActionOutcome
        });
        var unknown = await api.UpdateActionOutcome(serverId, execution.ExecutionId, first.ActionId, new()
        {
            ExpectedVersion = dispatched.Result!.Data!.Execution.Version,
            Result = ChildResult(first, receipt),
            NextState = RuleFeatureExecutionState.Evaluating
        });
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        execution = unknown.Result!.Data!.Execution;
        Assert.False((await api.ClaimAction(serverId, execution.ExecutionId, ChildClaim(execution, first))).Result!.Data!.ShouldDispatch);
        var next = await api.ClaimAction(serverId, execution.ExecutionId, ChildClaim(execution, second));
        if (receipt == RuleActionExecutionOutcome.OutcomeUnknown)
        {
            Assert.True(next.Result!.Data!.ShouldDispatch);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Conflict, next.StatusCode);
            Assert.False((await api.ClaimAction(serverId, execution.ExecutionId, ChildClaim(execution, second) with
            {
                TerminalOutcome = RuleActionExecutionOutcome.SkippedDependency
            })).Result!.Data!.ShouldDispatch);
        }
        var plan = Assert.Single((await api.GetExecution(serverId, execution.ExecutionId)).Result!.Data!.PreparedActions);
        Assert.Equal(receipt == RuleActionExecutionOutcome.OutcomeUnknown
            ? RuleActionExecutionOutcome.OutcomeUnknown : RuleActionExecutionOutcome.Failed, plan.Outcome);
        Assert.Equal(0, plan.CompletedFragmentCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdditionalProtectionExecutionWaitsUntilTerminalAndClear(bool completed)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var start = StartRequest(serverId, Guid.NewGuid(), actionTypeId: "message.private");
        start = start with
        {
            FeatureId = "welcome-messages",
            FrozenRulesJson = /*lang=json,strict*/ """
                {"timing":{"connectionDelaySeconds":0,"freshnessSeconds":120},"rules":[
                {"id":"rule-1","actions":[{"id":"kick-player","typeId":"message.private","parameters":{"message":"hello"}}]}]}
                """,
            Coordination = new() { DueAtUtc = start.Trigger.OccurredAtUtc, ExpiresAtUtc = start.Trigger.OccurredAtUtc.AddSeconds(120) }
        };
        var execution = (await api.StartExecution(serverId, start)).Result!.Data!;
        var protection = (await api.StartExecution(serverId, StartRequest(serverId, execution.ConnectionId))).Result!.Data!;
        if (completed)
        {
            await api.UpdateProgress(serverId, protection.ExecutionId, new()
            {
                ExpectedVersion = protection.Version,
                State = RuleFeatureExecutionState.Completed
            });
        }
        var prepared = await api.PrepareAction(serverId, execution.ExecutionId, new()
        {
            ExpectedVersion = execution.Version,
            ResolvedParametersJson = /*lang=json,strict*/ """{"message":"hello"}""",
            Commands = [DeliveryCommand(serverId, execution, 0, "hello")]
        });
        Assert.Equal(completed ? HttpStatusCode.Created : HttpStatusCode.Conflict, prepared.StatusCode);
    }

    [Theory]
    [InlineData("expiry")]
    [InlineData("protection")]
    [InlineData("disconnect")]
    public async Task FrozenChildrenCanBeTerminalizedWithoutDispatchAfterDeliveryBecomesIneligible(string reason)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var start = StartRequest(serverId, Guid.NewGuid(), actionTypeId: "message.private");
        start = start with
        {
            FeatureId = "welcome-messages",
            FrozenRulesJson = /*lang=json,strict*/ """
                {"timing":{"connectionDelaySeconds":0,"freshnessSeconds":2},"rules":[
                {"id":"rule-1","actions":[{"id":"kick-player","typeId":"message.private","parameters":{"message":"hello"}}]}]}
                """,
            Coordination = new() { DueAtUtc = start.Trigger.OccurredAtUtc, ExpiresAtUtc = start.Trigger.OccurredAtUtc.AddSeconds(2) }
        };
        var execution = (await api.StartExecution(serverId, start)).Result!.Data!;
        var bindings = (IRuleConnectionBindingsApi)new RuleConnectionBindingsController(context);
        var snapshot = new RuleConnectionStatusSnapshotDto
        {
            GameServerId = serverId,
            GameType = GameType.CallOfDuty4,
            SnapshotId = Guid.NewGuid(),
            SequenceId = 1,
            ObservedAtUtc = DateTimeOffset.UtcNow,
            Runtime = execution.Trigger.Runtime!,
            Players = [new()
            {
                PlayerGuid = execution.Trigger.PlayerGuid, SlotId = execution.Trigger.SlotId ?? 0,
                ConnectionId = execution.ConnectionId, State = RuleConnectionState.Connected
            }]
        };
        if (reason == "disconnect")
        {
            Assert.Equal(HttpStatusCode.OK, (await bindings.ObserveSnapshot(serverId, snapshot)).StatusCode);
        }
        var commands = Enumerable.Range(0, 3).Select(index => DeliveryCommand(serverId, execution, index, "part") with
        {
            ExpiresAtUtc = start.Trigger.OccurredAtUtc.AddSeconds(2)
        }).ToArray();
        Assert.Equal(HttpStatusCode.Created, (await api.PrepareAction(serverId, execution.ExecutionId, new()
        {
            ExpectedVersion = execution.Version,
            ResolvedParametersJson = /*lang=json,strict*/ """{"message":"hello"}""",
            Commands = commands
        })).StatusCode);
        var invalid = ChildClaim(execution, commands[0]) with { TerminalOutcome = RuleActionExecutionOutcome.Applied };
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ClaimAction(serverId, execution.ExecutionId, invalid)).StatusCode);
        var claimed = (await api.ClaimAction(serverId, execution.ExecutionId, ChildClaim(execution, commands[0]))).Result!.Data!;
        var applied = await api.UpdateActionOutcome(serverId, execution.ExecutionId, commands[0].ActionId, new()
        {
            ExpectedVersion = claimed.Execution.Version,
            Result = ChildResult(commands[0], RuleActionExecutionOutcome.Applied),
            NextState = RuleFeatureExecutionState.Evaluating
        });
        execution = applied.Result!.Data!.Execution;
        if (reason == "expiry")
        {
            await Task.Delay(TimeSpan.FromSeconds(2.1));
        }
        else if (reason == "protection")
        {
            await api.StartExecution(serverId, StartRequest(serverId, execution.ConnectionId));
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, (await bindings.ObserveSnapshot(serverId, snapshot with
            {
                SnapshotId = Guid.NewGuid(),
                SequenceId = 2,
                ObservedAtUtc = DateTimeOffset.UtcNow,
                Players = [snapshot.Players[0] with { State = RuleConnectionState.Disconnected }]
            })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await bindings.GetCurrentBinding(serverId, execution.Trigger.SlotId ?? 0, execution.Trigger.PlayerGuid!)).StatusCode);
        }
        if (reason != "disconnect")
        {
            Assert.Equal(HttpStatusCode.Conflict,
                (await api.ClaimAction(serverId, execution.ExecutionId, ChildClaim(execution, commands[1]))).StatusCode);
        }
        var terminal = ChildClaim(execution, commands[1]) with
        {
            TerminalOutcome = reason == "protection" ? RuleActionExecutionOutcome.SkippedDependency : RuleActionExecutionOutcome.Stale,
            TerminalDetail = reason
        };
        var skipped = (await api.ClaimAction(serverId, execution.ExecutionId, terminal)).Result!.Data!;
        Assert.False(skipped.ShouldDispatch);
        Assert.Equal(terminal.TerminalOutcome, skipped.Action.Outcome);
        Assert.Equal(RuleFeatureExecutionState.Evaluating, skipped.Execution.State);
        Assert.NotNull(skipped.Action.ResultJson);
        execution = skipped.Execution;
        Assert.Equal(HttpStatusCode.Conflict,
            (await api.ClaimAction(serverId, execution.ExecutionId, ChildClaim(execution, commands[2]))).StatusCode);
        var remainder = await api.ClaimAction(serverId, execution.ExecutionId, ChildClaim(execution, commands[2]) with
        {
            TerminalOutcome = RuleActionExecutionOutcome.SkippedDependency,
            TerminalDetail = "Earlier fragment was skipped."
        });
        Assert.False(remainder.Result!.Data!.ShouldDispatch);
        var replay = await api.ClaimAction(serverId, execution.ExecutionId, terminal);
        Assert.False(replay.Result!.Data!.ShouldDispatch);
        var plan = Assert.Single((await api.GetExecution(serverId, execution.ExecutionId)).Result!.Data!.PreparedActions);
        Assert.Equal(RuleActionExecutionOutcome.Failed, plan.Outcome);
        Assert.Equal(1, plan.CompletedFragmentCount);
    }

    [Fact]
    public async Task PreparedPlanRejectsChangedUnapprovedParametersAndRecipientOnModeration()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId,
            StartRequest(serverId, Guid.NewGuid(), actionTypeId: "message.private", parametersJson: /*lang=json,strict*/ """{"message":"literal"}""")))
            .Result!.Data!;
        var command = DeliveryCommand(serverId, execution, 0, "literal");
        var wrongRule = await api.PrepareAction(serverId, execution.ExecutionId, new()
        {
            ExpectedVersion = execution.Version,
            ResolvedParametersJson = /*lang=json,strict*/ """{"message":"literal"}""",
            Commands = [command with { Correlation = command.Correlation with { RuleId = "other-rule" } }]
        });
        Assert.Equal(HttpStatusCode.BadRequest, wrongRule.StatusCode);
        Assert.Empty(context.RulePreparedActions);
        var invalid = await api.PrepareAction(serverId, execution.ExecutionId, new()
        {
            ExpectedVersion = execution.Version,
            ResolvedParametersJson = /*lang=json,strict*/ """{"message":"changed"}""",
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
    public async Task TaggedPlanRequiresPreparationAndEveryAudienceRecipient()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId, StartRequest(serverId, Guid.NewGuid(),
            actionTypeId: "message.private-tagged", parametersJson: /*lang=json,strict*/ """{"message":"alert"}"""))).Result!.Data!;
        var direct = ClaimRequest(serverId, execution, new string('b', 64), RuleNativeEffectType.PrivateMessage);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ClaimAction(serverId, execution.ExecutionId, direct)).StatusCode);
        var command = DeliveryCommand(serverId, execution, 0, "alert");
        var first = new RuleMessageRecipientTarget
        {
            GameServerId = serverId,
            Runtime = command.Runtime,
            ConnectionId = Guid.NewGuid(),
            PlayerGuid = "FIRST",
            SlotId = 1
        };
        var second = first with { ConnectionId = Guid.NewGuid(), PlayerGuid = "SECOND", SlotId = 2 };
        var identity = command.DeliveryIdentity! with { RecipientConnectionId = first.ConnectionId };
        command = command with { RecipientTarget = first, DeliveryIdentity = identity, LogicalEffectId = identity.ToLogicalEffectId() };
        var request = new PrepareRuleActionDto
        {
            ExpectedVersion = execution.Version,
            ResolvedParametersJson = /*lang=json,strict*/ """{"message":"alert"}""",
            AudienceSnapshot = new() { GameServerId = serverId, Available = true, Recipients = [first, second] },
            Commands = [command]
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await api.PrepareAction(serverId, execution.ExecutionId, request)).StatusCode);
        Assert.Empty(context.RulePreparedActions);
        var secondIdentity = identity with { RecipientConnectionId = second.ConnectionId };
        request = request with
        {
            Commands = [command, command with
            {
                RecipientTarget = second, DeliveryIdentity = secondIdentity, LogicalEffectId = secondIdentity.ToLogicalEffectId()
            }]
        };
        Assert.Equal(HttpStatusCode.Created, (await api.PrepareAction(serverId, execution.ExecutionId, request)).StatusCode);
    }

    [Fact]
    public async Task TaggedEmptyAudienceIsSuccessOnlyWhenCompleteAndAvailable()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId, StartRequest(serverId, Guid.NewGuid(),
            actionTypeId: "message.private-tagged", parametersJson: /*lang=json,strict*/ """{"message":"alert"}"""))).Result!.Data!;
        var request = new PrepareRuleActionDto
        {
            ExpectedVersion = execution.Version,
            ResolvedParametersJson = /*lang=json,strict*/ """{"message":"alert"}""",
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
            FrozenRulesJson = /*lang=json,strict*/ """{"rules":[{"id":"rule-1","actions":[{"id":"kick-player","typeId":"message.private","parameters":{"message":"hello"}}]}]}""",
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
            FrozenRulesJson = /*lang=json,strict*/ """{"rules":[{"id":"rule-1","actions":[{"id":"kick-player","typeId":"moderation.observation","parameters":{"reason":"Hello {context.player.Name}"},"templateParameters":["reason"]}]}]}"""
        };
        var execution = (await api.StartExecution(serverId, start)).Result!.Data!;
        var request = ClaimRequest(serverId, execution, new string('f', 64), RuleNativeEffectType.Unknown);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ClaimAction(serverId, execution.ExecutionId, request)).StatusCode);
        request = request with { ResolvedParametersJson = /*lang=json,strict*/ """{"reason":"Hello Player"}""" };
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
            GameType = GameType.CallOfDuty4,
            SnapshotId = Guid.NewGuid(),
            ObservedAtUtc = DateTimeOffset.UtcNow,
            Players = [new() { State = RuleConnectionState.Unknown }]
        };
        context.RuleConnectionStatusSnapshots.Add(new()
        {
            GameServerId = serverId,
            GameType = (int)GameType.CallOfDuty4,
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
