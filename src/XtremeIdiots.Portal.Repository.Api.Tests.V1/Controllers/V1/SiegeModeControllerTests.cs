using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.SiegeMode;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.SiegeMode;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed class SiegeModeControllerTests
{
    [Fact]
    public async Task StartSession_RequiresExplicitMode()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var result = await new SiegeModeController(context).StartSession(Guid.NewGuid(),
            new StartSiegeSessionDto { OperationId = Guid.NewGuid(), Actor = "admin", ExpectedDurationMinutes = 240 });

        Assert.Equal(400, Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode);
    }

    [Fact]
    public async Task LifecycleResponses_IncludeExistingActionCount()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        await SeedSiegeSettings(context);
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
        context.RuleActionExecutions.AddRange(
            new RuleActionExecution
            {
                ActionExecutionId = Guid.NewGuid(),
                GameServerId = serverId,
                SiegeSessionId = sessionId,
                LogicalEffectId = "effect1",
                ActionId = "a",
                ActionTypeId = "player.kick",
                ParametersJson = "{}",
                CorrelationJson = "{}",
                CommandJson = "{}",
                Mode = "DryRun",
                Outcome = "WouldExecute"
            },
            new RuleActionExecution
            {
                ActionExecutionId = Guid.NewGuid(),
                GameServerId = serverId,
                SiegeSessionId = sessionId,
                LogicalEffectId = "effect3",
                ActionId = "c",
                ActionTypeId = "player.kick",
                ParametersJson = "{}",
                CorrelationJson = "{}",
                CommandJson = "{}",
                Mode = "Enforce",
                Outcome = "Applied"
            },
            new RuleActionExecution
            {
                ActionExecutionId = Guid.NewGuid(),
                GameServerId = serverId,
                SiegeSessionId = Guid.NewGuid(),
                LogicalEffectId = "effect2",
                ActionId = "b",
                ActionTypeId = "player.kick",
                ParametersJson = "{}",
                CorrelationJson = "{}",
                CommandJson = "{}",
                Mode = "DryRun",
                Outcome = "WouldExecute"
            });
        await context.SaveChangesAsync();

        Assert.Equal(2, Payload<SiegeSessionDto>(await controller.GetCurrentSession(serverId)).ActionCount);
        Assert.Equal(2, Assert.Single(Payload<CollectionModel<SiegeSessionDto>>(
            await controller.GetSessions(serverId)).Items!).ActionCount);
        Assert.Equal(2, Payload<SiegeSessionDto>(await controller.StartSession(serverId, start)).ActionCount);
        var extend = new ChangeSiegeSessionDto { OperationId = Guid.NewGuid(), Actor = "admin", ExtensionMinutes = 30 };
        Assert.Equal(2, Payload<SiegeSessionDto>(await controller.ExtendSession(serverId, sessionId, extend)).ActionCount);
        Assert.Equal(2, Payload<SiegeSessionDto>(await controller.ExtendSession(serverId, sessionId, extend)).ActionCount);
        var end = new ChangeSiegeSessionDto { OperationId = Guid.NewGuid(), Actor = "admin" };
        Assert.Equal(2, Payload<SiegeSessionDto>(await controller.EndSession(serverId, sessionId, end)).ActionCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangedOrDeletedServer_DoesNotReturnCurrentSession(bool deleted)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        await SeedSiegeSettings(context);
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
        Assert.Equal(404, Assert.IsAssignableFrom<IStatusCodeActionResult>(current).StatusCode);
    }

    [Fact]
    public async Task AuditSearch_OnlyReturnsRequestedServers()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        await SeedSiegeSettings(context);
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
            await controller.RecordCheck(serverId, sessionId, new RecordSiegeCheckDto
            {
                CheckId = Guid.NewGuid(),
                ConnectionId = Guid.NewGuid(),
                PlayerGuid = "player",
                Outcome = "Unknown",
                Detail = "test"
            });
        }
        var query = new SiegeAuditQueryDto { GameServerIds = [firstServer] };
        var sessions = Payload<CollectionModel<SiegeSessionDto>>(await controller.SearchSessions(query));
        var checks = Payload<CollectionModel<SiegeCheckDto>>(await controller.SearchChecks(query));
        Assert.Equal(firstServer, Assert.Single(sessions.Items!).GameServerId);
        Assert.Equal(firstServer, Assert.Single(checks.Items!).GameServerId);
    }

    [Fact]
    public async Task UnknownConnectionCheck_IsAuditedWithoutDispatch()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        await SeedSiegeSettings(context);
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
        var conflict = await controller.RecordCheck(serverId, sessionId, request with { Outcome = "Failed" });
        Assert.Equal(409, Assert.IsAssignableFrom<IStatusCodeActionResult>(conflict).StatusCode);
    }

    [Fact]
    public async Task Baseline_RemainsStableAfterNewHistoryAndPruning()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        await SeedSiegeSettings(context);
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

    [Fact]
    public async Task StartSession_UsesEffectiveServerEnablementDurationAndFrozenRules()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        await SeedSiegeSettings(context, enabled: false);
        var serverId = Guid.NewGuid();
        context.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Test", Hostname = "localhost" });
        var serverOverride = new SiegeModeServerSettingsOverrideDocument
        {
            EnabledOverride = FeatureEnablementOverride.Enabled,
            DurationMinutesOverride = 30,
            InheritGlobalRules = true
        };
        context.GameServerConfigurations.Add(new GameServerConfiguration
        {
            GameServerId = serverId,
            Namespace = SiegeModeSettingsConstants.Namespace,
            Configuration = JsonSerializer.Serialize(serverOverride, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await new SiegeModeController(context).StartSession(serverId, new StartSiegeSessionDto
        {
            OperationId = Guid.NewGuid(),
            Actor = "admin",
            ExpectedDurationMinutes = 30,
            Mode = SiegeSessionMode.DryRun
        });

        Assert.Equal(201, Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode);
        var session = await context.SiegeSessions.SingleAsync();
        var frozen = JsonSerializer.Deserialize<SiegeModeSettingsDocument>(
            session.SettingsJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.True(frozen.Enabled);
        Assert.Equal(30, frozen.DurationMinutes);
        Assert.Equal("new-guid", Assert.Single(frozen.Rules).Id);
        Assert.Equal(TimeSpan.FromMinutes(30), session.ExpiresAtUtc - session.StartedAtUtc);
    }

    [Fact]
    public async Task StartSession_RejectsEffectivelyDisabledFeature()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        await SeedSiegeSettings(context, enabled: true);
        var serverId = Guid.NewGuid();
        context.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Test", Hostname = "localhost" });
        var serverOverride = new SiegeModeServerSettingsOverrideDocument
        {
            EnabledOverride = FeatureEnablementOverride.Disabled,
            InheritGlobalRules = true
        };
        context.GameServerConfigurations.Add(new GameServerConfiguration
        {
            GameServerId = serverId,
            Namespace = SiegeModeSettingsConstants.Namespace,
            Configuration = JsonSerializer.Serialize(serverOverride, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await new SiegeModeController(context).StartSession(serverId, new StartSiegeSessionDto
        {
            OperationId = Guid.NewGuid(),
            Actor = "admin",
            ExpectedDurationMinutes = 240,
            Mode = SiegeSessionMode.DryRun
        });

        Assert.Equal(409, Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode);
        Assert.Empty(context.SiegeSessions);
    }

    [Fact]
    public async Task StartSession_RejectsDuplicateRuleIdsInEffectiveSettings()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        await SeedSiegeSettings(context);
        var serverId = Guid.NewGuid();
        context.GameServers.Add(new GameServer { GameServerId = serverId, Title = "Test", Hostname = "localhost" });
        var global = JsonSerializer.Deserialize<SiegeModeSettingsDocument>(
            context.GlobalConfigurations.Single().Configuration,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var serverOverride = new SiegeModeServerSettingsOverrideDocument
        {
            EnabledOverride = FeatureEnablementOverride.Inherit,
            InheritGlobalRules = true,
            Rules = [global.Rules[0]]
        };
        context.GameServerConfigurations.Add(new GameServerConfiguration
        {
            GameServerId = serverId,
            Namespace = SiegeModeSettingsConstants.Namespace,
            Configuration = JsonSerializer.Serialize(serverOverride, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await new SiegeModeController(context).StartSession(serverId, new StartSiegeSessionDto
        {
            OperationId = Guid.NewGuid(),
            Actor = "admin",
            ExpectedDurationMinutes = 240,
            Mode = SiegeSessionMode.DryRun
        });

        Assert.Equal(409, Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode);
        Assert.Empty(context.SiegeSessions);
    }

    private static T Payload<T>(IActionResult result)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        return Assert.IsType<ApiResponse<T>>(objectResult.Value).Data!;
    }

    private static async Task SeedSiegeSettings(PortalDbContext context, bool enabled = true)
    {
        var settings = new SiegeModeSettingsDocument
        {
            Enabled = enabled,
            DurationMinutes = 240,
            Rules =
            [
                new RuleDefinition
                {
                    Id = "new-guid",
                    Enabled = true,
                    Order = 0,
                    Condition = new RuleConditionNode
                    {
                        ConditionId = "player.first-seen-guid",
                        Parameters = new Dictionary<string, JsonElement>
                        {
                            ["operator"] = JsonSerializer.SerializeToElement("IsNew"),
                            ["referenceScope"] = JsonSerializer.SerializeToElement("SiegeBaseline")
                        }
                    },
                    Actions =
                    [
                        new RuleActionDefinition
                        {
                            Id = "record",
                            TypeId = "moderation.observation",
                            Parameters = new Dictionary<string, JsonElement>
                            {
                                ["reason"] = JsonSerializer.SerializeToElement("Test observation")
                            }
                        }
                    ],
                    AfterMatch = RuleAfterMatch.StopProcessingRules
                }
            ]
        };
        context.GlobalConfigurations.Add(new GlobalConfiguration
        {
            Namespace = SiegeModeSettingsConstants.Namespace,
            Configuration = JsonSerializer.Serialize(settings, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }
}
