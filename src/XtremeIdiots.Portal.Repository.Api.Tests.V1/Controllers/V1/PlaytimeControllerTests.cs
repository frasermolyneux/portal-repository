using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

[Trait("Category", "Unit")]
public class PlaytimeControllerTests
{
    [Fact]
    public async Task BatchObservations_ReconcilesMapSegmentsAndReturnsDuplicate()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Test",
            Guid = "guid-1",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        context.GameServers.Add(new GameServer
        {
            GameServerId = serverId,
            Title = "Test",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "127.0.0.1",
            QueryPort = 28960
        });
        context.SaveChanges();

        var start = DateTime.UtcNow.AddMinutes(-30).AddSeconds(-DateTime.UtcNow.Second);
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;
        var batch = new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start, "guid-1"),
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged, start.AddMinutes(10), null, "mp_a"),
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged, start.AddMinutes(20), null, "mp_b"),
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft, start.AddMinutes(30), "guid-1")
            ]
        };

        var accepted = await api.BatchObservations(batch);

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.NotNull(accepted.Result);
        var acceptedData = accepted.Result?.Data;
        Assert.NotNull(acceptedData);
        Assert.All(acceptedData!.Items, item => Assert.Equal(PlaytimeObservationOutcome.Accepted, item.Outcome));

        var duplicate = await api.BatchObservations(batch);
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.NotNull(duplicate.Result);
        var duplicateData = duplicate.Result?.Data;
        Assert.NotNull(duplicateData);
        Assert.All(duplicateData!.Items, item => Assert.Equal(PlaytimeObservationOutcome.Duplicate, item.Outcome));

        var sessions = await context.PlayerSessions.ToListAsync();
        Assert.Single(sessions);
        Assert.Equal(start, sessions[0].StartUtc);
        Assert.Equal(start.AddMinutes(30), sessions[0].EndUtc);

        var totals = await api.GetTotals(
            start,
            start.AddMinutes(30),
            playerId,
            serverId,
            GameType.CallOfDuty4,
            null);

        Assert.NotNull(totals.Result);
        var totalsData = totals.Result?.Data;
        Assert.NotNull(totalsData);
        Assert.Equal(1200, totalsData!.MeasuredSeconds);
        Assert.Equal(600, totalsData.UnknownMapSeconds);
    }

    [Fact]
    public async Task ReconnectWithChangedCorrelationOpensASecondSession()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-30);
        var firstCorrelation = Guid.NewGuid();
        var secondCorrelation = Guid.NewGuid();
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1") with { ConnectionCorrelationId = firstCorrelation }
            ]
        });

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddMinutes(20), "guid-1") with { ConnectionCorrelationId = secondCorrelation }
            ]
        });

        var sessions = await context.PlayerSessions
            .OrderBy(x => x.StartUtc)
            .ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.Equal(start, sessions[0].StartUtc);
        Assert.Equal(start, sessions[0].EndUtc);
        Assert.Equal(start.AddMinutes(20), sessions[1].StartUtc);
        Assert.Null(sessions[1].EndUtc);
    }

    [Fact]
    public async Task ReusedConnectionCorrelationDoesNotReuseSessionPrimaryKey()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-40);
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1") with { ConnectionCorrelationId = correlationId },
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddMinutes(20), "guid-1") with { ConnectionCorrelationId = correlationId },
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(25), "guid-1") with { ConnectionCorrelationId = correlationId }
            ]
        });

        var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.NotEqual(sessions[0].SessionId, sessions[1].SessionId);
        Assert.All(sessions, x => Assert.Equal(correlationId, x.ConnectionCorrelationId));
        Assert.Equal(start, sessions[0].EndUtc);
        Assert.Equal(start.AddMinutes(25), sessions[1].EndUtc);
    }

    [Fact]
    public async Task CanonicalOnlyJoinReplayDoesNotCreateDuplicateSession()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        var api = (IPlaytimeApi)new PlaytimeController(context);
        var firstJoin = Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
            start, null);

        await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [firstJoin] });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations = [firstJoin with { ObservationId = Guid.NewGuid() }]
        });

        var session = Assert.Single(await context.PlayerSessions.ToListAsync());
        Assert.Equal(start, session.StartUtc);
        Assert.Null(session.EndUtc);
    }

    [Fact]
    public async Task CanonicalOnlyJoinEnrichmentMatchesClosedSessionAfterEarlierJoinMovesStart()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-30);
        var laterJoin = Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start.AddMinutes(10), null);
        var api = (IPlaytimeApi)new PlaytimeController(context);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                laterJoin,
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start, null),
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft, start.AddMinutes(20), null)
            ]
        });

        var replay = await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations = [laterJoin with { IsBot = false }]
        });

        Assert.Equal(PlaytimeObservationOutcome.Duplicate, replay.Result?.Data?.Items.Single().Outcome);
        var session = Assert.Single(await context.PlayerSessions.ToListAsync());
        Assert.Equal(start, session.StartUtc);
        Assert.Equal(start.AddMinutes(20), session.EndUtc);
        Assert.False(session.IsBot);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(20)]
    public async Task EarlierCanonicalOnlyJoinDoesNotOverlapClosedLaterSession(int minutesBefore)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-40);
        var api = (IPlaytimeApi)new PlaytimeController(context);
        var laterStart = start.AddMinutes(minutesBefore);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, laterStart, null),
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft, laterStart.AddMinutes(5), null),
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start, null)
            ]
        });
        var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
        if (minutesBefore == 5)
        {
            var session = Assert.Single(sessions);
            Assert.Equal(start, session.StartUtc);
            Assert.Equal(laterStart.AddMinutes(5), session.EndUtc);
        }
        else
        {
            Assert.Equal(2, sessions.Count);
            Assert.Equal(laterStart, sessions[0].EndUtc);
            Assert.Equal(laterStart, sessions[1].StartUtc);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task JoinAtFinalizedEndOpensNewSessionAndReplayEnrichesIt(bool useCorrelation)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        var correlation = useCorrelation ? Guid.NewGuid() : (Guid?)null;
        var api = (IPlaytimeApi)new PlaytimeController(context);
        var firstJoin = Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start, "guid-1")
            with
        { ConnectionCorrelationId = correlation };
        var leave = Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft, start.AddMinutes(10), "guid-1")
            with
        { ConnectionCorrelationId = correlation };
        var reconnect = Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start.AddMinutes(10), "guid-1")
            with
        { ConnectionCorrelationId = correlation };
        var result = await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [firstJoin, leave, reconnect] });
        var replay = await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations = [reconnect with { IsBot = false }]
        });

        Assert.All(result.Result!.Data!.Items, item => Assert.Equal(PlaytimeObservationOutcome.Accepted, item.Outcome));
        Assert.Equal(PlaytimeObservationOutcome.Duplicate, replay.Result?.Data?.Items.Single().Outcome);
        var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.Equal(start.AddMinutes(10), sessions[0].EndUtc);
        Assert.Equal(start.AddMinutes(10), sessions[1].StartUtc);
        Assert.Null(sessions[1].EndUtc);
        Assert.False(sessions[1].IsBot);
    }

    [Fact]
    public async Task CompleteRosterClosesMissingPresenceButPartialRosterDoesNot()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var player1 = Guid.NewGuid();
        var player2 = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var player2Connection = Guid.NewGuid();
        AddPlayerAndServer(context, player1, serverId);
        context.Players.Add(new Player
        {
            PlayerId = player2,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Test 2",
            Guid = "guid-2",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        context.SaveChanges();

        var start = DateTime.UtcNow.AddSeconds(-90);
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, player1, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start, "guid-1"),
                Observation(serverId, player2, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start, "guid-2")
                    with { ConnectionCorrelationId = player2Connection }
            ]
        });

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start.AddSeconds(30),
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Partial,
                    Roster = [new PlaytimeRosterPlayerDto { CanonicalPlayerId = player1, GamePlayerId = "guid-1", SlotId = 1 }]
                }
            ]
        });
        var partialSessions = await context.PlayerSessions.ToListAsync();
        Assert.All(partialSessions, session => Assert.Null(session.EndUtc));
        var partialPlayer1 = Assert.Single(partialSessions, session => session.PlayerId == player1);
        Assert.Equal(start.AddSeconds(30), partialPlayer1.LastConfirmedUtc);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start.AddSeconds(45),
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Complete,
                    Roster = [new PlaytimeRosterPlayerDto { CanonicalPlayerId = player1, GamePlayerId = "guid-1", SlotId = 1 }]
                }
            ]
        });

        var sessions = await context.PlayerSessions.OrderBy(x => x.PlayerId).ToListAsync();
        var player1Session = Assert.Single(sessions, x => x.PlayerId == player1);
        var player2Session = Assert.Single(sessions, x => x.PlayerId == player2);
        Assert.Null(player1Session.EndUtc);
        Assert.Equal(start.AddSeconds(45), player2Session.EndUtc);
        Assert.Equal(start, player2Session.LastConfirmedUtc);
        Assert.Equal((byte)PlaytimeTimestampQuality.RosterSnapshotTimestamp, player2Session.EndQuality);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, player2, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddSeconds(40), "guid-2") with { ConnectionCorrelationId = player2Connection }
            ]
        });

        var correctedPlayer2Session = await context.PlayerSessions.SingleAsync(x => x.PlayerId == player2);
        Assert.Equal(start.AddSeconds(40), correctedPlayer2Session.EndUtc);
        Assert.Equal(start, correctedPlayer2Session.LastConfirmedUtc);
    }

    [Fact]
    public async Task StalePartialRosterAdvancesCompatibleOpenSessionWithoutClosingAbsences()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var player1 = Guid.NewGuid();
        var player2 = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var connection1 = Guid.NewGuid();
        var connection2 = Guid.NewGuid();
        AddPlayerAndServer(context, player1, serverId);
        context.Players.Add(new Player
        {
            PlayerId = player2,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Test 2",
            Guid = "guid-2",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        context.SaveChanges();

        var start = DateTime.UtcNow.AddMinutes(-20);
        var api = (IPlaytimeApi)new PlaytimeController(context);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, player1, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1") with { ConnectionCorrelationId = connection1 },
                Observation(serverId, player2, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-2") with { ConnectionCorrelationId = connection2 }
            ]
        });

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start.AddMinutes(12),
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Partial,
                    Roster =
                    [
                        new PlaytimeRosterPlayerDto
                        {
                            CanonicalPlayerId = player2,
                            GamePlayerId = "guid-2",
                            SlotId = 2
                        }
                    ],
                    ConnectionCorrelationId = connection2
                }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start.AddMinutes(10),
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Partial,
                    Roster =
                    [
                        new PlaytimeRosterPlayerDto
                        {
                            CanonicalPlayerId = player1,
                            GamePlayerId = "guid-1",
                            SlotId = 1
                        }
                    ],
                    ConnectionCorrelationId = connection1
                }
            ]
        });

        var sessions = await context.PlayerSessions.ToListAsync();
        Assert.Equal(2, sessions.Count);
        var player1Session = Assert.Single(sessions, x => x.PlayerId == player1);
        var player2Session = Assert.Single(sessions, x => x.PlayerId == player2);
        Assert.Equal(start.AddMinutes(10), player1Session.LastConfirmedUtc);
        Assert.Equal(start.AddMinutes(12), player2Session.LastConfirmedUtc);
        Assert.All(sessions, session => Assert.Null(session.EndUtc));
    }

    [Fact]
    public async Task KnownBotWithoutCanonicalPlayerDoesNotInvalidateCompleteRoster()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddSeconds(-90);
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1")
            ]
        });

        var roster = new PlaytimeObservationDto
        {
            ObservationId = Guid.NewGuid(),
            ServerId = serverId,
            GameType = GameType.CallOfDuty4,
            Kind = PlaytimeObservationKind.ServerRoster,
            EventUtc = start.AddSeconds(30),
            TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
            RosterCompleteness = PlaytimeRosterCompleteness.Complete,
            Roster =
            [
                new PlaytimeRosterPlayerDto
                {
                    CanonicalPlayerId = null,
                    GamePlayerId = "bot-guid",
                    SlotId = 0,
                    IsBot = true
                }
            ]
        };
        var result = await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [roster] });

        Assert.Equal(PlaytimeObservationOutcome.Accepted, result.Result?.Data?.Items.Single().Outcome);
        var session = Assert.Single(await context.PlayerSessions.ToListAsync());
        Assert.Equal(start.AddSeconds(30), session.EndUtc);
        Assert.Equal((byte)2, session.ClosureReason);
        Assert.False(session.IsFinal);

        var storedRoster = await context.PlaytimeObservations.SingleAsync(x => x.ObservationId == roster.ObservationId);
        Assert.Equal((byte)PlaytimeRosterCompleteness.Complete, storedRoster.RosterCompleteness);
        Assert.Contains("\"CanonicalPlayerId\":null", storedRoster.ResolvedRosterJson);
        Assert.Contains("\"IsBot\":true", storedRoster.ResolvedRosterJson);
    }

    [Fact]
    public async Task CompleteRosterAbsenceSplitsDelayedJoinFromNewerSession()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddSeconds(-90);
        var api = (IPlaytimeApi)new PlaytimeController(context);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start.AddSeconds(30),
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Complete,
                    Roster = []
                }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddSeconds(60), "guid-1")
            ]
        });

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1")
            ]
        });

        var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.Equal(start, sessions[0].StartUtc);
        Assert.Equal(start.AddSeconds(30), sessions[0].EndUtc);
        Assert.Equal((byte)2, sessions[0].ClosureReason);
        Assert.False(sessions[0].IsFinal);
        Assert.Equal(start.AddSeconds(60), sessions[1].StartUtc);
        Assert.Null(sessions[1].EndUtc);
    }

    [Theory]
    [InlineData(120, PlaytimeTimestampQuality.RosterSnapshotTimestamp, true)]
    [InlineData(121, PlaytimeTimestampQuality.RosterSnapshotTimestamp, false)]
    [InlineData(-1, PlaytimeTimestampQuality.RosterSnapshotTimestamp, false)]
    [InlineData(10, PlaytimeTimestampQuality.ProducerTimestamp, false)]
    [InlineData(10, PlaytimeTimestampQuality.ParseTime, false)]
    public async Task CompleteRosterAbsenceRequiresTrustedNonnegativeFreshTimestamp(
        int processingAgeSeconds,
        PlaytimeTimestampQuality timestampQuality,
        bool closesMissingPresence)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var processingUtc = DateTimeOffset.UtcNow;
        var timeProvider = new MutableTimeProvider(processingUtc);
        var start = processingUtc.UtcDateTime.AddMinutes(-10);
        var api = (IPlaytimeApi)new PlaytimeController(context, timeProvider: timeProvider);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1")
            ]
        });

        var roster = new PlaytimeObservationDto
        {
            ObservationId = Guid.NewGuid(),
            ServerId = serverId,
            GameType = GameType.CallOfDuty4,
            Kind = PlaytimeObservationKind.ServerRoster,
            EventUtc = processingUtc.UtcDateTime.AddSeconds(-processingAgeSeconds),
            TimestampQuality = timestampQuality,
            RosterCompleteness = PlaytimeRosterCompleteness.Complete,
            Roster = []
        };
        var result = await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [roster] });

        Assert.Equal(PlaytimeObservationOutcome.Accepted, result.Result?.Data?.Items.Single().Outcome);
        var session = Assert.Single(await context.PlayerSessions.AsNoTracking().ToListAsync());
        if (closesMissingPresence)
        {
            Assert.Equal(roster.EventUtc, session.EndUtc);
            Assert.Equal((byte)2, session.ClosureReason);
        }
        else
        {
            Assert.Null(session.EndUtc);
        }

        var storedRoster = await context.PlaytimeObservations
            .AsNoTracking()
            .SingleAsync(x => x.ObservationId == roster.ObservationId);
        Assert.Equal(roster.EventUtc, storedRoster.EventUtc);
        Assert.Equal((byte)PlaytimeRosterCompleteness.Complete, storedRoster.RosterCompleteness);
    }

    [Fact]
    public async Task EnrichmentReplayOfPreviouslyFreshCompleteRosterUsesCurrentProcessingAge()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var player1 = Guid.NewGuid();
        var player2 = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, player1, serverId);
        context.Players.Add(new Player
        {
            PlayerId = player2,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Test 2",
            Guid = "guid-2",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var receivedUtc = DateTimeOffset.UtcNow;
        var timeProvider = new MutableTimeProvider(receivedUtc);
        var start = receivedUtc.UtcDateTime.AddMinutes(-10);
        var api = (IPlaytimeApi)new PlaytimeController(context, timeProvider: timeProvider);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, player1, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1")
            ]
        });

        var roster = new PlaytimeObservationDto
        {
            ObservationId = Guid.NewGuid(),
            ServerId = serverId,
            GameType = GameType.CallOfDuty4,
            Kind = PlaytimeObservationKind.ServerRoster,
            EventUtc = receivedUtc.UtcDateTime.AddSeconds(-30),
            TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
            RosterCompleteness = PlaytimeRosterCompleteness.Complete,
            Roster =
            [
                new PlaytimeRosterPlayerDto
                {
                    CanonicalPlayerId = null,
                    GamePlayerId = "guid-2",
                    SlotId = 2
                }
            ]
        };
        await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [roster] });

        timeProvider.Advance(TimeSpan.FromMinutes(3));
        var replay = await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                roster with
                {
                    Roster =
                    [
                        new PlaytimeRosterPlayerDto
                        {
                            CanonicalPlayerId = player2,
                            GamePlayerId = "guid-2",
                            SlotId = 2
                        }
                    ]
                }
            ]
        });

        Assert.Equal(PlaytimeObservationOutcome.Duplicate, replay.Result?.Data?.Items.Single().Outcome);
        var player1Session = await context.PlayerSessions.AsNoTracking().SingleAsync(x => x.PlayerId == player1);
        Assert.Null(player1Session.EndUtc);
        Assert.Single(await context.PlayerSessions.AsNoTracking().ToListAsync(), x => x.PlayerId == player2);

        var storedRoster = await context.PlaytimeObservations
            .AsNoTracking()
            .SingleAsync(x => x.ObservationId == roster.ObservationId);
        Assert.Equal(roster.EventUtc, storedRoster.EventUtc);
        Assert.Equal((byte)PlaytimeRosterCompleteness.Complete, storedRoster.RosterCompleteness);
        Assert.Equal(receivedUtc.UtcDateTime, storedRoster.ReceivedUtc);
    }

    [Fact]
    public async Task PlayerAnalyticsV2Detail_UsesMeasuredPlaytimeInsteadOfRecentPlayerCount()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddMinutes(-20);
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Test",
            FirstSeen = start,
            LastSeen = start.AddMinutes(10)
        });
        context.GameServers.Add(new GameServer
        {
            GameServerId = serverId,
            Title = "Test",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "127.0.0.1",
            QueryPort = 28960
        });
        var sessionId = Guid.NewGuid();
        context.PlayerSessions.Add(new PlayerSession
        {
            SessionId = sessionId,
            PlayerId = playerId,
            ServerId = serverId,
            GameType = (int)GameType.CallOfDuty4,
            StartUtc = start,
            LastConfirmedUtc = start.AddMinutes(10),
            EndUtc = start.AddMinutes(10),
            StartQuality = 1,
            EndQuality = 1,
            ClosureReason = 1,
            IsFinal = true
        });
        context.PlayerSessionMapSegments.Add(new PlayerSessionMapSegment
        {
            SegmentId = Guid.NewGuid(),
            SessionId = sessionId,
            MapRunId = null,
            StartUtc = start,
            EndUtc = start.AddMinutes(10),
            UnknownMap = true
        });
        context.SaveChanges();

        var api = (IPlayerAnalyticsV2Api)new PlayerAnalyticsV2Controller(context);
        var result = await api.GetPlayerDetail(
            playerId,
            start.AddMinutes(-1),
            start.AddMinutes(11));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Result);
        var data = result.Result?.Data;
        Assert.NotNull(data);
        Assert.Equal(0, data!.SessionsCount);
        Assert.Equal(0, data.TotalPlayTimeMinutes);
    }

    [Fact]
    public async Task FractionalSegmentsAreSummedForSessionAndPlayerAnalytics()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-5);
        var sessionId = Guid.NewGuid();
        context.PlayerSessions.Add(new PlayerSession
        {
            SessionId = sessionId,
            PlayerId = playerId,
            ServerId = serverId,
            GameType = (int)GameType.CallOfDuty4,
            IsBot = false,
            StartUtc = start,
            LastConfirmedUtc = start.AddMilliseconds(61200),
            EndUtc = start.AddMilliseconds(61200),
            StartQuality = 1
        });
        context.PlayerSessionMapSegments.AddRange(
            new PlayerSessionMapSegment
            {
                SegmentId = Guid.NewGuid(),
                SessionId = sessionId,
                StartUtc = start,
                EndUtc = start.AddMilliseconds(30600),
                UnknownMap = false
            },
            new PlayerSessionMapSegment
            {
                SegmentId = Guid.NewGuid(),
                SessionId = sessionId,
                StartUtc = start.AddMilliseconds(30600),
                EndUtc = start.AddMilliseconds(61200),
                UnknownMap = false
            });
        await context.SaveChangesAsync();

        var sessions = await ((IPlaytimeApi)new PlaytimeController(context))
            .GetPlayerSessions(playerId, null, null, 0, 10);
        var detail = await ((IPlayerAnalyticsV2Api)new PlayerAnalyticsV2Controller(context))
            .GetPlayerDetail(playerId, start, start.AddMinutes(2));
        Assert.Equal(61, Assert.Single(sessions.Result!.Data!).MeasuredSeconds);
        Assert.Equal(2, detail.Result?.Data?.TotalPlayTimeMinutes);
    }

    [Fact]
    public async Task LeavePreservesParseTimeQuality()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-10);
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1") with { TimestampQuality = PlaytimeTimestampQuality.ParseTime },
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(10), "guid-1") with { TimestampQuality = PlaytimeTimestampQuality.ParseTime }
            ]
        });

        var session = Assert.Single(await context.PlayerSessions.ToListAsync());
        Assert.Equal((byte)PlaytimeTimestampQuality.ParseTime, session.StartQuality);
        Assert.Equal((byte)PlaytimeTimestampQuality.ParseTime, session.EndQuality);
        Assert.Equal(start, session.StartUtc);
        Assert.Equal(start.AddMinutes(10), session.EndUtc);
    }

    [Fact]
    public async Task BotAndUnclassifiedCoverageIsReportedHonest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var botPlayer = Guid.NewGuid();
        var unknownPlayer = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        context.Players.AddRange(
            new Player
            {
                PlayerId = botPlayer,
                GameType = (int)GameType.CallOfDuty4,
                Username = "Bot",
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            },
            new Player
            {
                PlayerId = unknownPlayer,
                GameType = (int)GameType.CallOfDuty4,
                Username = "Unknown",
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            });
        context.GameServers.Add(new GameServer
        {
            GameServerId = serverId,
            Title = "Test",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "127.0.0.1",
            QueryPort = 28960
        });
        context.Maps.Add(new Map
        {
            MapId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            MapName = "mp_known"
        });
        context.SaveChanges();

        var start = DateTime.UtcNow.AddMinutes(-20);
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, botPlayer, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start, "bot") with { IsBot = true },
                Observation(serverId, botPlayer, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft, start.AddMinutes(10), "bot") with { IsBot = true },
                Observation(serverId, botPlayer, Guid.NewGuid(), PlaytimeObservationKind.MapChanged,
                    start.AddMinutes(10), null, "mp_known"),
                Observation(serverId, unknownPlayer, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start.AddMinutes(10), "unknown") with { IsBot = null },
                Observation(serverId, unknownPlayer, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft, start.AddMinutes(20), "unknown") with { IsBot = null }
            ]
        });

        var totals = await api.GetTotals(start, start.AddMinutes(20), null, serverId, GameType.CallOfDuty4, null);
        Assert.NotNull(totals.Result);
        var data = totals.Result?.Data;
        Assert.NotNull(data);
        Assert.Equal(600, data!.KnownBotSeconds);
        Assert.Equal(600, data.MeasuredSeconds);
        Assert.Equal(600, data.UnclassifiedPlayerSeconds);
        Assert.False(data.HumanOnlyCoverage);
    }

    [Fact]
    public async Task TotalsSumSubsecondSegmentsBeforeConvertingAndPreserveUnclassifiedCoverage()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-1);
        var humanSession = new PlayerSession
        {
            SessionId = Guid.NewGuid(),
            PlayerId = playerId,
            ServerId = serverId,
            GameType = (int)GameType.CallOfDuty4,
            IsBot = false,
            StartUtc = start,
            LastConfirmedUtc = start.AddSeconds(2),
            StartQuality = 2
        };
        var unknownSession = new PlayerSession
        {
            SessionId = Guid.NewGuid(),
            PlayerId = playerId,
            ServerId = serverId,
            GameType = (int)GameType.CallOfDuty4,
            IsBot = null,
            StartUtc = start,
            LastConfirmedUtc = start.AddSeconds(1),
            StartQuality = 2
        };
        context.PlayerSessions.AddRange(humanSession, unknownSession);
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
        await context.SaveChangesAsync();

        var result = await ((IPlaytimeApi)new PlaytimeController(context))
            .GetTotals(start, start.AddSeconds(3), playerId, serverId, null, null);

        Assert.Equal(1, result.Result?.Data?.UnknownMapSeconds);
        Assert.Equal(0, result.Result?.Data?.UnclassifiedPlayerSeconds);
        Assert.False(result.Result?.Data?.HumanOnlyCoverage);
    }

    [Fact]
    public async Task RepeatedJoinsAcrossMapChangesKeepOneSession()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start, "guid-1")
                    with { TimestampQuality = PlaytimeTimestampQuality.ParseTime },
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged, start.AddMinutes(5), null, "mp_a")
                    with { TimestampQuality = PlaytimeTimestampQuality.ParseTime },
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start.AddMinutes(5), "guid-1")
                    with { TimestampQuality = PlaytimeTimestampQuality.ParseTime },
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged, start.AddMinutes(10), null, "mp_b")
                    with { TimestampQuality = PlaytimeTimestampQuality.ParseTime },
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft, start.AddMinutes(15), "guid-1")
                    with { TimestampQuality = PlaytimeTimestampQuality.ParseTime }
            ]
        });

        var sessions = await context.PlayerSessions.ToListAsync();
        Assert.Single(sessions);
        Assert.Equal(start, sessions[0].StartUtc);
        Assert.Equal(start.AddMinutes(15), sessions[0].EndUtc);
    }

    [Fact]
    public async Task DelayedJoinAndDisconnectCorrectAnInferredSession()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-30);
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddMinutes(10), "guid-1") with { ConnectionCorrelationId = connectionId }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1") with { ConnectionCorrelationId = connectionId }
            ]
        });

        await PlaytimeSweeperHostedService.SweepAsync(context, start.AddMinutes(11), CancellationToken.None);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(15), "guid-1") with
                {
                    ConnectionCorrelationId = connectionId,
                    IsBot = true
                }
            ]
        });

        var session = Assert.Single(await context.PlayerSessions.ToListAsync());
        Assert.Equal(start, session.StartUtc);
        Assert.Equal(start.AddMinutes(15), session.EndUtc);
        Assert.True(session.IsBot);
    }

    [Fact]
    public async Task DelayedUncorrelatedLeaveCorrectsInferredSessionWithoutClosingNewerReconnect()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-30);
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1")
            ]
        });
        await PlaytimeSweeperHostedService.SweepAsync(context, start.AddMinutes(1), CancellationToken.None);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddMinutes(20), "guid-1")
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(10), "guid-1")
            ]
        });

        var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.Equal(start.AddMinutes(10), sessions[0].EndUtc);
        Assert.Equal((byte)1, sessions[0].ClosureReason);
        Assert.True(sessions[0].IsFinal);
        Assert.Equal(start.AddMinutes(20), sessions[1].StartUtc);
        Assert.Null(sessions[1].EndUtc);
    }

    [Fact]
    public async Task DelayedUncorrelatedLeaveDoesNotRewriteFinalClosure()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1"),
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(10), "guid-1")
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(5), "guid-1")
            ]
        });

        var session = await context.PlayerSessions.SingleAsync();
        Assert.Equal(start.AddMinutes(10), session.EndUtc);
        Assert.Equal((byte)1, session.ClosureReason);
        Assert.True(session.IsFinal);
    }

    [Fact]
    public async Task DelayedJoinDoesNotMergeAcrossAcceptedLeaveBoundary()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-30);
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(10), "guid-1")
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddMinutes(20), "guid-1")
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(30), "guid-1")
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1")
            ]
        });

        var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.Equal(start, sessions[0].StartUtc);
        Assert.Equal(start.AddMinutes(10), sessions[0].EndUtc);
        Assert.True(sessions[0].IsFinal);
        Assert.Equal(start.AddMinutes(20), sessions[1].StartUtc);
        Assert.Equal(start.AddMinutes(30), sessions[1].EndUtc);
        Assert.True(sessions[1].IsFinal);
    }

    [Fact]
    public async Task RetainedLeaveFromLaterSessionDoesNotOverrideEarlierRosterAbsence()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var processingUtc = DateTimeOffset.UtcNow;
        var start = processingUtc.UtcDateTime.AddSeconds(-50);
        var api = (IPlaytimeApi)new PlaytimeController(context, timeProvider: new MutableTimeProvider(processingUtc));

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start.AddSeconds(5),
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Complete,
                    Roster = []
                }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddSeconds(10), "guid-1"),
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddSeconds(20), "guid-1")
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1")
            ]
        });

        var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.Equal(start, sessions[0].StartUtc);
        Assert.Equal(start.AddSeconds(5), sessions[0].EndUtc);
        Assert.Equal((byte)2, sessions[0].ClosureReason);
        Assert.Equal(start.AddSeconds(10), sessions[1].StartUtc);
        Assert.Equal(start.AddSeconds(20), sessions[1].EndUtc);
        Assert.True(sessions[1].IsFinal);
    }

    [Fact]
    public async Task ForwardPresenceAfterRetainedUnresolvedLeaveStartsNewSession()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddSeconds(-30);
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1")
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddSeconds(5), "guid-1") with { CanonicalPlayerId = null }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddSeconds(10), "guid-1")
            ]
        });

        var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.Equal(start, sessions[0].StartUtc);
        Assert.Equal(start.AddSeconds(5), sessions[0].EndUtc);
        Assert.Equal((byte)1, sessions[0].ClosureReason);
        Assert.True(sessions[0].IsFinal);
        Assert.Equal(start.AddSeconds(10), sessions[1].StartUtc);
        Assert.Null(sessions[1].EndUtc);
    }

    [Fact]
    public async Task UnresolvedLeaveWithoutGamePlayerIdDoesNotCloseNewSession()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(10), null) with { CanonicalPlayerId = null }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, null)
            ]
        });

        var session = await context.PlayerSessions.SingleAsync();
        Assert.Equal(start, session.StartUtc);
        Assert.Null(session.EndUtc);
        Assert.False(session.IsFinal);
    }

    [Fact]
    public async Task DelayedJoinEnrichmentDoesNotShortenFinalSessionClosure()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        context.PlayerSessions.Add(new PlayerSession
        {
            SessionId = Guid.NewGuid(),
            PlayerId = playerId,
            ServerId = serverId,
            ConnectionCorrelationId = correlationId,
            GameType = (int)GameType.CallOfDuty4,
            GamePlayerId = "guid-1",
            StartUtc = start,
            EndUtc = start.AddMinutes(20),
            LastConfirmedUtc = start,
            StartQuality = (byte)PlaytimeTimestampQuality.ProducerTimestamp,
            EndQuality = (byte)PlaytimeTimestampQuality.ProducerTimestamp,
            ClosureReason = 1,
            IsFinal = true
        });
        context.PlaytimeObservations.Add(new PlaytimeObservation
        {
            ObservationId = Guid.NewGuid(),
            ServerId = serverId,
            GameType = (int)GameType.CallOfDuty4,
            Kind = (byte)PlaytimeObservationKind.PlayerLeft,
            EventUtc = start.AddMinutes(10),
            ReceivedUtc = DateTime.UtcNow,
            TimestampQuality = (byte)PlaytimeTimestampQuality.ProducerTimestamp,
            ConnectionCorrelationId = correlationId,
            CanonicalPlayerId = playerId,
            GamePlayerId = "guid-1",
            PayloadHash = new byte[32],
            Outcome = (byte)PlaytimeObservationOutcome.Accepted
        });
        await context.SaveChangesAsync();

        var api = (IPlaytimeApi)new PlaytimeController(context);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddMinutes(5), "guid-1") with { ConnectionCorrelationId = correlationId }
            ]
        });

        var session = await context.PlayerSessions.SingleAsync();
        Assert.Equal(start.AddMinutes(20), session.EndUtc);
        Assert.Equal((byte)1, session.ClosureReason);
        Assert.True(session.IsFinal);
    }

    [Fact]
    public async Task DuplicateLeaveEnrichesBotClassificationWithoutChangingFinalClosure()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        var leave = Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
            start.AddMinutes(10), "guid-1") with
        { ConnectionCorrelationId = correlationId };
        var api = (IPlaytimeApi)new PlaytimeController(context);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1") with { ConnectionCorrelationId = correlationId },
                leave
            ]
        });

        var session = await context.PlayerSessions.SingleAsync();
        session.EndUtc = start.AddMinutes(20);
        await context.SaveChangesAsync();
        Assert.Equal(correlationId, session.ConnectionCorrelationId);
        Assert.Null(session.IsBot);

        var replay = await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations = [leave with { IsBot = true }]
        });

        Assert.Equal(PlaytimeObservationOutcome.Duplicate, replay.Result?.Data?.Items.Single().Outcome);
        var storedLeave = await context.PlaytimeObservations.SingleAsync(x => x.ObservationId == leave.ObservationId);
        Assert.Equal(playerId, storedLeave.CanonicalPlayerId);
        Assert.Equal(correlationId, storedLeave.ConnectionCorrelationId);
        Assert.True(storedLeave.IsBot);
        var updatedSession = await context.PlayerSessions.AsNoTracking().SingleAsync();
        Assert.Equal(start.AddMinutes(20), updatedSession.EndUtc);
        Assert.True(updatedSession.IsFinal);
        Assert.True(updatedSession.IsBot);
    }

    [Fact]
    public async Task ObservationHashDoesNotCollideAcrossDelimitedFields()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var observationId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow.AddMinutes(-10);
        var api = (IPlaytimeApi)new PlaytimeController(context);
        var first = Observation(serverId, playerId, observationId, PlaytimeObservationKind.PlayerJoined,
            timestamp, "a|1|b", "c") with
        { SlotId = 2 };
        var collidingPayload = Observation(serverId, playerId, observationId, PlaytimeObservationKind.PlayerJoined,
            timestamp, "a", "b|2|c") with
        { SlotId = 1 };

        var accepted = await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations = [first]
        });
        var replay = await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations = [collidingPayload]
        });

        Assert.Equal(PlaytimeObservationOutcome.Accepted, accepted.Result?.Data?.Items.Single().Outcome);
        Assert.Equal(PlaytimeObservationOutcome.Rejected, replay.Result?.Data?.Items.Single().Outcome);
    }

    [Fact]
    public async Task DelayedUncorrelatedLeaveCorrectsRosterInferredClosure()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1")
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start.AddMinutes(5),
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Complete,
                    Roster = []
                }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(10), "guid-1")
            ]
        });

        var session = await context.PlayerSessions.SingleAsync();
        Assert.Equal(start.AddMinutes(10), session.EndUtc);
        Assert.Equal((byte)1, session.ClosureReason);
        Assert.True(session.IsFinal);
    }

    [Fact]
    public async Task DuplicateJoinCanResolveBotClassificationAfterSessionClosure()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        var join = Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
            start, "guid-1") with
        { IsBot = null };
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                join,
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(10), "guid-1")
            ]
        });
        var resolved = await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations = [join with { IsBot = true }]
        });
        Assert.Equal(PlaytimeObservationOutcome.Duplicate, resolved.Result?.Data?.Items.Single().Outcome);

        var conflicting = await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations = [join with { IsBot = false }]
        });
        Assert.Equal(PlaytimeObservationOutcome.Rejected, conflicting.Result?.Data?.Items.Single().Outcome);

        var session = Assert.Single(await context.PlayerSessions.ToListAsync());
        Assert.True(session.IsBot);
        Assert.Single(await context.PlayerSessionMapSegments.ToListAsync());
    }

    [Fact]
    public async Task DelayedMapChangeIsInsertedChronologicallyAndOnlyRebuildsItsInterval()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-40);
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1"),
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged,
                    start, null, "mp_a"),
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged,
                    start.AddMinutes(20), null, "mp_b"),
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(30), "guid-1")
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged,
                    start.AddMinutes(10), null, "My Map")
            ]
        });

        var runs = await context.ServerMapRuns.OrderBy(x => x.StartUtc).ToListAsync();
        Assert.Equal(3, runs.Count);
        Assert.Equal(start, runs[0].StartUtc);
        Assert.Equal(start.AddMinutes(10), runs[0].EndUtc);
        Assert.Equal("my_map", runs[1].NormalizedMapKey);
        Assert.Equal(start.AddMinutes(10), runs[1].StartUtc);
        Assert.Equal(start.AddMinutes(20), runs[1].EndUtc);
        Assert.Equal("mp_b", runs[2].NormalizedMapKey);
        Assert.Null(runs[2].EndUtc);

        var mapTotals = await api.GetTotals(
            start,
            start.AddMinutes(30),
            playerId,
            serverId,
            GameType.CallOfDuty4,
            "My Map");
        Assert.Equal(600, mapTotals.Result?.Data?.MeasuredSeconds);
    }

    [Fact]
    public async Task MaplessRosterSequenceDoesNotSuppressMapEvidenceAtSameTimestamp()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var timestamp = DateTime.UtcNow.AddMinutes(-10);
        var epoch = Guid.NewGuid();
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.ServerRoster,
                    timestamp, null) with
                {
                    ProducerEpoch = epoch,
                    ProducerSequence = 2,
                    RosterCompleteness = PlaytimeRosterCompleteness.Partial,
                    Roster = []
                }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged,
                    timestamp, null, "mp_a") with
                {
                    ProducerEpoch = epoch,
                    ProducerSequence = 1
                }
            ]
        });

        var run = await context.ServerMapRuns.SingleAsync();
        Assert.Equal("mp_a", run.NormalizedMapKey);
    }

    [Fact]
    public async Task DelayedMapChangeReplaysLaterSameMapRosterConfirmation()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-30);
        var epoch = Guid.NewGuid();
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged,
                    start, null, "mp_a") with { ProducerEpoch = epoch, ProducerSequence = 1 }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.ServerRoster,
                    start.AddMinutes(10), null, "mp_a") with
                {
                    ProducerEpoch = epoch,
                    ProducerSequence = 3,
                    RosterCompleteness = PlaytimeRosterCompleteness.Partial,
                    Roster = []
                }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged,
                    start.AddMinutes(5), null, "mp_b") with { ProducerEpoch = epoch, ProducerSequence = 2 }
            ]
        });

        var runs = await context.ServerMapRuns.OrderBy(x => x.StartUtc).ToListAsync();
        Assert.Equal(3, runs.Count);
        Assert.Equal("mp_a", runs[0].NormalizedMapKey);
        Assert.Equal(start, runs[0].StartUtc);
        Assert.Equal(start.AddMinutes(5), runs[0].EndUtc);
        Assert.Equal("mp_b", runs[1].NormalizedMapKey);
        Assert.Equal(start.AddMinutes(5), runs[1].StartUtc);
        Assert.Equal(start.AddMinutes(10), runs[1].EndUtc);
        Assert.Equal("mp_a", runs[2].NormalizedMapKey);
        Assert.Equal(start.AddMinutes(10), runs[2].StartUtc);
        Assert.Null(runs[2].EndUtc);
    }

    [Fact]
    public async Task StatusCoverageCountersAreReportedPerEpochWithoutLossInference()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = Guid.NewGuid();
        var secondServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = serverId,
            Title = "Test",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "127.0.0.1",
            QueryPort = 28960
        });
        context.GameServers.Add(new GameServer
        {
            GameServerId = secondServerId,
            Title = "Test 2",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "127.0.0.2",
            QueryPort = 28960
        });
        context.SaveChanges();

        var epoch = Guid.NewGuid();
        var start = DateTime.UtcNow.AddMinutes(-10);
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start,
                    TimestampQuality = PlaytimeTimestampQuality.ParseTime,
                    ProducerEpoch = epoch,
                    RosterCompleteness = PlaytimeRosterCompleteness.Partial,
                    Coverage = new PlaytimeCoverageCountersDto
                    {
                        ProducerEpoch = epoch,
                        ProducerStartedUtc = start.AddMinutes(-10),
                        ReportedUtc = start.AddSeconds(12),
                        DroppedCapacityMax = 2,
                        DroppedAttemptsMax = 3,
                        DroppedAgeMax = 4
                    }
                }
            ]
        });

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = secondServerId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start,
                    TimestampQuality = PlaytimeTimestampQuality.ParseTime,
                    ProducerEpoch = epoch,
                    RosterCompleteness = PlaytimeRosterCompleteness.Partial,
                    Coverage = new PlaytimeCoverageCountersDto
                    {
                        ProducerEpoch = epoch,
                        ProducerStartedUtc = start.AddMinutes(-10),
                        ReportedUtc = start,
                        DroppedCapacityMax = 5,
                        DroppedAttemptsMax = 6,
                        DroppedAgeMax = 7
                    }
                }
            ]
        });

        var totals = await api.GetTotals(start.AddMinutes(-1), start.AddMinutes(1), null, null, GameType.CallOfDuty4, null);
        Assert.NotNull(totals.Result);
        var data = totals.Result?.Data;
        Assert.NotNull(data);
        Assert.Equal(2, data!.CoverageReports.Count);
        var report = Assert.Single(data.CoverageReports, x => x.ServerId == serverId);
        Assert.Equal(serverId, report.ServerId);
        Assert.Equal(epoch, report.ProducerEpoch);
        Assert.Equal(start.AddSeconds(12), report.ReportedUtc);
        Assert.Equal(2, report.DroppedCapacityMax);
        Assert.Equal(3, report.DroppedAttemptsMax);
        Assert.Equal(4, report.DroppedAgeMax);
        var secondReport = Assert.Single(data.CoverageReports, x => x.ServerId == secondServerId);
        Assert.Equal(5, secondReport.DroppedCapacityMax);
    }

    [Fact]
    public async Task CoverageRejectsMismatchedEpochAndChangedReportTimeOnReplay()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = serverId,
            Title = "Test",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "127.0.0.1",
            QueryPort = 28960
        });
        context.SaveChanges();

        var epoch = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var observation = new PlaytimeObservationDto
        {
            ObservationId = Guid.NewGuid(),
            ServerId = serverId,
            GameType = GameType.CallOfDuty4,
            Kind = PlaytimeObservationKind.ServerRoster,
            EventUtc = now,
            TimestampQuality = PlaytimeTimestampQuality.ParseTime,
            ProducerEpoch = epoch,
            RosterCompleteness = PlaytimeRosterCompleteness.Partial,
            Coverage = new PlaytimeCoverageCountersDto { ProducerEpoch = epoch, ReportedUtc = now.AddSeconds(3) }
        };
        var api = (IPlaytimeApi)new PlaytimeController(context);
        var mismatch = observation with { Coverage = observation.Coverage with { ProducerEpoch = Guid.NewGuid() } };
        var invalid = await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [mismatch] });
        var accepted = await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [observation] });
        var replay = observation with { Coverage = observation.Coverage with { ReportedUtc = now.AddSeconds(4) } };
        var conflict = await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [replay] });

        Assert.Equal(PlaytimeObservationOutcome.Rejected, invalid.Result?.Data?.Items.Single().Outcome);
        Assert.Equal(PlaytimeObservationOutcome.Accepted, accepted.Result?.Data?.Items.Single().Outcome);
        Assert.Equal(PlaytimeObservationOutcome.Rejected, conflict.Result?.Data?.Items.Single().Outcome);
    }

    [Fact]
    public async Task TwoRosterIdentityRetriesResolveSeparatelyWithoutRemint()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var player1 = Guid.NewGuid();
        var player2 = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, player1, serverId);
        context.Players.Add(new Player
        {
            PlayerId = player2,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Test 2",
            Guid = "guid-2",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        context.SaveChanges();

        var observationId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddMinutes(-5);
        var unresolved = new PlaytimeObservationDto
        {
            ObservationId = observationId,
            ServerId = serverId,
            GameType = GameType.CallOfDuty4,
            Kind = PlaytimeObservationKind.ServerRoster,
            EventUtc = start,
            TimestampQuality = PlaytimeTimestampQuality.ParseTime,
            RosterCompleteness = PlaytimeRosterCompleteness.Partial,
            Roster =
            [
                new PlaytimeRosterPlayerDto { CanonicalPlayerId = null, GamePlayerId = "guid-1", SlotId = 1 },
                new PlaytimeRosterPlayerDto { CanonicalPlayerId = null, GamePlayerId = "guid-2", SlotId = 2 }
            ]
        };
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;
        await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [unresolved] });

        var firstResolved = unresolved with
        {
            Roster =
            [
                new PlaytimeRosterPlayerDto { CanonicalPlayerId = player1, GamePlayerId = "guid-1", SlotId = 1 },
                new PlaytimeRosterPlayerDto { CanonicalPlayerId = null, GamePlayerId = "guid-2", SlotId = 2 }
            ]
        };
        await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [firstResolved] });

        var secondResolved = unresolved with
        {
            Roster =
            [
                new PlaytimeRosterPlayerDto { CanonicalPlayerId = player1, GamePlayerId = "guid-1", SlotId = 1 },
                new PlaytimeRosterPlayerDto { CanonicalPlayerId = player2, GamePlayerId = "guid-2", SlotId = 2 }
            ]
        };
        await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [secondResolved] });
        await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [secondResolved] });

        Assert.Equal(2, await context.PlayerSessions.CountAsync());
    }

    [Fact]
    public async Task ReplayAfterIdentityResolutionDoesNotConflictAndCreatesSession()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-5);
        var observationId = Guid.NewGuid();
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;

        var unresolved = new PlaytimeObservationDto
        {
            ObservationId = observationId,
            ServerId = serverId,
            GameType = GameType.CallOfDuty4,
            Kind = PlaytimeObservationKind.ServerRoster,
            EventUtc = start,
            TimestampQuality = PlaytimeTimestampQuality.ParseTime,
            RosterCompleteness = PlaytimeRosterCompleteness.Partial,
            Roster =
            [
                new PlaytimeRosterPlayerDto
                {
                    CanonicalPlayerId = null,
                    GamePlayerId = "guid-1",
                    SlotId = 1
                }
            ]
        };
        await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [unresolved] });
        Assert.Empty(await context.PlayerSessions.ToListAsync());

        var resolved = unresolved with
        {
            Roster =
            [
                new PlaytimeRosterPlayerDto
                {
                    CanonicalPlayerId = playerId,
                    GamePlayerId = "guid-1",
                    SlotId = 1
                }
            ]
        };
        var replay = await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [resolved] });
        Assert.NotNull(replay.Result);
        var replayData = replay.Result?.Data;
        Assert.NotNull(replayData);
        Assert.Equal(PlaytimeObservationOutcome.Duplicate, replayData!.Items[0].Outcome);
        Assert.Single(await context.PlayerSessions.ToListAsync());
    }

    [Fact]
    public async Task StaleAndIdentityIncompleteCompleteRostersDoNotCloseAbsences()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var player1 = Guid.NewGuid();
        var player2 = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, player1, serverId);
        context.Players.Add(new Player
        {
            PlayerId = player2,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Test 2",
            Guid = "guid-2",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        context.SaveChanges();

        var start = DateTime.UtcNow.AddSeconds(-90);
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, player1, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start, "guid-1"),
                Observation(serverId, player2, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start, "guid-2")
            ]
        });

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start.AddSeconds(30),
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Complete,
                    Roster =
                    [
                        new PlaytimeRosterPlayerDto { CanonicalPlayerId = player1, GamePlayerId = "guid-1", SlotId = 1 },
                        new PlaytimeRosterPlayerDto { CanonicalPlayerId = null, GamePlayerId = "guid-x", SlotId = 2 }
                    ]
                }
            ]
        });

        var sessions = await context.PlayerSessions.ToListAsync();
        Assert.All(sessions, session => Assert.Null(session.EndUtc));

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start.AddSeconds(60),
                    ProducerEpoch = Guid.NewGuid(),
                    ProducerSequence = 2,
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Complete,
                    Roster = [new PlaytimeRosterPlayerDto { CanonicalPlayerId = player1, GamePlayerId = "guid-1", SlotId = 1 }]
                }
            ]
        });

        var older = new PlaytimeObservationDto
        {
            ObservationId = Guid.NewGuid(),
            ServerId = serverId,
            GameType = GameType.CallOfDuty4,
            Kind = PlaytimeObservationKind.ServerRoster,
            EventUtc = start.AddSeconds(20),
            TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
            RosterCompleteness = PlaytimeRosterCompleteness.Complete,
            Roster = [new PlaytimeRosterPlayerDto { CanonicalPlayerId = player1, GamePlayerId = "guid-1", SlotId = 1 }]
        };
        await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [older] });
        var player2Session = Assert.Single(await context.PlayerSessions.ToListAsync(), x => x.PlayerId == player2);
        Assert.Equal(start.AddSeconds(60), player2Session.EndUtc);
    }

    [Fact]
    public async Task SweepClosesAtLastConfirmedPresenceNotWallTime()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var stalePlayer = Guid.NewGuid();
        var freshPlayer = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        context.Players.AddRange(
            new Player
            {
                PlayerId = stalePlayer,
                GameType = (int)GameType.CallOfDuty4,
                Username = "Stale",
                FirstSeen = now.AddMinutes(-40),
                LastSeen = now.AddMinutes(-20)
            },
            new Player
            {
                PlayerId = freshPlayer,
                GameType = (int)GameType.CallOfDuty4,
                Username = "Fresh",
                FirstSeen = now.AddMinutes(-10),
                LastSeen = now
            });
        context.GameServers.Add(new GameServer
        {
            GameServerId = serverId,
            Title = "Test",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "127.0.0.1",
            QueryPort = 28960
        });
        context.PlayerSessions.AddRange(
            new PlayerSession
            {
                SessionId = Guid.NewGuid(),
                PlayerId = stalePlayer,
                ServerId = serverId,
                GameType = (int)GameType.CallOfDuty4,
                StartUtc = now.AddMinutes(-40),
                LastConfirmedUtc = now.AddMinutes(-20),
                StartQuality = 4,
                IsFinal = false
            },
            new PlayerSession
            {
                SessionId = Guid.NewGuid(),
                PlayerId = freshPlayer,
                ServerId = serverId,
                GameType = (int)GameType.CallOfDuty4,
                StartUtc = now.AddMinutes(-10),
                LastConfirmedUtc = now.AddMinutes(-5),
                StartQuality = 4,
                IsFinal = false
            });
        context.SaveChanges();

        await PlaytimeSweeperHostedService.SweepAsync(context, now.AddMinutes(-15), CancellationToken.None);

        var stale = Assert.Single(await context.PlayerSessions.ToListAsync(), x => x.PlayerId == stalePlayer);
        var fresh = Assert.Single(await context.PlayerSessions.ToListAsync(), x => x.PlayerId == freshPlayer);
        Assert.Equal(now.AddMinutes(-20), stale.EndUtc);
        Assert.Equal((byte)PlaytimeTimestampQuality.InferredTimestamp, stale.EndQuality);
        Assert.Null(fresh.EndUtc);
    }

    [Fact]
    public async Task LateIdentityReplayIsRejectedAfterCorrectionWindow()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddHours(-1);
        var observationId = Guid.NewGuid();
        var unresolved = new PlaytimeObservationDto
        {
            ObservationId = observationId,
            ServerId = serverId,
            GameType = GameType.CallOfDuty4,
            Kind = PlaytimeObservationKind.ServerRoster,
            EventUtc = start,
            TimestampQuality = PlaytimeTimestampQuality.ParseTime,
            RosterCompleteness = PlaytimeRosterCompleteness.Partial,
            Roster =
            [
                new PlaytimeRosterPlayerDto
                {
                    CanonicalPlayerId = null,
                    GamePlayerId = "guid-1",
                    SlotId = 1
                }
            ]
        };
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;
        await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [unresolved] });

        var fact = await context.PlaytimeObservations.SingleAsync(x => x.ObservationId == observationId);
        fact.ResolutionDeadlineUtc = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var resolved = unresolved with
        {
            Roster =
            [
                new PlaytimeRosterPlayerDto
                {
                    CanonicalPlayerId = playerId,
                    GamePlayerId = "guid-1",
                    SlotId = 1
                }
            ]
        };
        var replay = await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [resolved] });
        Assert.NotNull(replay.Result);
        var data = replay.Result?.Data;
        Assert.NotNull(data);
        Assert.Equal(PlaytimeObservationOutcome.Rejected, data!.Items[0].Outcome);
        Assert.Empty(await context.PlayerSessions.ToListAsync());
    }

    [Fact]
    public async Task BatchObservations_RejectsUnknownServerAndConflictingReuse()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = new PlaytimeController(context);
        var api = (IPlaytimeApi)controller;
        var observationId = Guid.NewGuid();
        var serverId = Guid.NewGuid();

        var result = await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, Guid.NewGuid(), observationId, PlaytimeObservationKind.MapChanged,
                    DateTime.UtcNow, null, "mp_a")
            ]
        });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Result);
        var resultData = result.Result?.Data;
        Assert.NotNull(resultData);
        Assert.Equal(PlaytimeObservationOutcome.Rejected, resultData!.Items[0].Outcome);
    }

    [Fact]
    public async Task DuplicateRosterIdentitiesAreRejectedBeforePersistence()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var observation = new PlaytimeObservationDto
        {
            ObservationId = Guid.NewGuid(),
            ServerId = serverId,
            GameType = GameType.CallOfDuty4,
            Kind = PlaytimeObservationKind.ServerRoster,
            EventUtc = DateTime.UtcNow.AddMinutes(-1),
            TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
            RosterCompleteness = PlaytimeRosterCompleteness.Partial,
            Roster =
            [
                new PlaytimeRosterPlayerDto { CanonicalPlayerId = playerId, GamePlayerId = "guid-1", SlotId = 1 },
                new PlaytimeRosterPlayerDto { CanonicalPlayerId = playerId, GamePlayerId = "guid-1", SlotId = 1 }
            ]
        };
        var api = (IPlaytimeApi)new PlaytimeController(context);

        var result = await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [observation] });

        Assert.Equal(PlaytimeObservationOutcome.Rejected, result.Result?.Data?.Items.Single().Outcome);
        Assert.Empty(await context.PlaytimeObservations.ToListAsync());
    }

    [Fact]
    public async Task LeaveAcceptedBeforeResolvedJoinClosesTheSessionOnEnrichment()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        var join = Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
            start, "guid-1") with
        {
            CanonicalPlayerId = null,
            ConnectionCorrelationId = correlationId
        };
        var leave = Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
            start.AddMinutes(10), "guid-1") with
        { ConnectionCorrelationId = correlationId };
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [leave, join] });
        Assert.Empty(await context.PlayerSessions.ToListAsync());

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations = [join with { CanonicalPlayerId = playerId }]
        });

        var session = Assert.Single(await context.PlayerSessions.ToListAsync());
        Assert.Equal(start, session.StartUtc);
        Assert.Equal(start.AddMinutes(10), session.EndUtc);
    }

    [Fact]
    public async Task LeaveWithMissingJoinCorrelationClosesMatchingSession()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var leaveCorrelationId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        var api = (IPlaytimeApi)new PlaytimeController(context);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(10), "guid-1") with { ConnectionCorrelationId = leaveCorrelationId }
            ]
        });

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1")
            ]
        });

        var session = Assert.Single(await context.PlayerSessions.ToListAsync());
        Assert.Equal(start, session.StartUtc);
        Assert.Equal(start.AddMinutes(10), session.EndUtc);
        Assert.True(session.IsFinal);
    }

    [Fact]
    public async Task PresenceAfterRecentInferredClosureReopensTheSameConnection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-30);
        var api = (IPlaytimeApi)new PlaytimeController(context);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1") with { ConnectionCorrelationId = correlationId }
            ]
        });
        var sessionId = (await context.PlayerSessions.SingleAsync()).SessionId;
        await PlaytimeSweeperHostedService.SweepAsync(context, start.AddMinutes(1), CancellationToken.None);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddMinutes(10), "guid-1") with { ConnectionCorrelationId = correlationId }
            ]
        });

        var session = await context.PlayerSessions.SingleAsync();
        Assert.Equal(sessionId, session.SessionId);
        Assert.Null(session.EndUtc);
        Assert.Equal(start.AddMinutes(10), session.LastConfirmedUtc);
    }

    [Fact]
    public async Task PresenceAfterInferredClosureAndRosterAbsenceStartsNewSession()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var processingUtc = DateTimeOffset.UtcNow;
        var start = processingUtc.UtcDateTime.AddSeconds(-40);
        var api = (IPlaytimeApi)new PlaytimeController(context, timeProvider: new MutableTimeProvider(processingUtc));
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1") with { ConnectionCorrelationId = correlationId }
            ]
        });
        var originalSessionId = (await context.PlayerSessions.SingleAsync()).SessionId;
        await PlaytimeSweeperHostedService.SweepAsync(context, start.AddSeconds(1), CancellationToken.None);
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start.AddSeconds(5),
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Complete,
                    Roster = []
                }
            ]
        });

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddSeconds(10), "guid-1") with { ConnectionCorrelationId = correlationId }
            ]
        });

        var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.Equal(originalSessionId, sessions[0].SessionId);
        Assert.Equal(start.AddSeconds(5), sessions[0].EndUtc);
        Assert.Equal((byte)PlaytimeTimestampQuality.RosterSnapshotTimestamp, sessions[0].EndQuality);
        Assert.Equal((byte)2, sessions[0].ClosureReason);
        Assert.Equal(start.AddSeconds(10), sessions[1].StartUtc);
        Assert.Null(sessions[1].EndUtc);
        Assert.NotEqual(sessions[0].SessionId, sessions[1].SessionId);
    }

    [Fact]
    public async Task StaleRetainedRosterDoesNotCloseDelayedJoin()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var processingUtc = DateTimeOffset.UtcNow;
        var timeProvider = new MutableTimeProvider(processingUtc);
        var api = (IPlaytimeApi)new PlaytimeController(context, timeProvider: timeProvider);
        var rosterUtc = processingUtc.UtcDateTime.AddSeconds(-30);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = rosterUtc,
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Complete,
                    Roster = []
                }
            ]
        });
        timeProvider.Advance(TimeSpan.FromSeconds(121));
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    rosterUtc.AddSeconds(-10), "guid-1")
            ]
        });

        var session = Assert.Single(await context.PlayerSessions.AsNoTracking().ToListAsync());
        Assert.Equal(rosterUtc.AddSeconds(-10), session.StartUtc);
        Assert.Null(session.EndUtc);
    }

    [Fact]
    public async Task DifferentKnownConnectionsSplitAndMismatchedLeaveDoesNotCloseNewConnection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var firstConnection = Guid.NewGuid();
        var secondConnection = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1") with { ConnectionCorrelationId = firstConnection },
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddMinutes(5), "guid-1") with
                {
                    ConnectionCorrelationId = secondConnection,
                    IsBot = true
                },
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(10), "guid-1") with { ConnectionCorrelationId = Guid.NewGuid() }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddMinutes(-1), "guid-1") with { ConnectionCorrelationId = firstConnection }
            ]
        });

        var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.Equal(start.AddMinutes(-1), sessions[0].StartUtc);
        Assert.Equal(start, sessions[0].EndUtc);
        Assert.Null(sessions[0].IsBot);
        Assert.Null(sessions[1].EndUtc);
        Assert.True(sessions[1].IsBot);
        Assert.Equal(secondConnection, sessions[1].ConnectionCorrelationId);
    }

    [Fact]
    public async Task DelayedJoinDoesNotMatchFutureSessionWithDifferentConnectionId()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var existingConnection = Guid.NewGuid();
        var delayedConnection = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var start = DateTime.UtcNow.AddMinutes(-20);
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddMinutes(10), "guid-1") with { ConnectionCorrelationId = existingConnection }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1") with { ConnectionCorrelationId = delayedConnection }
            ]
        });

        var sessions = await context.PlayerSessions.OrderBy(x => x.StartUtc).ToListAsync();
        Assert.Equal(2, sessions.Count);
        Assert.Equal(start, sessions[0].StartUtc);
        Assert.Equal(start.AddMinutes(10), sessions[0].EndUtc);
        Assert.Equal(delayedConnection, sessions[0].ConnectionCorrelationId);
        Assert.Equal(start.AddMinutes(10), sessions[1].StartUtc);
        Assert.Null(sessions[1].EndUtc);
        Assert.Equal(existingConnection, sessions[1].ConnectionCorrelationId);
    }

    [Fact]
    public async Task StaleRosterCanCorrectKnownPresenceWithoutClosingNewerSessions()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var player1 = Guid.NewGuid();
        var player2 = Guid.NewGuid();
        var player3 = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, player1, serverId);
        context.Players.AddRange(
            new Player
            {
                PlayerId = player2,
                GameType = (int)GameType.CallOfDuty4,
                Username = "Test 2",
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            },
            new Player
            {
                PlayerId = player3,
                GameType = (int)GameType.CallOfDuty4,
                Username = "Test 3",
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            });
        await context.SaveChangesAsync();
        var start = DateTime.UtcNow.AddSeconds(-90);
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, player1, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start, "guid-1"),
                Observation(serverId, player2, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start, "guid-2"),
                Observation(serverId, player3, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start.AddSeconds(45), "guid-3")
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start.AddSeconds(30),
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Complete,
                    Roster =
                    [
                        new PlaytimeRosterPlayerDto
                        {
                            CanonicalPlayerId = player1,
                            GamePlayerId = "guid-1",
                            SlotId = 1
                        }
                    ]
                }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                new PlaytimeObservationDto
                {
                    ObservationId = Guid.NewGuid(),
                    ServerId = serverId,
                    GameType = GameType.CallOfDuty4,
                    Kind = PlaytimeObservationKind.ServerRoster,
                    EventUtc = start,
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Complete,
                    Roster =
                    [
                        new PlaytimeRosterPlayerDto
                        {
                            CanonicalPlayerId = player1,
                            GamePlayerId = "guid-1",
                            SlotId = 1,
                            IsBot = true
                        }
                    ]
                }
            ]
        });

        var session1 = Assert.Single(await context.PlayerSessions.ToListAsync(), x => x.PlayerId == player1);
        var session2 = Assert.Single(await context.PlayerSessions.ToListAsync(), x => x.PlayerId == player2);
        var session3 = Assert.Single(await context.PlayerSessions.ToListAsync(), x => x.PlayerId == player3);
        Assert.True(session1.IsBot);
        Assert.Equal(start.AddSeconds(30), session2.EndUtc);
        Assert.Null(session3.EndUtc);
    }

    [Fact]
    public async Task LateSameTimestampMapSequenceDoesNotOverwriteNewerMap()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, playerId, serverId);
        var timestamp = DateTime.UtcNow.AddMinutes(-10);
        var epoch = Guid.NewGuid();
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged,
                    timestamp, null, "mp_b") with { ProducerEpoch = epoch, ProducerSequence = 2 }
            ]
        });
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged,
                    timestamp, null, "mp_a") with { ProducerEpoch = epoch, ProducerSequence = 1 }
            ]
        });

        var run = await context.ServerMapRuns.SingleAsync();
        Assert.Equal("mp_b", run.NormalizedMapKey);
    }

    [Fact]
    public async Task CallOfDuty4xMapChangeUsesCod4CatalogueAndDisplayName()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddMinutes(-20);
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4x,
            Username = "Test",
            FirstSeen = start,
            LastSeen = DateTime.UtcNow
        });
        context.GameServers.Add(new GameServer
        {
            GameServerId = serverId,
            Title = "Test",
            GameType = (int)GameType.CallOfDuty4x,
            Hostname = "127.0.0.1",
            QueryPort = 28960
        });
        var mapId = Guid.NewGuid();
        context.Maps.Add(new Map
        {
            MapId = mapId,
            GameType = (int)GameType.CallOfDuty4,
            MapName = "mp_test",
            DisplayName = "Test Map",
            MapStatus = 0,
            TotalLikes = 0,
            TotalDislikes = 0,
            TotalVotes = 0,
            LikePercentage = 0,
            DislikePercentage = 0
        });
        await context.SaveChangesAsync();
        var api = (IPlaytimeApi)new PlaytimeController(context);

        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    start, "guid-1") with { GameType = GameType.CallOfDuty4x },
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.MapChanged,
                    start, null, "Test Map") with { GameType = GameType.CallOfDuty4x },
                Observation(serverId, playerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft,
                    start.AddMinutes(10), "guid-1") with { GameType = GameType.CallOfDuty4x }
            ]
        });

        var run = await context.ServerMapRuns.SingleAsync();
        Assert.Equal(mapId, run.MapId);
        Assert.Equal((int)GameType.CallOfDuty4x, run.GameType);
        var totals = await api.GetTotals(
            start,
            start.AddMinutes(10),
            playerId,
            serverId,
            GameType.CallOfDuty4x,
            "mp_test");
        Assert.Equal(600, totals.Result?.Data?.MeasuredSeconds);
    }

    [Fact]
    public async Task RedactedDuplicateReplayEnrichesOnlyRetainedRosterEntries()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var deletedPlayerId = Guid.NewGuid();
        var retainedPlayerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, deletedPlayerId, serverId);
        context.Players.Add(new Player
        {
            PlayerId = retainedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Retained",
            Guid = "guid-retained",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        context.SaveChanges();

        var observation = new PlaytimeObservationDto
        {
            ObservationId = Guid.NewGuid(),
            ServerId = serverId,
            GameType = GameType.CallOfDuty4,
            Kind = PlaytimeObservationKind.ServerRoster,
            EventUtc = DateTime.UtcNow.AddMinutes(-10),
            TimestampQuality = PlaytimeTimestampQuality.ProducerTimestamp,
            RosterCompleteness = PlaytimeRosterCompleteness.Partial,
            Roster =
            [
                new PlaytimeRosterPlayerDto
                {
                    GamePlayerId = "guid-1",
                    SlotId = 1,
                    CanonicalPlayerId = deletedPlayerId
                },
                new PlaytimeRosterPlayerDto
                {
                    GamePlayerId = "guid-retained",
                    SlotId = 2
                }
            ]
        };
        var playtimeApi = (IPlaytimeApi)new PlaytimeController(context);
        var firstSubmission = await playtimeApi.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations = [observation]
        });
        var deleteApi = (IDataMaintenanceApi)new DataMaintenanceController(
            context,
            new ConfigurationBuilder().Build());
        var deletion = await deleteApi.DeletePlayer(deletedPlayerId);
        var sourceRoster = observation.Roster!;
        var replay = observation with
        {
            Roster =
            [
                sourceRoster[0],
                sourceRoster[1] with { CanonicalPlayerId = retainedPlayerId }
            ]
        };
        var replayResult = await ((IPlaytimeApi)new PlaytimeController(context))
            .BatchObservations(new PlaytimeObservationBatchDto
            {
                Observations = [replay]
            });

        Assert.Equal(PlaytimeObservationOutcome.Accepted,
            firstSubmission.Result?.Data?.Items.Single().Outcome);
        Assert.Equal(HttpStatusCode.OK, deletion.StatusCode);
        Assert.Equal(PlaytimeObservationOutcome.Duplicate,
            replayResult.Result?.Data?.Items.Single().Outcome);
        Assert.True(context.PlaytimeObservations.Single().IdentifiersRedacted);
        var resolvedRosterJson = context.PlaytimeObservations.Single().ResolvedRosterJson!;
        Assert.Contains("guid-retained", resolvedRosterJson, StringComparison.Ordinal);
        Assert.Contains(retainedPlayerId.ToString(), resolvedRosterJson, StringComparison.Ordinal);
        Assert.DoesNotContain("guid-1", resolvedRosterJson, StringComparison.Ordinal);
        Assert.DoesNotContain(deletedPlayerId.ToString(), resolvedRosterJson, StringComparison.Ordinal);
        var retainedSession = Assert.Single(await context.PlayerSessions.ToListAsync());
        Assert.Equal(retainedPlayerId, retainedSession.PlayerId);
    }

    [Fact]
    public async Task RedactedCompleteRosterReplayDoesNotCloseReplacementPlayer()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var deletedPlayerId = Guid.NewGuid();
        var retainedPlayerId = Guid.NewGuid();
        var replacementPlayerId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        AddPlayerAndServer(context, deletedPlayerId, serverId);
        context.Players.Add(new Player
        {
            PlayerId = retainedPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Retained",
            Guid = "guid-retained",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var processingUtc = DateTimeOffset.UtcNow;
        var api = (IPlaytimeApi)new PlaytimeController(context, timeProvider: new MutableTimeProvider(processingUtc));
        var roster = new PlaytimeObservationDto
        {
            ObservationId = Guid.NewGuid(),
            ServerId = serverId,
            GameType = GameType.CallOfDuty4,
            Kind = PlaytimeObservationKind.ServerRoster,
            EventUtc = processingUtc.UtcDateTime.AddSeconds(-30),
            TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
            RosterCompleteness = PlaytimeRosterCompleteness.Complete,
            Roster =
            [
                new PlaytimeRosterPlayerDto { GamePlayerId = "guid-1", SlotId = 1, CanonicalPlayerId = deletedPlayerId },
                new PlaytimeRosterPlayerDto { GamePlayerId = "guid-retained", SlotId = 2 }
            ]
        };
        await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [roster] });
        var deletion = await ((IDataMaintenanceApi)new DataMaintenanceController(context, new ConfigurationBuilder().Build()))
            .DeletePlayer(deletedPlayerId);
        context.Players.Add(new Player
        {
            PlayerId = replacementPlayerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Replacement",
            Guid = "guid-1",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                Observation(serverId, replacementPlayerId, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined,
                    processingUtc.UtcDateTime.AddSeconds(-40), "guid-1")
            ]
        });
        var sourceRoster = roster.Roster!;
        var replay = await api.BatchObservations(new PlaytimeObservationBatchDto
        {
            Observations =
            [
                roster with
                {
                    Roster = [sourceRoster[0], sourceRoster[1] with { CanonicalPlayerId = retainedPlayerId }]
                }
            ]
        });

        Assert.Equal(HttpStatusCode.OK, deletion.StatusCode);
        Assert.Equal(PlaytimeObservationOutcome.Duplicate, replay.Result?.Data?.Items.Single().Outcome);
        var replacementSession = await context.PlayerSessions.AsNoTracking()
            .SingleAsync(x => x.PlayerId == replacementPlayerId);
        Assert.Null(replacementSession.EndUtc);
        Assert.Single(await context.PlayerSessions.AsNoTracking().ToListAsync(), x => x.PlayerId == retainedPlayerId);
        var storedRoster = await context.PlaytimeObservations.AsNoTracking()
            .SingleAsync(x => x.ObservationId == roster.ObservationId);
        Assert.True(storedRoster.IdentifiersRedacted);
        Assert.Equal((byte)PlaytimeRosterCompleteness.Complete, storedRoster.RosterCompleteness);
        Assert.DoesNotContain("guid-1", storedRoster.ResolvedRosterJson!, StringComparison.Ordinal);
    }

    private static void AddPlayerAndServer(PortalDbContext context, Guid playerId, Guid serverId)
    {
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "Test",
            Guid = "guid-1",
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        });
        context.GameServers.Add(new GameServer
        {
            GameServerId = serverId,
            Title = "Test",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "127.0.0.1",
            QueryPort = 28960
        });
        context.SaveChanges();
    }

    private static PlaytimeObservationDto Observation(
        Guid serverId,
        Guid playerId,
        Guid observationId,
        PlaytimeObservationKind kind,
        DateTime eventUtc,
        string? gamePlayerId,
        string? mapName = null)
    {
        return new PlaytimeObservationDto
        {
            ObservationId = observationId,
            ServerId = serverId,
            GameType = GameType.CallOfDuty4,
            Kind = kind,
            EventUtc = eventUtc,
            TimestampQuality = PlaytimeTimestampQuality.ProducerTimestamp,
            CanonicalPlayerId = kind is PlaytimeObservationKind.PlayerJoined or PlaytimeObservationKind.PlayerLeft
                ? playerId
                : null,
            GamePlayerId = gamePlayerId,
            MapName = mapName,
            RosterCompleteness = PlaytimeRosterCompleteness.NotApplicable
        };
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan amount) => utcNow = utcNow.Add(amount);
    }
}
