using System.Net;
using Microsoft.EntityFrameworkCore;
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
    public async Task OccurrenceClaimDoesNotRepeatDispatchAndRejectsChangedSafeFields()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var playerId = await AddOccurrencePlayer(context);
        var occurrenceId = Guid.NewGuid();
        var request = OccurrenceClaim(serverId, occurrenceId, playerId);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);

        var first = await api.ClaimOccurrenceEffect(serverId, occurrenceId, request);
        var duplicate = await api.ClaimOccurrenceEffect(serverId, occurrenceId, request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.True(first.Result!.Data!.ShouldDispatch);
        Assert.Equal(RuleOccurrenceEffectDisposition.Acquired, first.Result.Data.Disposition);
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.False(duplicate.Result!.Data!.ShouldDispatch);
        Assert.Equal(RuleOccurrenceEffectDisposition.AlreadyClaimed, duplicate.Result.Data.Disposition);
        Assert.Equal(HttpStatusCode.Conflict, (await api.ClaimOccurrenceEffect(
            serverId, occurrenceId, request with { ActionTypeId = "message.public" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.ClaimOccurrenceEffect(
            serverId, occurrenceId, request with { RuleSetRevision = "revision-2" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ClaimOccurrenceEffect(
            serverId, Guid.NewGuid(), request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ClaimOccurrenceEffect(
            serverId, occurrenceId, request with { Mode = RuleExecutionMode.DryRun })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.ClaimOccurrenceEffect(
            serverId, occurrenceId, request with { PlayerId = Guid.NewGuid() })).StatusCode);
        Assert.Single(await context.RuleOccurrenceEffects.ToListAsync());
    }

    [Fact]
    public async Task OccurrenceClaimRequiresAnActiveServerAndPlayerOfItsGame()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty2 };
        context.Players.Add(player);
        await context.SaveChangesAsync();
        var occurrence = Guid.NewGuid();
        var claim = OccurrenceClaim(serverId, occurrence, player.PlayerId);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);

        Assert.Equal(HttpStatusCode.NotFound, (await api.ClaimOccurrenceEffect(serverId, occurrence, claim)).StatusCode);
        (await context.Players.SingleAsync()).GameType = (int)GameType.CallOfDuty4;
        (await context.GameServers.SingleAsync()).Deleted = true;
        await context.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await api.ClaimOccurrenceEffect(serverId, occurrence, claim)).StatusCode);
        Assert.Empty(await context.RuleOccurrenceEffects.ToListAsync());
    }

    [Fact]
    public async Task OccurrenceCooldownIsRecordedWithSuppressionAndDoesNotAuthorizeRetry()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var playerId = await AddOccurrencePlayer(context);
        var firstOccurrence = Guid.NewGuid();
        var nextOccurrence = Guid.NewGuid();
        var cooldown = new RuleOccurrenceCooldownDto { PolicyId = "nudge", DurationSeconds = 120 };
        var firstRequest = OccurrenceClaim(serverId, firstOccurrence, playerId) with { Cooldown = cooldown };
        var nextRequest = OccurrenceClaim(serverId, nextOccurrence, playerId) with { Cooldown = cooldown };
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);

        Assert.Equal(HttpStatusCode.Created, (await api.ClaimOccurrenceEffect(serverId, firstOccurrence, firstRequest)).StatusCode);
        var suppressed = await api.ClaimOccurrenceEffect(serverId, nextOccurrence, nextRequest);
        var repeated = await api.ClaimOccurrenceEffect(serverId, nextOccurrence, nextRequest);
        Assert.Equal(HttpStatusCode.OK, suppressed.StatusCode);
        Assert.Equal(RuleOccurrenceEffectDisposition.CooldownSuppressed, suppressed.Result!.Data!.Disposition);
        Assert.False(suppressed.Result.Data.ShouldDispatch);
        Assert.Equal(RuleOccurrenceEffectDisposition.CooldownSuppressed, repeated.Result!.Data!.Disposition);
        Assert.False(repeated.Result.Data.ShouldDispatch);
        Assert.Equal(2, await context.RuleOccurrenceEffects.CountAsync());
        Assert.Single(await context.RuleOccurrenceCooldowns.ToListAsync());
    }

    [Fact]
    public async Task OccurrenceOutcomeNeverInfersApplicationOrRepeatsUnknownEffects()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var playerId = await AddOccurrencePlayer(context);
        var occurrenceId = Guid.NewGuid();
        var request = OccurrenceClaim(serverId, occurrenceId, playerId);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        await api.ClaimOccurrenceEffect(serverId, occurrenceId, request);

        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateOccurrenceEffectOutcome(
            serverId, occurrenceId, request.LogicalEffectId, new() { Outcome = RuleOccurrenceEffectOutcome.Applied })).StatusCode);
        var unknown = new UpdateRuleOccurrenceEffectOutcomeDto { Outcome = RuleOccurrenceEffectOutcome.OutcomeUnknown };
        Assert.Equal(HttpStatusCode.OK, (await api.UpdateOccurrenceEffectOutcome(serverId, occurrenceId, request.LogicalEffectId, unknown)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.UpdateOccurrenceEffectOutcome(serverId, occurrenceId, request.LogicalEffectId, unknown)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.UpdateOccurrenceEffectOutcome(
            serverId, occurrenceId, request.LogicalEffectId, new() { Outcome = RuleOccurrenceEffectOutcome.Dispatched })).StatusCode);
        Assert.False((await api.ClaimOccurrenceEffect(serverId, occurrenceId, request)).Result!.Data!.ShouldDispatch);
    }

    [Fact]
    public async Task OccurrenceRecordIsCreatedAndLinkedOnceAndDeletionCannotRecreateIt()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var playerId = await AddOccurrencePlayer(context);
        var occurrenceId = Guid.NewGuid();
        var request = OccurrenceClaim(serverId, occurrenceId, playerId) with { ActionTypeId = "moderation.observation" };
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        await api.ClaimOccurrenceEffect(serverId, occurrenceId, request);
        var record = new EnsureRuleOccurrenceAdminActionDto { Type = AdminActionType.Observation, Text = "First observation" };

        var created = await api.EnsureOccurrenceAdminAction(serverId, occurrenceId, request.LogicalEffectId, record);
        var repeated = await api.EnsureOccurrenceAdminAction(serverId, occurrenceId, request.LogicalEffectId, record with { Text = "Changed" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal(created.Result!.Data!.AdminAction.AdminActionId, repeated.Result!.Data!.AdminAction.AdminActionId);
        Assert.Equal("First observation", repeated.Result.Data.AdminAction.Text);
        Assert.Single(await context.AdminActions.ToListAsync());
        var effect = await context.RuleOccurrenceEffects.SingleAsync();
        Assert.True(effect.AdminActionRecorded);

        context.AdminActions.Remove(await context.AdminActions.SingleAsync());
        effect.AdminActionId = null;
        await context.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await api.EnsureOccurrenceAdminAction(
            serverId, occurrenceId, request.LogicalEffectId, record)).StatusCode);
        Assert.Empty(await context.AdminActions.ToListAsync());
    }

    [Theory]
    [InlineData("moderation.observation", AdminActionType.Observation)]
    [InlineData("moderation.kick-with-record", AdminActionType.Kick)]
    public async Task OccurrenceObservationAndKickRequireTheirOwnRecord(string actionTypeId, AdminActionType type)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var playerId = await AddOccurrencePlayer(context);
        var occurrenceId = Guid.NewGuid();
        var claim = OccurrenceClaim(serverId, occurrenceId, playerId) with { ActionTypeId = actionTypeId };
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        await api.ClaimOccurrenceEffect(serverId, occurrenceId, claim);
        var existing = new AdminAction
        {
            AdminActionId = Guid.NewGuid(),
            PlayerId = playerId,
            Type = (int)type,
            Text = "Previous occurrence",
            Created = DateTime.UtcNow
        };
        context.AdminActions.Add(existing);
        await context.SaveChangesAsync();

        var request = new EnsureRuleOccurrenceAdminActionDto { Type = type, ExistingAdminActionId = existing.AdminActionId };
        Assert.Equal(HttpStatusCode.BadRequest, (await api.EnsureOccurrenceAdminAction(
            serverId, occurrenceId, claim.LogicalEffectId, request)).StatusCode);
        Assert.False((await context.RuleOccurrenceEffects.SingleAsync()).AdminActionRecorded);

        var created = await api.EnsureOccurrenceAdminAction(serverId, occurrenceId, claim.LogicalEffectId,
            request with { ExistingAdminActionId = null, Text = "Current occurrence" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotEqual(existing.AdminActionId, created.Result!.Data!.AdminAction.AdminActionId);
        Assert.Equal(2, await context.AdminActions.CountAsync());
    }

    [Fact]
    public async Task OccurrenceBanLinksOnlyAnActiveSamePlayerRecord()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var playerId = await AddOccurrencePlayer(context);
        var otherPlayer = await AddOccurrencePlayer(context);
        var occurrenceId = Guid.NewGuid();
        var request = OccurrenceClaim(serverId, occurrenceId, playerId) with { ActionTypeId = "moderation.temporary-ban" };
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        await api.ClaimOccurrenceEffect(serverId, occurrenceId, request);
        var active = new AdminAction
        {
            AdminActionId = Guid.NewGuid(),
            PlayerId = playerId,
            Type = (int)AdminActionType.Ban,
            Text = "Active",
            Created = DateTime.UtcNow
        };
        var unrelated = new AdminAction
        {
            AdminActionId = Guid.NewGuid(),
            PlayerId = otherPlayer,
            Type = (int)AdminActionType.Ban,
            Text = "Other player",
            Created = DateTime.UtcNow
        };
        context.AdminActions.AddRange(active, unrelated);
        await context.SaveChangesAsync();
        var record = new EnsureRuleOccurrenceAdminActionDto { Type = AdminActionType.TempBan };

        Assert.Equal(HttpStatusCode.BadRequest, (await api.EnsureOccurrenceAdminAction(
            serverId, occurrenceId, request.LogicalEffectId, record)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.EnsureOccurrenceAdminAction(
            serverId, occurrenceId, request.LogicalEffectId, record with { ExistingAdminActionId = unrelated.AdminActionId })).StatusCode);
        var linked = await api.EnsureOccurrenceAdminAction(
            serverId, occurrenceId, request.LogicalEffectId, record with { ExistingAdminActionId = active.AdminActionId });
        Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
        Assert.Equal(active.AdminActionId, linked.Result!.Data!.AdminAction.AdminActionId);
        Assert.Equal(2, await context.AdminActions.CountAsync());
    }

    private static async Task<Guid> AddOccurrencePlayer(PortalDbContext context)
    {
        var player = new Player { PlayerId = Guid.NewGuid(), GameType = (int)GameType.CallOfDuty4 };
        context.Players.Add(player);
        await context.SaveChangesAsync();
        return player.PlayerId;
    }

    private static ClaimRuleOccurrenceEffectDto OccurrenceClaim(Guid serverId, Guid occurrenceId, Guid playerId) => new()
    {
        LogicalEffectId = RuleOccurrenceEffectIdentity.Create(serverId, occurrenceId, "chat-moderation", "new-player", "nudge"),
        PlayerId = playerId,
        FeatureId = "chat-moderation",
        RuleId = "new-player",
        ActionId = "nudge",
        ActionTypeId = "message.private",
        RuleSetRevision = "revision-1",
        Mode = RuleExecutionMode.Enforce
    };
}
