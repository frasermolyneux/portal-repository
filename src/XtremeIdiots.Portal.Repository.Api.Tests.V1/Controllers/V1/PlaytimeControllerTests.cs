using System.Net;
using Microsoft.EntityFrameworkCore;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
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
            StartQuality = 2,
            EndQuality = 2,
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
