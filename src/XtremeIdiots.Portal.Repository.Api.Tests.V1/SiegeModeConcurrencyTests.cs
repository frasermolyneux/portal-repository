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
    [Fact]
    public Task Maintenance_PrunesLegacyActionsInBatches() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            await CreateLegacySiegeActionsTableAsync(context);
            var server = new GameServer { Title = "Test", Hostname = "test", BanFileRootPath = "/", GameType = 1 };
            context.GameServers.Add(server);
            var oldId = Guid.NewGuid();
            var recentId = Guid.NewGuid();
            foreach (var (id, age) in new[] { (oldId, 366), (recentId, 364) })
            {
                var ended = DateTime.UtcNow.AddDays(-age);
                context.SiegeSessions.Add(new SiegeSession
                {
                    SessionId = id,
                    GameServer = server,
                    GameType = 1,
                    StartedBy = "admin",
                    StartedAtUtc = ended.AddHours(-1),
                    ExpiresAtUtc = ended,
                    EndedAtUtc = ended,
                    Mode = "Enforce",
                    SettingsJson = "{}"
                });
            }
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [dbo].[SiegeActions] ([ActionId], [SessionId], [PlayerGuid])
                SELECT TOP (1001) NEWID(), {oldId}, N'old'
                FROM sys.all_objects AS a CROSS JOIN sys.all_objects AS b;
                INSERT INTO [dbo].[SiegeActions] ([ActionId], [SessionId], [PlayerGuid])
                VALUES ({Guid.NewGuid()}, {recentId}, N'recent');
                """);

            var result = await new SiegeModeController(context).PruneSiegeMode();

            Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode);
            Assert.Equal(recentId, (await context.SiegeSessions.SingleAsync()).SessionId);
            var remaining = await context.Database.SqlQuery<Guid>($"""
                SELECT [SessionId] AS [Value] FROM [dbo].[SiegeActions]
                """).ToListAsync();
            Assert.Equal(recentId, Assert.Single(remaining));
        });

    [Fact]
    public Task DeletePlayer_LegacyCleanupIsScopedAndRollsBackOnFailure() =>
        SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            await CreateLegacySiegeActionsTableAsync(context);
            const string playerGuid = "deleted'--";
            var player = new Player { PlayerId = Guid.NewGuid(), GameType = 1, Guid = playerGuid, FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            var first = new GameServer { Title = "First", Hostname = "first", BanFileRootPath = "/", GameType = 1 };
            var second = new GameServer { Title = "Second", Hostname = "second", BanFileRootPath = "/", GameType = 2 };
            var firstSession = Guid.NewGuid();
            var secondSession = Guid.NewGuid();
            context.Players.Add(player);
            context.GameServers.AddRange(first, second);
            foreach (var (id, server) in new[] { (firstSession, first), (secondSession, second) })
            {
                context.SiegeSessions.Add(new SiegeSession
                {
                    SessionId = id,
                    GameServer = server,
                    GameType = server.GameType,
                    StartedBy = "admin",
                    StartedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                    Mode = "Enforce",
                    SettingsJson = "{}"
                });
            }
            await context.SaveChangesAsync();
            var removedAction = Guid.NewGuid();
            var retainedAction = Guid.NewGuid();
            var otherGameAction = Guid.NewGuid();
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [dbo].[SiegeActions] ([ActionId], [SessionId], [PlayerGuid]) VALUES
                    ({removedAction}, {firstSession}, {playerGuid}),
                    ({retainedAction}, {firstSession}, {"retained"}),
                    ({otherGameAction}, {secondSession}, {playerGuid});
                CREATE TABLE [dbo].[DeletePlayerBlocker]
                    ([PlayerId] uniqueidentifier NOT NULL REFERENCES [dbo].[Players] ([PlayerId]));
                INSERT INTO [dbo].[DeletePlayerBlocker] ([PlayerId]) VALUES ({player.PlayerId});
                """);
            var api = (IDataMaintenanceApi)new DataMaintenanceController(context, new ConfigurationBuilder().Build());

            await Assert.ThrowsAsync<DbUpdateException>(() => api.DeletePlayer(player.PlayerId));
            context.ChangeTracker.Clear();
            var beforeRetry = await context.Database.SqlQuery<Guid>($"""
                SELECT [ActionId] AS [Value] FROM [dbo].[SiegeActions]
                """).ToListAsync();
            Assert.Contains(removedAction, beforeRetry);
            Assert.True(await context.Players.AnyAsync(p => p.PlayerId == player.PlayerId));

            await context.Database.ExecuteSqlRawAsync("DROP TABLE [dbo].[DeletePlayerBlocker]");
            var result = await api.DeletePlayer(player.PlayerId);
            var remaining = await context.Database.SqlQuery<Guid>($"""
                SELECT [ActionId] AS [Value] FROM [dbo].[SiegeActions]
                """).ToListAsync();
            Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(2, remaining.Count);
            Assert.Contains(retainedAction, remaining);
            Assert.Contains(otherGameAction, remaining);
            Assert.DoesNotContain(removedAction, remaining);
        });

    [Theory]
    [InlineData(SiegeSessionMode.Enforce)]
    [InlineData(SiegeSessionMode.DryRun)]
    public Task ConcurrentStarts_AreSerialized(SiegeSessionMode mode) =>
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
            Assert.Single(await setup.SiegeSessions.ToListAsync());
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
            Assert.Equal(recentId, (await context.SiegeChecks.SingleAsync()).SessionId);
            Assert.Equal(recentId, (await context.SiegeSessionEvents.SingleAsync()).SessionId);
        });

    [Fact]
    public Task DeletePlayer_RemovesOnlyOwnedSnapshotValues() =>
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
            var connectionId = Guid.NewGuid();
            await controller.RecordCheck(server.GameServerId, sessionId, new RecordSiegeCheckDto
            {
                CheckId = Guid.NewGuid(),
                ConnectionId = connectionId,
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
            Assert.Empty(await context.SiegeChecks.ToListAsync());
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
    public Task EndAndCheck_AreSerializedAndLaterChecksFail() =>
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
            var check = new RecordSiegeCheckDto
            {
                CheckId = Guid.NewGuid(),
                ConnectionId = Guid.NewGuid(),
                PlayerGuid = "guid",
                Outcome = "Unknown",
                Detail = "test"
            };
            async Task<int?> End()
            {
                await using var context = new PortalDbContext(options);
                var result = await new SiegeModeController(context).EndSession(server.GameServerId, sessionId,
                    new ChangeSiegeSessionDto { OperationId = Guid.NewGuid(), Actor = "admin" });
                return Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode;
            }
            async Task<int?> Check(RecordSiegeCheckDto request)
            {
                await using var context = new PortalDbContext(options);
                return Assert.IsAssignableFrom<IStatusCodeActionResult>(await new SiegeModeController(context)
                    .RecordCheck(server.GameServerId, sessionId, request)).StatusCode;
            }
            var results = await Task.WhenAll(End(), Check(check));
            Assert.Equal(200, results[0]);
            Assert.Contains(results[1], new int?[] { 200, 409 });
            Assert.Equal(409, await Check(check with { CheckId = Guid.NewGuid(), ConnectionId = Guid.NewGuid() }));
        });

    private static Task CreateLegacySiegeActionsTableAsync(PortalDbContext context) =>
        context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE [dbo].[SiegeActions]
            (
                [ActionId] uniqueidentifier NOT NULL PRIMARY KEY,
                [SessionId] uniqueidentifier NOT NULL REFERENCES [dbo].[SiegeSessions] ([SessionId]) ON DELETE CASCADE,
                [PlayerGuid] nvarchar(50) NOT NULL
            );
            """);

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
