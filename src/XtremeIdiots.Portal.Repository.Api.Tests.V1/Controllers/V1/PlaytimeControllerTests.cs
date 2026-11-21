using System.Net;
using Microsoft.EntityFrameworkCore;
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
    public async Task CompleteRosterClosesMissingPresenceButPartialRosterDoesNot()
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

        var start = DateTime.UtcNow.AddMinutes(-20);
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
                    EventUtc = start.AddMinutes(10),
                    TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
                    RosterCompleteness = PlaytimeRosterCompleteness.Partial,
                    Roster = [new PlaytimeRosterPlayerDto { CanonicalPlayerId = player1, GamePlayerId = "guid-1", SlotId = 1 }]
                }
            ]
        });
        var partialSessions = await context.PlayerSessions.ToListAsync();
        Assert.All(partialSessions, session => Assert.Null(session.EndUtc));
        var partialPlayer1 = Assert.Single(partialSessions, session => session.PlayerId == player1);
        Assert.Equal(start.AddMinutes(10), partialPlayer1.LastConfirmedUtc);

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
                    EventUtc = start.AddMinutes(15),
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
        Assert.Equal(start.AddMinutes(15), player2Session.EndUtc);
        Assert.Equal((byte)PlaytimeTimestampQuality.RosterSnapshotTimestamp, player2Session.EndQuality);
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
                Observation(serverId, unknownPlayer, Guid.NewGuid(), PlaytimeObservationKind.PlayerJoined, start.AddMinutes(10), "unknown") with { IsBot = null },
                Observation(serverId, unknownPlayer, Guid.NewGuid(), PlaytimeObservationKind.PlayerLeft, start.AddMinutes(20), "unknown") with { IsBot = null }
            ]
        });

        var totals = await api.GetTotals(start, start.AddMinutes(20), null, serverId, GameType.CallOfDuty4, null);
        Assert.NotNull(totals.Result);
        var data = totals.Result?.Data;
        Assert.NotNull(data);
        Assert.Equal(600, data!.KnownBotSeconds);
        Assert.Equal(600, data.UnclassifiedPlayerSeconds);
        Assert.False(data.HumanOnlyCoverage);
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
    public async Task StatusCoverageCountersAreReportedPerEpochWithoutLossInference()
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
                        ReportedUtc = start,
                        DroppedCapacityMax = 2,
                        DroppedAttemptsMax = 3,
                        DroppedAgeMax = 4
                    }
                }
            ]
        });

        var totals = await api.GetTotals(start.AddMinutes(-1), start.AddMinutes(1), null, serverId, GameType.CallOfDuty4, null);
        Assert.NotNull(totals.Result);
        var data = totals.Result?.Data;
        Assert.NotNull(data);
        var report = Assert.Single(data!.CoverageReports);
        Assert.Equal(epoch, report.ProducerEpoch);
        Assert.Equal(2, report.DroppedCapacityMax);
        Assert.Equal(3, report.DroppedAttemptsMax);
        Assert.Equal(4, report.DroppedAgeMax);
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

        var start = DateTime.UtcNow.AddMinutes(-20);
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
                    EventUtc = start.AddMinutes(10),
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
                    EventUtc = start.AddMinutes(15),
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
            EventUtc = start.AddMinutes(5),
            TimestampQuality = PlaytimeTimestampQuality.RosterSnapshotTimestamp,
            RosterCompleteness = PlaytimeRosterCompleteness.Complete,
            Roster = [new PlaytimeRosterPlayerDto { CanonicalPlayerId = player1, GamePlayerId = "guid-1", SlotId = 1 }]
        };
        await api.BatchObservations(new PlaytimeObservationBatchDto { Observations = [older] });
        var player2Session = Assert.Single(await context.PlayerSessions.ToListAsync(), x => x.PlayerId == player2);
        Assert.Equal(start.AddMinutes(15), player2Session.EndUtc);
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
}
