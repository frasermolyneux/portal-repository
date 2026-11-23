using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.SiegeMode;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed class SiegeModeControllerTests
{
    [Theory]
    [InlineData(SiegeSessionMode.DryRun, false, SiegeActionOutcomes.WouldKick)]
    [InlineData(SiegeSessionMode.Enforce, true, SiegeActionOutcomes.Pending)]
    public async Task ClaimAction_RespectsModeAndDeduplicates(SiegeSessionMode mode, bool shouldDispatch, string outcome)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = Guid.NewGuid();
        context.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Test", Hostname = "localhost" });
        await context.SaveChangesAsync();
        var controller = new SiegeModeController(context);
        var sessionId = Guid.NewGuid();
        var start = await controller.StartSession(serverId, new StartSiegeSessionDto
        {
            OperationId = sessionId,
            Actor = "admin",
            ExpectedDurationMinutes = 240,
            Mode = mode
        });
        Assert.Equal(201, Assert.IsAssignableFrom<ObjectResult>(start).StatusCode);

        var request = new ClaimSiegeActionDto
        {
            ActionId = Guid.NewGuid(),
            ConnectionId = Guid.NewGuid(),
            PlayerGuid = "player",
            SlotId = 1,
            RuleId = "new-guid",
            BaselineRevision = 0
        };
        var first = Payload<SiegeActionClaimDto>(await controller.ClaimAction(serverId, sessionId, request));
        var retry = Payload<SiegeActionClaimDto>(await controller.ClaimAction(serverId, sessionId, request));
        Assert.Equal(shouldDispatch, first.ShouldDispatch);
        Assert.False(retry.ShouldDispatch);
        Assert.Equal(outcome, first.Action.Outcome);
        Assert.Single(context.SiegeActions);

        var callback = await controller.UpdateActionOutcome(serverId, sessionId, request.ActionId,
            new UpdateSiegeActionOutcomeDto { ConnectionId = request.ConnectionId, Outcome = SiegeActionOutcomes.Dispatched });
        Assert.Equal(mode == SiegeSessionMode.DryRun ? 409 : 200, Assert.IsAssignableFrom<IStatusCodeActionResult>(callback).StatusCode);
    }

    [Fact]
    public async Task StartSession_RequiresExplicitMode()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var result = await new SiegeModeController(context).StartSession(Guid.NewGuid(),
            new StartSiegeSessionDto { OperationId = Guid.NewGuid(), Actor = "admin", ExpectedDurationMinutes = 240 });

        Assert.Equal(400, Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode);
    }

    [Fact]
    public async Task ClaimAction_MatchesTrimmedConfiguredRuleId()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        context.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Test", Hostname = "localhost" });
        await context.SaveChangesAsync();
        var controller = new SiegeModeController(context);
        await controller.StartSession(serverId, new StartSiegeSessionDto
        {
            OperationId = sessionId,
            Actor = "admin",
            ExpectedDurationMinutes = 240,
            Mode = SiegeSessionMode.DryRun
        });
        var session = await context.SiegeSessions.SingleAsync();
        session.SettingsJson = session.SettingsJson.Replace("\"new-guid\"", $"\"{new string(' ', 64)}new-guid \"", StringComparison.Ordinal);
        await context.SaveChangesAsync();

        var claim = Payload<SiegeActionClaimDto>(await controller.ClaimAction(serverId, sessionId, new ClaimSiegeActionDto
        {
            ActionId = Guid.NewGuid(),
            ConnectionId = Guid.NewGuid(),
            PlayerGuid = "player",
            SlotId = 1,
            RuleId = "new-guid",
            BaselineRevision = 0
        }));
        Assert.Equal(SiegeActionOutcomes.WouldKick, claim.Action.Outcome);
    }

    [Fact]
    public async Task LifecycleResponses_IncludeExistingActionCount()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        context.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Test", Hostname = "localhost" });
        await context.SaveChangesAsync();
        var controller = new SiegeModeController(context);
        var start = new StartSiegeSessionDto
        {
            OperationId = sessionId,
            Actor = "admin",
            ExpectedDurationMinutes = 240,
            Mode = SiegeSessionMode.DryRun
        };
        await controller.StartSession(serverId, start);
        await controller.ClaimAction(serverId, sessionId, new ClaimSiegeActionDto
        {
            ActionId = Guid.NewGuid(),
            ConnectionId = Guid.NewGuid(),
            PlayerGuid = "player",
            SlotId = 1,
            RuleId = "new-guid",
            BaselineRevision = 0
        });

        Assert.Equal(1, Payload<SiegeSessionDto>(await controller.StartSession(serverId, start)).ActionCount);
        var extend = new ChangeSiegeSessionDto { OperationId = Guid.NewGuid(), Actor = "admin", ExtensionMinutes = 30 };
        Assert.Equal(1, Payload<SiegeSessionDto>(await controller.ExtendSession(serverId, sessionId, extend)).ActionCount);
        Assert.Equal(1, Payload<SiegeSessionDto>(await controller.ExtendSession(serverId, sessionId, extend)).ActionCount);
        var end = new ChangeSiegeSessionDto { OperationId = Guid.NewGuid(), Actor = "admin" };
        Assert.Equal(1, Payload<SiegeSessionDto>(await controller.EndSession(serverId, sessionId, end)).ActionCount);
    }

    [Fact]
    public async Task ActionOutcome_IsIdempotentAndRejectsConflictingCallbacks()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        context.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Test", Hostname = "localhost" });
        await context.SaveChangesAsync();
        var controller = new SiegeModeController(context);
        await controller.StartSession(serverId, new StartSiegeSessionDto
        {
            OperationId = sessionId,
            Actor = "admin",
            ExpectedDurationMinutes = 240,
            Mode = SiegeSessionMode.Enforce
        });
        var claim = new ClaimSiegeActionDto
        {
            ActionId = Guid.NewGuid(),
            ConnectionId = Guid.NewGuid(),
            PlayerGuid = "player",
            SlotId = 1,
            RuleId = "new-guid",
            BaselineRevision = 0
        };
        await controller.ClaimAction(serverId, sessionId, claim);
        var outcome = new UpdateSiegeActionOutcomeDto
        {
            ConnectionId = claim.ConnectionId,
            Outcome = SiegeActionOutcomes.Dispatched,
            Detail = "attempted"
        };
        Assert.Equal("Dispatched", Payload<SiegeActionDto>(await controller.UpdateActionOutcome(serverId, sessionId, claim.ActionId, outcome)).Outcome);
        Assert.Equal("Dispatched", Payload<SiegeActionDto>(await controller.UpdateActionOutcome(serverId, sessionId, claim.ActionId, outcome)).Outcome);
        var conflict = await controller.UpdateActionOutcome(serverId, sessionId, claim.ActionId,
            outcome with { Outcome = SiegeActionOutcomes.Applied });
        Assert.Equal(409, Assert.IsAssignableFrom<IStatusCodeActionResult>(conflict).StatusCode);
        await controller.EndSession(serverId, sessionId, new ChangeSiegeSessionDto { OperationId = Guid.NewGuid(), Actor = "admin" });
        var lateClaim = await controller.ClaimAction(serverId, sessionId,
            claim with { ActionId = Guid.NewGuid(), ConnectionId = Guid.NewGuid() });
        Assert.Equal(409, Assert.IsAssignableFrom<IStatusCodeActionResult>(lateClaim).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangedOrDeletedServer_DoesNotPermitNewClaims(bool deleted)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var server = new GameServer { GameServerId = Guid.NewGuid(), Title = "Test", Hostname = "localhost" };
        context.GameServers.Add(server);
        await context.SaveChangesAsync();
        var controller = new SiegeModeController(context);
        var sessionId = Guid.NewGuid();
        await controller.StartSession(server.GameServerId, new StartSiegeSessionDto
        {
            OperationId = sessionId,
            Actor = "admin",
            ExpectedDurationMinutes = 240,
            Mode = SiegeSessionMode.Enforce
        });
        var storedServer = await context.GameServers.SingleAsync();
        if (deleted)
        {
            storedServer.Deleted = true;
        }
        else
        {
            storedServer.GameType++;
        }
        await context.SaveChangesAsync();
        var current = await controller.GetCurrentSession(server.GameServerId);
        var claim = await controller.ClaimAction(server.GameServerId, sessionId, new ClaimSiegeActionDto
        {
            ActionId = Guid.NewGuid(),
            ConnectionId = Guid.NewGuid(),
            PlayerGuid = "player",
            SlotId = 1,
            RuleId = "new-guid",
            BaselineRevision = 0
        });
        Assert.Equal(404, Assert.IsAssignableFrom<IStatusCodeActionResult>(current).StatusCode);
        Assert.Equal(409, Assert.IsAssignableFrom<IStatusCodeActionResult>(claim).StatusCode);
    }

    [Fact]
    public async Task AuditSearch_OnlyReturnsRequestedServers()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var firstServer = Guid.NewGuid();
        var secondServer = Guid.NewGuid();
        context.GameServers.AddRange(
            new GameServer { GameServerId = firstServer, Title = "First", Hostname = "localhost" },
            new GameServer { GameServerId = secondServer, Title = "Second", Hostname = "localhost" });
        await context.SaveChangesAsync();
        var controller = new SiegeModeController(context);
        foreach (var serverId in new[] { firstServer, secondServer })
        {
            var sessionId = Guid.NewGuid();
            await controller.StartSession(serverId, new StartSiegeSessionDto
            {
                OperationId = sessionId,
                Actor = "admin",
                ExpectedDurationMinutes = 240,
                Mode = SiegeSessionMode.DryRun
            });
            await controller.ClaimAction(serverId, sessionId, new ClaimSiegeActionDto
            {
                ActionId = Guid.NewGuid(),
                ConnectionId = Guid.NewGuid(),
                PlayerGuid = "player",
                SlotId = 1,
                RuleId = "new-guid",
                BaselineRevision = 0
            });
        }
        var query = new SiegeAuditQueryDto { GameServerIds = [firstServer] };
        var sessions = Payload<CollectionModel<SiegeSessionDto>>(await controller.SearchSessions(query));
        var actions = Payload<CollectionModel<SiegeActionDto>>(await controller.SearchActions(query));
        Assert.Equal(firstServer, Assert.Single(sessions.Items!).GameServerId);
        Assert.Equal(firstServer, Assert.Single(actions.Items!).GameServerId);
    }

    [Fact]
    public async Task ActionSummary_GroupsAllRetainedMatchesByModeAndOutcome()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = Guid.NewGuid();
        var otherServerId = Guid.NewGuid();
        context.GameServers.AddRange(
            new GameServer { GameServerId = serverId, Title = "First", Hostname = "localhost" },
            new GameServer { GameServerId = otherServerId, Title = "Other", Hostname = "localhost" });
        await context.SaveChangesAsync();
        var controller = new SiegeModeController(context);

        async Task<Guid> Start(Guid server, SiegeSessionMode mode)
        {
            var id = Guid.NewGuid();
            await controller.StartSession(server, new StartSiegeSessionDto
            {
                OperationId = id,
                Actor = "admin",
                ExpectedDurationMinutes = 240,
                Mode = mode
            });
            return id;
        }

        async Task Claim(Guid server, Guid session, string? outcome = null)
        {
            var request = new ClaimSiegeActionDto
            {
                ActionId = Guid.NewGuid(),
                ConnectionId = Guid.NewGuid(),
                PlayerGuid = "player",
                SlotId = 1,
                RuleId = "new-guid",
                BaselineRevision = 0
            };
            await controller.ClaimAction(server, session, request);
            if (outcome is not null)
            {
                await controller.UpdateActionOutcome(server, session, request.ActionId,
                    new UpdateSiegeActionOutcomeDto { ConnectionId = request.ConnectionId, Outcome = outcome });
            }
        }

        var dryRun = await Start(serverId, SiegeSessionMode.DryRun);
        await Claim(serverId, dryRun);
        await Claim(serverId, dryRun);
        await controller.EndSession(serverId, dryRun, new ChangeSiegeSessionDto { OperationId = Guid.NewGuid(), Actor = "admin" });
        var enforce = await Start(serverId, SiegeSessionMode.Enforce);
        await Claim(serverId, enforce, SiegeActionOutcomes.Applied);
        await Claim(serverId, enforce, SiegeActionOutcomes.Failed);
        await Claim(serverId, enforce);
        var other = await Start(otherServerId, SiegeSessionMode.DryRun);
        await Claim(otherServerId, other);

        var oldAction = await context.SiegeActions.FirstAsync(a => a.SessionId == dryRun);
        oldAction.ClaimedAtUtc = DateTime.UtcNow.AddDays(-2);
        await context.SaveChangesAsync();
        var query = new SiegeAuditQueryDto
        {
            GameServerIds = [serverId],
            FromUtc = DateTime.UtcNow.AddDays(-1),
            ToUtc = DateTime.UtcNow.AddDays(1),
            SkipEntries = 1,
            TakeEntries = 1
        };
        var summary = Payload<SiegeActionSummaryDto>(await controller.SummarizeActions(query));
        Assert.Equal(4, summary.Counts.Sum(c => c.Count));
        Assert.Contains(summary.Counts, c => c.Mode == SiegeSessionMode.DryRun && c.Outcome == SiegeActionOutcomes.WouldKick && c.Count == 1);
        Assert.Contains(summary.Counts, c => c.Mode == SiegeSessionMode.Enforce && c.Outcome == SiegeActionOutcomes.Applied && c.Count == 1);
        Assert.Contains(summary.Counts, c => c.Mode == SiegeSessionMode.Enforce && c.Outcome == SiegeActionOutcomes.Failed && c.Count == 1);
        Assert.Contains(summary.Counts, c => c.Mode == SiegeSessionMode.Enforce && c.Outcome == SiegeActionOutcomes.Pending && c.Count == 1);
        Assert.Empty(context.AdminActions);
        Assert.Empty(Payload<SiegeActionSummaryDto>(await controller.SummarizeActions(
            query with { FromUtc = DateTime.UtcNow.AddDays(2), ToUtc = DateTime.UtcNow.AddDays(3) })).Counts);
    }

    [Fact]
    public async Task UnknownConnectionCheck_IsAuditedWithoutDispatch()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        context.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Test", Hostname = "localhost" });
        await context.SaveChangesAsync();
        var controller = new SiegeModeController(context);
        await controller.StartSession(serverId, new StartSiegeSessionDto
        {
            OperationId = sessionId,
            Actor = "admin",
            ExpectedDurationMinutes = 240,
            Mode = SiegeSessionMode.DryRun
        });
        var request = new RecordSiegeCheckDto
        {
            CheckId = Guid.NewGuid(),
            ConnectionId = null,
            Outcome = "Unknown",
            Detail = "connection identity unavailable"
        };
        var check = Payload<SiegeCheckDto>(await controller.RecordCheck(serverId, sessionId, request));
        var replay = Payload<SiegeCheckDto>(await controller.RecordCheck(serverId, sessionId, request));
        Assert.Null(check.ConnectionId);
        Assert.Equal(SiegeSessionMode.DryRun, check.Mode);
        Assert.Equal(check.CheckId, replay.CheckId);
        Assert.Empty(context.SiegeActions);
        var conflict = await controller.RecordCheck(serverId, sessionId, request with { Outcome = "Failed" });
        Assert.Equal(409, Assert.IsAssignableFrom<IStatusCodeActionResult>(conflict).StatusCode);
    }

    [Fact]
    public async Task Baseline_RemainsStableAfterNewHistoryAndPruning()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Test", Hostname = "localhost", GameType = 1 });
        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Guid = "known", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.Players.Add(new Player { PlayerId = Guid.NewGuid(), GameType = 2, Guid = "other-game", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = playerId, Address = "2001:0db8::1", Added = DateTime.UtcNow, LastUsed = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var controller = new SiegeModeController(context);
        var sessionId = Guid.NewGuid();
        var start = await controller.StartSession(serverId, new StartSiegeSessionDto
        {
            OperationId = sessionId,
            Actor = "admin",
            ExpectedDurationMinutes = 240,
            Mode = SiegeSessionMode.DryRun
        });
        Assert.Equal(201, Assert.IsAssignableFrom<IStatusCodeActionResult>(start).StatusCode);
        context.Players.Add(new Player { PlayerId = Guid.NewGuid(), GameType = 1, Guid = "new", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.PlayerIpAddresses.Add(new PlayerIpAddress { PlayerIpAddressId = Guid.NewGuid(), PlayerId = playerId, Address = "192.0.2.1", Added = DateTime.UtcNow, LastUsed = DateTime.UtcNow });
        context.PlayerIpAddresses.RemoveRange(context.PlayerIpAddresses.Where(ip => ip.Address == "2001:0db8::1"));
        await context.SaveChangesAsync();

        var known = Payload<SiegeBaselineDto>(await controller.GetBaseline(serverId, sessionId,
            new SiegeBaselineRequestDto { PlayerGuid = "known", IpAddress = "2001:db8::1" }));
        var recent = Payload<SiegeBaselineDto>(await controller.GetBaseline(serverId, sessionId,
            new SiegeBaselineRequestDto { PlayerGuid = "new", IpAddress = "192.0.2.1" }));
        var otherGame = Payload<SiegeBaselineDto>(await controller.GetBaseline(serverId, sessionId,
            new SiegeBaselineRequestDto { PlayerGuid = "other-game" }));
        Assert.False(known.NewGuid);
        Assert.False(known.NewIp);
        Assert.True(recent.NewGuid);
        Assert.True(recent.NewIp);
        Assert.True(otherGame.NewGuid);
        Assert.Null(otherGame.NewIp);
    }

    private static T Payload<T>(IActionResult result)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        return Assert.IsType<ApiResponse<T>>(objectResult.Value).Data!;
    }
}
