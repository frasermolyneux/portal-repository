using System.Net;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Moq;
using MX.Observability.ApplicationInsights.Auditing;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1;

[Trait("Category", "SqlIntegration")]
[Collection("SqlServer")]
public class ConnectedPlayerClaimsConcurrencyTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public Task RetainedBoundaryAtLastConfirmationSeparatesSubsequentPresence(bool sweepFirst, bool rosterAbsence)
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var server = new GameServer { GameType = 2, Title = "Test", Hostname = "test", BanFileRootPath = "" };
            var player = new Player { GameType = 2, Guid = "guid-1", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            context.AddRange(server, player);
            await context.SaveChangesAsync();
            var start = DateTime.UtcNow.AddSeconds(-40);
            var join = new PlaytimeObservationDto
            {
                ObservationId = Guid.NewGuid(),
                ServerId = server.GameServerId,
                GameType = GameType.CallOfDuty4,
                CanonicalPlayerId = player.PlayerId,
                GamePlayerId = "guid-1",
                ConnectionCorrelationId = Guid.NewGuid(),
                Kind = PlaytimeObservationKind.PlayerJoined,
                EventUtc = start,
                TimestampQuality = PlaytimeTimestampQuality.ProducerTimestamp
            };
            var confirmation = join with { ObservationId = Guid.NewGuid(), EventUtc = start.AddSeconds(10) };
            var boundary = rosterAbsence
                ? new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = server.GameServerId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = confirmation.EventUtc,
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Complete,
                    Roster = []
                }
                : confirmation with
                {
                    ObservationId = Guid.NewGuid(),
                    Kind = PlaytimeObservationKind.PlayerLeft,
                    CanonicalPlayerId = null
                };
            var nextPresence = join with { ObservationId = Guid.NewGuid(), EventUtc = start.AddSeconds(20) };
            var api = (IPlaytimeApi)new PlaytimeController(context);
            await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [join, confirmation] });
            if (sweepFirst)
            {
                await PlaytimeSweeperHostedService.SweepAsync(context, start.AddSeconds(11), CancellationToken.None);
            }
            await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [boundary, nextPresence] });
            var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
            Assert.Equal(2, sessions.Count);
            Assert.Equal(confirmation.EventUtc, sessions[0].EndUtc);
            Assert.Equal(!rosterAbsence, sessions[0].IsFinal);
            Assert.Equal(rosterAbsence ? (byte)2 : (byte)1, sessions[0].ClosureReason);
            Assert.Equal(nextPresence.EventUtc, sessions[1].StartUtc);
            Assert.Null(sessions[1].EndUtc);
        });

    [Fact]
    public Task PlayerDeletionCoordinatesIngestionOnServerCreatedDuringDeletion()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var player = new Player { GameType = 2, Guid = "deleted-id", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.Players.Add(player);
            await setup.SaveChangesAsync();

            var gate = new ServerListGate();
            await using var deletionDb = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(gate).Options);
            await using var ingestionDb = new PortalDbContext(options);
            var deletion = ((IDataMaintenanceApi)new DataMaintenanceController(deletionDb, new ConfigurationBuilder().Build()))
                .DeletePlayer(player.PlayerId);
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            var server = new GameServer { GameType = 2, Title = "New", Hostname = "new", BanFileRootPath = "" };
            await using var serverDb = new PortalDbContext(options);
            serverDb.GameServers.Add(server);
            await serverDb.SaveChangesAsync();
            var ingestion = ((IPlaytimeApi)new PlaytimeController(ingestionDb))
                .BatchObservations(new PlaytimeObservationBatchDto
                {
                    Observations =
                    [
                        new PlaytimeObservationDto
                        {
                            ObservationId = Guid.NewGuid(), ServerId = server.GameServerId,
                            GameType = GameType.CallOfDuty4, Kind = PlaytimeObservationKind.PlayerJoined,
                            EventUtc = DateTime.UtcNow, TimestampQuality = PlaytimeTimestampQuality.ProducerTimestamp,
                            CanonicalPlayerId = player.PlayerId, GamePlayerId = "deleted-id"
                        }
                    ]
                });
            try
            {
                Assert.NotSame(ingestion, await Task.WhenAny(ingestion, Task.Delay(300)));
            }
            finally
            {
                gate.Release.TrySetResult();
            }
            Assert.Equal(HttpStatusCode.OK, (await deletion).StatusCode);
            Assert.Equal(PlaytimeObservationOutcome.Rejected, (await ingestion).Result?.Data?.Items.Single().Outcome);
            Assert.Empty(await setup.PlaytimeObservations.AsNoTracking().ToListAsync());
        });

    private sealed class ServerListGate : DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM [GameServers]", StringComparison.Ordinal))
            {
                Entered.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }

    [Fact]
    public Task JoinReplayAndFinalizedBoundaryReconcileAgainstSqlServer()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var server = new GameServer { GameType = 2, Title = "Test", Hostname = "test", BanFileRootPath = "" };
            var player = new Player { GameType = 2, FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            context.AddRange(server, player);
            await context.SaveChangesAsync();
            var start = DateTime.UtcNow.AddMinutes(-30);
            var laterJoin = new PlaytimeObservationDto
            {
                ObservationId = Guid.NewGuid(),
                ServerId = server.GameServerId,
                GameType = GameType.CallOfDuty4,
                CanonicalPlayerId = player.PlayerId,
                Kind = PlaytimeObservationKind.PlayerJoined,
                EventUtc = start.AddMinutes(10),
                TimestampQuality = PlaytimeTimestampQuality.ProducerTimestamp
            };
            var earlierJoin = laterJoin with { ObservationId = Guid.NewGuid(), EventUtc = start };
            var leave = laterJoin with
            {
                ObservationId = Guid.NewGuid(),
                EventUtc = start.AddMinutes(20),
                Kind = PlaytimeObservationKind.PlayerLeft
            };
            var reconnect = laterJoin with { ObservationId = Guid.NewGuid(), EventUtc = leave.EventUtc };
            var api = (IPlaytimeApi)new PlaytimeController(context);
            var batch = await api.BatchObservations(new PlaytimeObservationBatchDto
            {
                Observations = [laterJoin, leave, earlierJoin, reconnect]
            });
            var replay = await api.BatchObservations(new PlaytimeObservationBatchDto
            {
                Observations = [laterJoin with { IsBot = false }]
            });
            Assert.All(batch.Result!.Data!.Items, item => Assert.Equal(PlaytimeObservationOutcome.Accepted, item.Outcome));
            Assert.Equal(PlaytimeObservationOutcome.Duplicate, replay.Result?.Data?.Items.Single().Outcome);
            var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
            Assert.Equal(2, sessions.Count);
            Assert.Equal(start, sessions[0].StartUtc);
            Assert.Equal(leave.EventUtc, sessions[0].EndUtc);
            Assert.False(sessions[0].IsBot);
            Assert.Equal(reconnect.EventUtc, sessions[1].StartUtc);
            Assert.Null(sessions[1].EndUtc);
        });

    [Fact]
    public Task ConcurrentDifferentServersReusingObservationIdReturnConflict()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var firstServer = new GameServer { GameType = 2, Title = "First", Hostname = "first", BanFileRootPath = "" };
            var secondServer = new GameServer { GameType = 2, Title = "Second", Hostname = "second", BanFileRootPath = "" };
            setup.AddRange(firstServer, secondServer);
            await setup.SaveChangesAsync();

            var gate = new CommitGate();
            await using var firstDb = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString).AddInterceptors(gate).Options);
            await using var secondDb = new PortalDbContext(options);
            var observation = new PlaytimeObservationDto
            {
                ObservationId = Guid.NewGuid(),
                ServerId = firstServer.GameServerId,
                GameType = GameType.CallOfDuty4,
                Kind = PlaytimeObservationKind.ServerRoster,
                EventUtc = DateTime.UtcNow,
                TimestampQuality = PlaytimeTimestampQuality.ParseTime,
                RosterCompleteness = PlaytimeRosterCompleteness.Partial
            };
            var first = ((IPlaytimeApi)new PlaytimeController(firstDb))
                .BatchObservations(new PlaytimeObservationBatchDto { Observations = [observation] });
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            var second = ((IPlaytimeApi)new PlaytimeController(secondDb))
                .BatchObservations(new PlaytimeObservationBatchDto { Observations = [observation with { ServerId = secondServer.GameServerId }] });
            try
            {
                await Task.Delay(250);
            }
            finally
            {
                gate.Release.TrySetResult();
            }
            var firstResult = await first;
            var secondResult = await second;
            Assert.Equal(PlaytimeObservationOutcome.Accepted, firstResult.Result?.Data?.Items.Single().Outcome);
            Assert.Equal(PlaytimeObservationOutcome.Rejected, secondResult.Result?.Data?.Items.Single().Outcome);
        });

    private sealed class CommitGate : DbTransactionInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction, TransactionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return result;
        }
    }

    [Fact]
    public Task PlaytimeTotals_SumsFractionalSegmentsAndTracksUnclassifiedPresence()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var context = new PortalDbContext(options);
            var server = new GameServer { GameType = 2, Title = "Test", Hostname = "test", BanFileRootPath = "" };
            var human = new Player { GameType = 2, FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            var unknown = new Player { GameType = 2, FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            context.AddRange(server, human, unknown);
            await context.SaveChangesAsync();
            var start = DateTime.UtcNow.AddMinutes(-1);
            var humanSession = new PlayerSession
            {
                SessionId = Guid.NewGuid(),
                PlayerId = human.PlayerId,
                ServerId = server.GameServerId,
                GameType = 2,
                IsBot = false,
                StartUtc = start,
                LastConfirmedUtc = start.AddSeconds(2),
                StartQuality = 2
            };
            var unknownSession = new PlayerSession
            {
                SessionId = Guid.NewGuid(),
                PlayerId = unknown.PlayerId,
                ServerId = server.GameServerId,
                GameType = 2,
                IsBot = null,
                StartUtc = start,
                LastConfirmedUtc = start.AddSeconds(1),
                StartQuality = 2
            };
            context.AddRange(humanSession, unknownSession);
            context.PlayerSessionMapSegments.AddRange(
                new PlayerSessionMapSegment
                {
                    SegmentId = Guid.NewGuid(),
                    SessionId = humanSession.SessionId,
                    StartUtc = start,
                    EndUtc = start.AddMilliseconds(600),
                    UnknownMap = true
                },
                new PlayerSessionMapSegment
                {
                    SegmentId = Guid.NewGuid(),
                    SessionId = humanSession.SessionId,
                    StartUtc = start.AddSeconds(1),
                    EndUtc = start.AddMilliseconds(1600),
                    UnknownMap = true
                },
                new PlayerSessionMapSegment
                {
                    SegmentId = Guid.NewGuid(),
                    SessionId = unknownSession.SessionId,
                    StartUtc = start,
                    EndUtc = start.AddMilliseconds(100),
                    UnknownMap = true
                });
            var epoch = Guid.NewGuid();
            var reportedUtc = start.AddSeconds(5);
            context.PlaytimeObservations.Add(new PlaytimeObservation
            {
                ObservationId = Guid.NewGuid(),
                ServerId = server.GameServerId,
                GameType = 2,
                Kind = 4,
                EventUtc = start,
                ReceivedUtc = start,
                TimestampQuality = 4,
                ProducerEpoch = epoch,
                CoverageReportedUtc = reportedUtc,
                DroppedCapacityMax = 1,
                PayloadHash = new byte[32],
                Outcome = 1
            });
            await context.SaveChangesAsync();

            var result = await ((IPlaytimeApi)new PlaytimeController(context))
                .GetTotals(start, start.AddSeconds(3), null, server.GameServerId, GameType.CallOfDuty4, null);
            Assert.Equal(1, result.Result?.Data?.UnknownMapSeconds);
            Assert.Equal(0, result.Result?.Data?.UnclassifiedPlayerSeconds);
            Assert.False(result.Result?.Data?.HumanOnlyCoverage);
            Assert.Equal(reportedUtc, Assert.Single(result.Result!.Data!.CoverageReports).ReportedUtc);
        });

    [Fact]
    public Task DeletePlayer_RetriesAfterRolledBackRedaction()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var player = new Player { GameType = 2, Guid = "deleted-id", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            var server = new GameServer { GameType = 2, Title = "Test", Hostname = "test", BanFileRootPath = "" };
            setup.AddRange(player, server);
            await setup.SaveChangesAsync();
            var observationId = Guid.NewGuid();
            setup.PlaytimeObservations.Add(new PlaytimeObservation
            {
                ObservationId = observationId,
                ServerId = server.GameServerId,
                GameType = 2,
                Kind = 1,
                EventUtc = DateTime.UtcNow,
                ReceivedUtc = DateTime.UtcNow,
                TimestampQuality = 2,
                CanonicalPlayerId = player.PlayerId,
                GamePlayerId = "deleted-id",
                ResolvedRosterJson = "[]",
                PayloadHash = new byte[32],
                Outcome = 1
            });
            await setup.SaveChangesAsync();

            var fault = new RollbackCommitFault();
            await using var deletionDb = new PortalDbContext(new DbContextOptionsBuilder<PortalDbContext>()
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(2, TimeSpan.Zero, null))
                .AddInterceptors(fault).Options);
            var result = await ((IDataMaintenanceApi)new DataMaintenanceController(deletionDb, new ConfigurationBuilder().Build()))
                .DeletePlayer(player.PlayerId);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(1, fault.Failures);
            await using var verify = new PortalDbContext(options);
            Assert.False(await verify.Players.AnyAsync(x => x.PlayerId == player.PlayerId));
            var observation = await verify.PlaytimeObservations.SingleAsync(x => x.ObservationId == observationId);
            Assert.True(observation.IdentifiersRedacted);
            Assert.Null(observation.CanonicalPlayerId);
            Assert.Null(observation.GamePlayerId);
        });

    private sealed class RollbackCommitFault : DbTransactionInterceptor
    {
        public int Failures { get; private set; }

        public override ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction, TransactionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            if (Failures == 0)
            {
                Failures++;
                throw new TimeoutException("Simulated failure before commit.");
            }

            return ValueTask.FromResult(result);
        }
    }

    [Fact]
    public Task DeletePlayer_WaitsForInFlightClaimBeforeReadingDependents()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var user = new UserProfile();
            var player = new Player { FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.AddRange(user, player);
            await setup.SaveChangesAsync();
            var code = new ConnectedPlayerActivationCode
            {
                UserProfileId = user.UserProfileId,
                Code = "ABC123",
                CodeHash = "A",
                ActivatedBy = "ApprovalRequired",
                IsActive = true,
                ActivatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                MaxAttempts = 5
            };
            setup.Add(code);
            await setup.SaveChangesAsync();

            await using var transaction = await setup.Database.BeginTransactionAsync();
            await setup.Database.ExecuteSqlRawAsync("EXEC sys.sp_getapplock @Resource = N'Portal.UserProfileLifecycle', @LockMode = N'Exclusive', @LockOwner = N'Transaction'");
            await using var deletionDb = new PortalDbContext(options);
            IDataMaintenanceApi maintenance = new DataMaintenanceController(deletionDb, new ConfigurationBuilder().Build());
            var deletion = maintenance.DeletePlayer(player.PlayerId);
            await Task.Delay(250);
            Assert.False(deletion.IsCompleted);
            setup.ConnectedPlayerClaims.Add(new ConnectedPlayerClaim
            {
                ConnectedPlayerClaimId = Guid.NewGuid(),
                ConnectedPlayerActivationCodeId = code.ConnectedPlayerActivationCodeId,
                UserProfileId = user.UserProfileId,
                PlayerId = player.PlayerId,
                GameServerId = Guid.NewGuid(),
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
            });
            await setup.SaveChangesAsync();
            await transaction.CommitAsync();
            Assert.Equal(HttpStatusCode.OK, (await deletion).StatusCode);
            await using var verify = new PortalDbContext(options);
            Assert.Empty(await verify.ConnectedPlayerClaims.Where(c => c.PlayerId == player.PlayerId).ToListAsync());
        });

    [Fact]
    public Task ConcurrentApprovals_OnlyOneMemberOwnsPlayer()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var firstUser = new UserProfile();
            var secondUser = new UserProfile();
            var player = new Player { FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.AddRange(firstUser, secondUser, player);
            foreach (var name in new[] { "verified-player", "senior-admin", "head-admin", "game-admin", "moderator", "clan-member" })
            {
                setup.Tags.Add(new Tag { Name = name, UserDefined = false });
            }
            await setup.SaveChangesAsync();

            var firstCode = new ConnectedPlayerActivationCode
            {
                UserProfileId = firstUser.UserProfileId,
                Code = "ABC123",
                CodeHash = "A",
                ActivatedBy = "ApprovalRequired",
                IsActive = true,
                ActivatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                MaxAttempts = 5
            };
            var secondCode = new ConnectedPlayerActivationCode
            {
                UserProfileId = secondUser.UserProfileId,
                Code = "DEF456",
                CodeHash = "B",
                ActivatedBy = "ApprovalRequired",
                IsActive = true,
                ActivatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                MaxAttempts = 5
            };
            setup.AddRange(firstCode, secondCode);
            await setup.SaveChangesAsync();
            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            var serverId = Guid.NewGuid();
            await using var firstDb = new PortalDbContext(options);
            await using var secondDb = new PortalDbContext(options);
            IConnectedPlayersApi first = new ConnectedPlayersController(firstDb, Mock.Of<IAuditLogger>());
            IConnectedPlayersApi second = new ConnectedPlayersController(secondDb, Mock.Of<IAuditLogger>());
            Assert.Equal(HttpStatusCode.Created, (await first.SubmitConnectedPlayerClaim(new SubmitConnectedPlayerClaimDto
            { RequestId = firstId, PlayerId = player.PlayerId, GameServerId = serverId, Code = firstCode.Code })).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await second.SubmitConnectedPlayerClaim(new SubmitConnectedPlayerClaimDto
            { RequestId = secondId, PlayerId = player.PlayerId, GameServerId = serverId, Code = secondCode.Code })).StatusCode);
            var outcomes = await Task.WhenAll(first.ApproveConnectedPlayerClaim(firstUser.UserProfileId, firstId),
                second.ApproveConnectedPlayerClaim(secondUser.UserProfileId, secondId));
            Assert.Single(outcomes, outcome => outcome.StatusCode == HttpStatusCode.OK);
            Assert.Single(outcomes, outcome => outcome.StatusCode == HttpStatusCode.Conflict);
            await using var verify = new PortalDbContext(options);
            Assert.Equal(1, await verify.ConnectedPlayerProfiles.CountAsync(p => p.PlayerId == player.PlayerId && p.IsActive));
        });
}
