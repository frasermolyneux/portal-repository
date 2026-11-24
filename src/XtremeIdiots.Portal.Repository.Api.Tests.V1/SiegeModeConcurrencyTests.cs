using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.SiegeMode;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.SiegeMode;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1;

[Trait("Category", "SqlIntegration")]
[Collection("SqlServer")]
public sealed class SiegeModeConcurrencyTests
{
    [Theory]
    [InlineData(SiegeSessionMode.Enforce)]
    [InlineData(SiegeSessionMode.DryRun)]
    public Task ConcurrentStartsAndClaims_AreSerialized(SiegeSessionMode mode) =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var server = new GameServer { Title = "Test", Hostname = "test", BanFileRootPath = "/", GameType = 1 };
            setup.GameServers.Add(server);
            setup.GlobalConfigurations.Add(CreateEnabledSiegeConfiguration());
            await setup.SaveChangesAsync();

            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            async Task<IActionResult> Start(Guid id)
            {
                await using var context = new PortalDbContext(options);
                return await new SiegeModeController(context).StartSession(server.GameServerId, new StartSiegeSessionDto
                {
                    OperationId = id,
                    Actor = "admin",
                    ExpectedDurationMinutes = 240,
                    Mode = mode
                });
            }
            var starts = await Task.WhenAll(Start(firstId), Start(secondId));
            Assert.Equal(new[] { 201, 409 }, starts.Select(r => Assert.IsAssignableFrom<IStatusCodeActionResult>(r).StatusCode!.Value).Order());
            var session = await setup.SiegeSessions.SingleAsync();
            var claim = new ClaimSiegeActionDto
            {
                ActionId = Guid.NewGuid(),
                ConnectionId = Guid.NewGuid(),
                PlayerGuid = "guid",
                SlotId = 1,
                RuleId = "new-guid",
                BaselineRevision = 0
            };
            async Task<SiegeActionClaimDto> Claim()
            {
                await using var context = new PortalDbContext(options);
                var result = Assert.IsAssignableFrom<ObjectResult>(await new SiegeModeController(context)
                    .ClaimAction(server.GameServerId, session.SessionId, claim));
                return Assert.IsType<ApiResponse<SiegeActionClaimDto>>(result.Value).Data!;
            }
            var claims = await Task.WhenAll(Claim(), Claim());
            Assert.Equal(mode == SiegeSessionMode.Enforce ? 1 : 0, claims.Count(c => c.ShouldDispatch));
            Assert.Single(await setup.SiegeActions.ToListAsync());
            var summaryResult = Assert.IsAssignableFrom<ObjectResult>(await new SiegeModeController(setup).SummarizeActions(
                new SiegeAuditQueryDto { GameServerIds = [server.GameServerId], SkipEntries = 1, TakeEntries = 1 }));
            var summary = Assert.IsType<ApiResponse<SiegeActionSummaryDto>>(summaryResult.Value).Data!;
            var count = Assert.Single(summary.Counts);
            Assert.Equal(mode, count.Mode);
            Assert.Equal(mode == SiegeSessionMode.DryRun ? SiegeActionOutcomes.WouldKick : SiegeActionOutcomes.Pending, count.Outcome);
            Assert.Equal(1, count.Count);
            Assert.Empty(await setup.AdminActions.ToListAsync());
        });

    [Fact]
    public Task Maintenance_ExpiresAndRemovesTemporaryBaseline() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var server = new GameServer { Title = "Test", Hostname = "test", BanFileRootPath = "/", GameType = 1 };
            var player = new Player { PlayerId = Guid.NewGuid(), GameType = 1, Guid = "old", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            context.GameServers.Add(server);
            context.Players.Add(player);
            context.GlobalConfigurations.Add(CreateEnabledSiegeConfiguration());
            await context.SaveChangesAsync();
            var controller = new SiegeModeController(context);
            var sessionId = Guid.NewGuid();
            await controller.StartSession(server.GameServerId, new StartSiegeSessionDto
            {
                OperationId = sessionId,
                Actor = "admin",
                ExpectedDurationMinutes = 240,
                Mode = SiegeSessionMode.DryRun
            });
            var session = await context.SiegeSessions.SingleAsync();
            session.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var result = await controller.PruneSiegeMode();
            Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode);
            Assert.NotNull((await context.SiegeSessions.SingleAsync()).EndedAtUtc);
            Assert.Empty(await context.SiegeBaselineGuids.ToListAsync());
            Assert.Empty(await context.SiegeBaselineGuidOwners.ToListAsync());
            Assert.Single(await context.SiegeSessionEvents.Where(e => e.EventType == "Expired").ToListAsync());
        });

    [Fact]
    public Task Maintenance_PrunesOnlyAuditOlderThanOneYear() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var server = new GameServer { Title = "Test", Hostname = "test", BanFileRootPath = "/", GameType = 1 };
            context.GameServers.Add(server);
            await context.SaveChangesAsync();
            var oldId = Guid.NewGuid();
            var recentId = Guid.NewGuid();
            foreach (var (id, age) in new[] { (oldId, 366), (recentId, 364) })
            {
                var ended = DateTime.UtcNow.AddDays(-age);
                context.SiegeSessions.Add(new SiegeSession
                {
                    SessionId = id,
                    GameServerId = server.GameServerId,
                    GameType = 1,
                    StartedBy = "admin",
                    StartedAtUtc = ended.AddHours(-1),
                    ExpiresAtUtc = ended,
                    EndedAtUtc = ended,
                    Mode = "Enforce",
                    SettingsJson = "{}"
                });
                context.SiegeActions.Add(new SiegeAction
                {
                    ActionId = Guid.NewGuid(),
                    SessionId = id,
                    ConnectionId = Guid.NewGuid(),
                    PlayerGuid = "guid",
                    SlotId = 1,
                    RuleId = "new-guid",
                    ClaimedAtUtc = ended.AddMinutes(-1),
                    Outcome = "Failed"
                });
                context.SiegeChecks.Add(new SiegeCheck
                {
                    CheckId = Guid.NewGuid(),
                    SessionId = id,
                    OccurredAtUtc = ended.AddMinutes(-1),
                    Outcome = "Unknown",
                    Detail = "test"
                });
                context.SiegeSessionEvents.Add(new SiegeSessionEvent
                {
                    OperationId = Guid.NewGuid(),
                    SessionId = id,
                    EventType = "Ended",
                    Actor = "admin",
                    OccurredAtUtc = ended,
                    ExpiresAtUtc = ended
                });
            }
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var result = await new SiegeModeController(context).PruneSiegeMode();
            Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode);
            Assert.Equal(recentId, (await context.SiegeSessions.SingleAsync()).SessionId);
            Assert.Equal(recentId, (await context.SiegeActions.SingleAsync()).SessionId);
            Assert.Equal(recentId, (await context.SiegeChecks.SingleAsync()).SessionId);
            Assert.Equal(recentId, (await context.SiegeSessionEvents.SingleAsync()).SessionId);
        });

    [Fact]
    public Task DeletePlayer_RemovesOnlyOwnedSnapshotValuesAndRejectsStaleClaims() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var server = new GameServer { Title = "Test", Hostname = "test", BanFileRootPath = "/", GameType = 1 };
            var deleted = new Player { PlayerId = Guid.NewGuid(), GameType = 1, Guid = "deleted", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            var retained = new Player { PlayerId = Guid.NewGuid(), GameType = 1, Guid = "retained", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            context.GameServers.Add(server);
            context.Players.AddRange(deleted, retained);
            context.GlobalConfigurations.Add(CreateEnabledSiegeConfiguration());
            context.PlayerIpAddresses.AddRange(
                new PlayerIpAddress { PlayerId = deleted.PlayerId, Address = "192.0.2.1", Added = DateTime.UtcNow, LastUsed = DateTime.UtcNow },
                new PlayerIpAddress { PlayerId = deleted.PlayerId, Address = "192.0.2.2", Added = DateTime.UtcNow, LastUsed = DateTime.UtcNow },
                new PlayerIpAddress { PlayerId = retained.PlayerId, Address = "192.0.2.2", Added = DateTime.UtcNow, LastUsed = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var sessionId = Guid.NewGuid();
            var controller = new SiegeModeController(context);
            await controller.StartSession(server.GameServerId, new StartSiegeSessionDto
            {
                OperationId = sessionId,
                Actor = "admin",
                ExpectedDurationMinutes = 240,
                Mode = SiegeSessionMode.Enforce
            });
            var request = new ClaimSiegeActionDto
            {
                ActionId = Guid.NewGuid(),
                ConnectionId = Guid.NewGuid(),
                PlayerGuid = "deleted",
                SlotId = 1,
                RuleId = "new-guid",
                BaselineRevision = 0
            };
            await controller.ClaimAction(server.GameServerId, sessionId, request);
            var beforeDeletion = Assert.IsAssignableFrom<ObjectResult>(await controller.SummarizeActions(
                new SiegeAuditQueryDto { GameServerIds = [server.GameServerId] }));
            Assert.Equal(1, Assert.IsType<ApiResponse<SiegeActionSummaryDto>>(beforeDeletion.Value).Data!.Counts.Sum(c => c.Count));
            await controller.RecordCheck(server.GameServerId, sessionId, new RecordSiegeCheckDto
            {
                CheckId = Guid.NewGuid(),
                ConnectionId = request.ConnectionId,
                PlayerGuid = "deleted",
                Outcome = "Unknown",
                Detail = "test"
            });
            await context.PlayerIpAddresses.Where(ip => ip.PlayerId == deleted.PlayerId && ip.Address == "192.0.2.1").ExecuteDeleteAsync();
            context.ChangeTracker.Clear();

            var deletion = await ((IDataMaintenanceApi)new DataMaintenanceController(context, new ConfigurationBuilder().Build()))
                .DeletePlayer(deleted.PlayerId);
            Assert.Equal(System.Net.HttpStatusCode.OK, deletion.StatusCode);
            context.ChangeTracker.Clear();
            Assert.Empty(await context.SiegeActions.ToListAsync());
            Assert.Empty(await context.SiegeChecks.ToListAsync());
            var afterDeletion = Assert.IsAssignableFrom<ObjectResult>(await controller.SummarizeActions(
                new SiegeAuditQueryDto { GameServerIds = [server.GameServerId] }));
            Assert.Empty(Assert.IsType<ApiResponse<SiegeActionSummaryDto>>(afterDeletion.Value).Data!.Counts);
            Assert.Empty(await context.SiegeBaselineGuidOwners.Where(o => o.PlayerId == deleted.PlayerId).ToListAsync());
            Assert.Empty(await context.SiegeBaselineIpOwners.Where(o => o.PlayerId == deleted.PlayerId).ToListAsync());

            var result = Assert.IsAssignableFrom<ObjectResult>(await controller.GetBaseline(server.GameServerId, sessionId,
                new SiegeBaselineRequestDto { PlayerGuid = "deleted", IpAddress = "192.0.2.1" }));
            var baseline = Assert.IsType<ApiResponse<SiegeBaselineDto>>(result.Value).Data!;
            Assert.True(baseline.NewGuid);
            Assert.True(baseline.NewIp);
            Assert.Equal(1, baseline.BaselineRevision);
            var shared = Assert.IsAssignableFrom<ObjectResult>(await controller.GetBaseline(server.GameServerId, sessionId,
                new SiegeBaselineRequestDto { PlayerGuid = "retained", IpAddress = "192.0.2.2" }));
            var sharedBaseline = Assert.IsType<ApiResponse<SiegeBaselineDto>>(shared.Value).Data!;
            Assert.False(sharedBaseline.NewGuid);
            Assert.False(sharedBaseline.NewIp);
            var stale = await controller.ClaimAction(server.GameServerId, sessionId, request with { ActionId = Guid.NewGuid() });
            Assert.Equal(409, Assert.IsAssignableFrom<IStatusCodeActionResult>(stale).StatusCode);
        });

    [Fact]
    public Task ConcurrentActivationAndDeletion_DoNotRetainDeletedHistory() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var server = new GameServer { Title = "Test", Hostname = "test", BanFileRootPath = "/", GameType = 1 };
            var player = new Player { PlayerId = Guid.NewGuid(), GameType = 1, Guid = "deleted", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.GameServers.Add(server);
            setup.Players.Add(player);
            setup.GlobalConfigurations.Add(CreateEnabledSiegeConfiguration());
            setup.PlayerIpAddresses.Add(new PlayerIpAddress
            {
                PlayerId = player.PlayerId,
                Address = "192.0.2.8",
                Added = DateTime.UtcNow,
                LastUsed = DateTime.UtcNow
            });
            await setup.SaveChangesAsync();
            var sessionId = Guid.NewGuid();

            async Task Start()
            {
                await using var context = new PortalDbContext(options);
                var result = await new SiegeModeController(context).StartSession(server.GameServerId, new StartSiegeSessionDto
                {
                    OperationId = sessionId,
                    Actor = "admin",
                    ExpectedDurationMinutes = 240,
                    Mode = SiegeSessionMode.Enforce
                });
                Assert.Equal(201, Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode);
            }
            async Task Delete()
            {
                await using var context = new PortalDbContext(options);
                var result = await ((IDataMaintenanceApi)new DataMaintenanceController(context, new ConfigurationBuilder().Build()))
                    .DeletePlayer(player.PlayerId);
                Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
            }
            await Task.WhenAll(Start(), Delete());
            setup.ChangeTracker.Clear();
            Assert.Empty(await setup.SiegeBaselineGuids.ToListAsync());
            Assert.Empty(await setup.SiegeBaselineIps.ToListAsync());
            Assert.Empty(await setup.SiegeBaselineGuidOwners.ToListAsync());
            Assert.Empty(await setup.SiegeBaselineIpOwners.ToListAsync());
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task PlayerDeletionAndBaselineRemoval_ShareHistoryLock(bool prune) =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var server = new GameServer { Title = "Test", Hostname = "test", BanFileRootPath = "/", GameType = 1 };
            var player = new Player { PlayerId = Guid.NewGuid(), GameType = 1, Guid = "deleted", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.GameServers.Add(server);
            setup.Players.Add(player);
            setup.GlobalConfigurations.Add(CreateEnabledSiegeConfiguration());
            setup.PlayerIpAddresses.Add(new PlayerIpAddress
            {
                PlayerId = player.PlayerId,
                Address = "192.0.2.8",
                Added = DateTime.UtcNow,
                LastUsed = DateTime.UtcNow
            });
            await setup.SaveChangesAsync();
            var sessionId = Guid.NewGuid();
            await new SiegeModeController(setup).StartSession(server.GameServerId, new StartSiegeSessionDto
            {
                OperationId = sessionId,
                Actor = "admin",
                ExpectedDurationMinutes = 240,
                Mode = SiegeSessionMode.Enforce
            });
            if (prune)
            {
                var session = await setup.SiegeSessions.SingleAsync();
                session.EndedAtUtc = DateTime.UtcNow;
                await setup.SaveChangesAsync();
            }

            await using var lockContext = new PortalDbContext(options);
            await using var transaction = await lockContext.Database.BeginTransactionAsync();
            await lockContext.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource = 'siege-history-snapshot',
                    @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
                IF @result < 0 THROW 50000, 'Could not acquire test lock', 1;
                """);
            async Task<int?> RemoveBaseline()
            {
                await using var context = new PortalDbContext(options);
                var controller = new SiegeModeController(context);
                var result = prune
                    ? await controller.PruneSiegeMode()
                    : await controller.EndSession(server.GameServerId, sessionId,
                        new ChangeSiegeSessionDto { OperationId = Guid.NewGuid(), Actor = "admin" });
                return Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode;
            }
            async Task<System.Net.HttpStatusCode> Delete()
            {
                await using var context = new PortalDbContext(options);
                return (await ((IDataMaintenanceApi)new DataMaintenanceController(context, new ConfigurationBuilder().Build()))
                    .DeletePlayer(player.PlayerId)).StatusCode;
            }

            var removal = RemoveBaseline();
            var deletion = Delete();
            try
            {
                Assert.NotSame(removal, await Task.WhenAny(removal, Task.Delay(500)));
                Assert.NotSame(deletion, await Task.WhenAny(deletion, Task.Delay(500)));
            }
            finally
            {
                await transaction.CommitAsync();
            }
            Assert.Equal(200, await removal);
            Assert.Equal(System.Net.HttpStatusCode.OK, await deletion);
            setup.ChangeTracker.Clear();
            Assert.Empty(await setup.SiegeBaselineGuidOwners.ToListAsync());
            Assert.Empty(await setup.SiegeBaselineIpOwners.ToListAsync());
            Assert.Empty(await setup.SiegeBaselineGuids.ToListAsync());
            Assert.Empty(await setup.SiegeBaselineIps.ToListAsync());
        });

    [Fact]
    public Task EndAndClaim_AreSerializedAndLaterClaimsFail() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var server = new GameServer { Title = "Test", Hostname = "test", BanFileRootPath = "/", GameType = 1 };
            setup.GameServers.Add(server);
            setup.GlobalConfigurations.Add(CreateEnabledSiegeConfiguration());
            await setup.SaveChangesAsync();
            var sessionId = Guid.NewGuid();
            await new SiegeModeController(setup).StartSession(server.GameServerId, new StartSiegeSessionDto
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
                PlayerGuid = "guid",
                SlotId = 1,
                RuleId = "new-guid",
                BaselineRevision = 0
            };
            async Task<int?> End()
            {
                await using var context = new PortalDbContext(options);
                var result = await new SiegeModeController(context).EndSession(server.GameServerId, sessionId,
                    new ChangeSiegeSessionDto { OperationId = Guid.NewGuid(), Actor = "admin" });
                return Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode;
            }
            async Task<int?> Claim(ClaimSiegeActionDto request)
            {
                await using var context = new PortalDbContext(options);
                return Assert.IsAssignableFrom<IStatusCodeActionResult>(await new SiegeModeController(context)
                    .ClaimAction(server.GameServerId, sessionId, request)).StatusCode;
            }
            var results = await Task.WhenAll(End(), Claim(claim));
            Assert.Equal(200, results[0]);
            Assert.Contains(results[1], new int?[] { 200, 409 });
            Assert.Equal(409, await Claim(claim with { ActionId = Guid.NewGuid(), ConnectionId = Guid.NewGuid() }));
        });

    private static GlobalConfiguration CreateEnabledSiegeConfiguration() => new()
    {
        Namespace = SiegeModeSettingsConstants.Namespace,
        Configuration = JsonSerializer.Serialize(
            new SiegeModeSettingsDocument
            {
                Enabled = true,
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
                            Parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                                """{"operator":"IsNew","referenceScope":"ObservedHistory"}""")
                        },
                        Actions =
                        [
                            new RuleActionDefinition
                            {
                                Id = "kick",
                                TypeId = "player.kick",
                                Parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                                    """{"reason":"test"}""")!
                            }
                        ]
                    }
                ]
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        LastModifiedUtc = DateTime.UtcNow
    };
}
