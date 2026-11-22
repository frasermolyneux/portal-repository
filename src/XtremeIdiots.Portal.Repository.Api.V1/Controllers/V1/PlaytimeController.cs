using System.Net;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public sealed class PlaytimeController : ControllerBase, IPlaytimeApi
{
    private static readonly TimeSpan LateEventThreshold = TimeSpan.FromHours(24);
    private static readonly TimeSpan FutureEventThreshold = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CompleteRosterFreshnessThreshold = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan ReconnectGapThreshold = TimeSpan.FromMinutes(15);
    private readonly PortalDbContext context;
    private readonly ILogger<PlaytimeController>? logger;
    private readonly TimeProvider timeProvider;

    public PlaytimeController(
        PortalDbContext context,
        ILogger<PlaytimeController>? logger = null,
        TimeProvider? timeProvider = null)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.logger = logger;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    [HttpPost("playtime/observations:batch")]
    [ProducesResponseType<PlaytimeObservationBatchResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchObservations([FromBody] PlaytimeObservationBatchDto dto, CancellationToken cancellationToken)
    {
        var response = await ((IPlaytimeApi)this).BatchObservations(dto, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlaytimeObservationBatchResultDto>> IPlaytimeApi.BatchObservations(
        PlaytimeObservationBatchDto dto,
        CancellationToken cancellationToken)
    {
        if (dto?.Observations is null)
        {
            return new ApiResult<PlaytimeObservationBatchResultDto>(HttpStatusCode.BadRequest);
        }

        if (dto.Observations.Count == 0 || dto.Observations.Count > 100)
        {
            return new ApiResult<PlaytimeObservationBatchResultDto>(HttpStatusCode.BadRequest);
        }
        if (dto.Observations.Any(x => x is null))
        {
            return new ApiResult<PlaytimeObservationBatchResultDto>(HttpStatusCode.BadRequest);
        }

        var items = new List<PlaytimeObservationItemResultDto>(dto.Observations.Count);
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.ChangeTracker.Clear();
            items.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            foreach (var gameType in dto.Observations.Select(x => (int)x.GameType).Distinct().OrderBy(x => x))
            {
                await PlaytimeServerLock.AcquireGameAsync(context, gameType, exclusive: false, cancellationToken)
                    .ConfigureAwait(false);
            }
            foreach (var serverId in dto.Observations
                         .Select(x => x.ServerId)
                         .Distinct()
                         .OrderBy(x => x))
            {
                await PlaytimeServerLock.AcquireAsync(context, serverId, cancellationToken)
                    .ConfigureAwait(false);
            }
            foreach (var observationId in dto.Observations.Select(x => x.ObservationId).Distinct().OrderBy(x => x))
            {
                await PlaytimeServerLock.AcquireObservationAsync(context, observationId, cancellationToken)
                    .ConfigureAwait(false);
            }

            foreach (var observation in dto.Observations)
            {
                if (observation.Roster is not null &&
                    (observation.Roster.Any(x => x is null ||
                                                 string.IsNullOrWhiteSpace(x.GamePlayerId) ||
                                                 x.GamePlayerId.Length > 50) ||
                     observation.Roster.GroupBy(x => (x.GamePlayerId, x.SlotId)).Any(x => x.Count() > 1)))
                {
                    items.Add(Rejected(
                        observation.ObservationId,
                        "Roster entries must have unique game-player/slot identities with supported lengths."));
                    continue;
                }

                var now = timeProvider.GetUtcNow().UtcDateTime;
                var hash = ComputeHash(observation);
                var existing = await context.PlaytimeObservations
                    .FirstOrDefaultAsync(x => x.ObservationId == observation.ObservationId, cancellationToken)
                    .ConfigureAwait(false);

                if (existing is not null)
                {
                    if (existing.ResolutionDeadlineUtc is not null &&
                        now > existing.ResolutionDeadlineUtc.Value)
                    {
                        items.Add(new PlaytimeObservationItemResultDto
                        {
                            ObservationId = observation.ObservationId,
                            Outcome = PlaytimeObservationOutcome.Rejected,
                            Reason = "Replay or identity resolution arrived after the accepted correction window."
                        });
                        continue;
                    }
                    var outcome = CryptographicOperations.FixedTimeEquals(existing.PayloadHash, hash)
                        ? PlaytimeObservationOutcome.Duplicate
                        : PlaytimeObservationOutcome.Rejected;
                    items.Add(new PlaytimeObservationItemResultDto
                    {
                        ObservationId = observation.ObservationId,
                        Outcome = outcome,
                        Reason = outcome == PlaytimeObservationOutcome.Rejected
                            ? "ObservationId was reused for conflicting payload data."
                            : null
                    });
                    IReadOnlyCollection<RosterResolution> previousRoster = [];
                    var replayObservation = observation;
                    if (outcome == PlaytimeObservationOutcome.Duplicate)
                    {
                        previousRoster = DeserializeRosterResolutions(existing.ResolvedRosterJson);
                        if (existing.IdentifiersRedacted)
                        {
                            replayObservation = await FilterRedactedReplay(
                                existing,
                                observation,
                                previousRoster,
                                cancellationToken).ConfigureAwait(false);
                        }
                    }
                    if (outcome == PlaytimeObservationOutcome.Duplicate &&
                        !await HasValidCanonicalPlayers(replayObservation, cancellationToken).ConfigureAwait(false))
                    {
                        items[^1] = Rejected(
                            observation.ObservationId,
                            "Canonical player is unknown or belongs to another game.");
                        continue;
                    }
                    if (outcome == PlaytimeObservationOutcome.Duplicate)
                    {
                        if (HasConflictingResolution(existing, replayObservation, previousRoster))
                        {
                            items[^1] = Rejected(
                                observation.ObservationId,
                                "Replay conflicts with a previously resolved player identity or bot classification.");
                            continue;
                        }

                        var identityResolved = replayObservation.CanonicalPlayerId is not null &&
                                               existing.CanonicalPlayerId is null;
                        var botResolved = replayObservation.IsBot is not null &&
                                          existing.IsBot is null;
                        var rosterResolutionsChanged = false;
                        var mergedRoster = previousRoster;
                        if (replayObservation.Roster is not null)
                        {
                            var merged = MergeRosterResolutions(mergedRoster, replayObservation.Roster);
                            rosterResolutionsChanged = !AreRosterResolutionsEqual(mergedRoster, merged);
                            mergedRoster = merged;
                            existing.ResolvedRosterJson = SerializeRosterResolutions(mergedRoster);
                        }
                        if (identityResolved)
                        {
                            if (replayObservation.CanonicalPlayerId is not null)
                            {
                                existing.CanonicalPlayerId = replayObservation.CanonicalPlayerId;
                            }
                        }
                        if (botResolved)
                        {
                            existing.IsBot = replayObservation.IsBot;
                        }
                        if (identityResolved || botResolved || rosterResolutionsChanged)
                        {
                            var resolvedObservation = replayObservation with
                            {
                                CanonicalPlayerId = existing.CanonicalPlayerId,
                                IsBot = existing.IsBot,
                                Roster = replayObservation.Roster is null
                                    ? null
                                    : mergedRoster.Select(x => new PlaytimeRosterPlayerDto
                                    {
                                        GamePlayerId = x.GamePlayerId,
                                        SlotId = x.SlotId,
                                        CanonicalPlayerId = x.CanonicalPlayerId,
                                        IsBot = x.IsBot
                                    }).ToList()
                            };
                            await ReconcileObservation(resolvedObservation, now, cancellationToken, isReplay: true).ConfigureAwait(false);
                            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                    continue;
                }

                var server = await context.GameServers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.GameServerId == observation.ServerId, cancellationToken)
                    .ConfigureAwait(false);
                if (server is null)
                {
                    items.Add(Rejected(observation.ObservationId, "Unknown server."));
                    continue;
                }
                if (server.GameType != (int)observation.GameType)
                {
                    items.Add(Rejected(observation.ObservationId, "Server and game type are inconsistent."));
                    continue;
                }
                if (observation.ObservationId == Guid.Empty)
                {
                    items.Add(Rejected(observation.ObservationId, "ObservationId is required."));
                    continue;
                }
                if (observation.EventUtc == default)
                {
                    items.Add(Rejected(observation.ObservationId, "EventUtc is required."));
                    continue;
                }
                if (!Enum.IsDefined(observation.Kind))
                {
                    items.Add(Rejected(observation.ObservationId, "Unknown observation kind."));
                    continue;
                }
                if (!Enum.IsDefined(observation.TimestampQuality))
                {
                    items.Add(Rejected(observation.ObservationId, "Unknown timestamp quality."));
                    continue;
                }
                if (!Enum.IsDefined(observation.RosterCompleteness))
                {
                    items.Add(Rejected(observation.ObservationId, "Unknown roster completeness."));
                    continue;
                }
                if (observation.Kind == PlaytimeObservationKind.MapChanged &&
                    string.IsNullOrWhiteSpace(observation.MapName))
                {
                    items.Add(Rejected(observation.ObservationId, "MapChanged observations require MapName."));
                    continue;
                }
                if (observation.Kind == PlaytimeObservationKind.ServerRoster &&
                    observation.RosterCompleteness == PlaytimeRosterCompleteness.Complete &&
                    observation.Roster is null)
                {
                    items.Add(Rejected(observation.ObservationId, "A complete roster requires Roster entries."));
                    continue;
                }
                if (observation.Coverage is { } coverage &&
                    (coverage.ProducerEpoch == Guid.Empty ||
                     coverage.ProducerEpoch != observation.ProducerEpoch ||
                     coverage.ReportedUtc == default ||
                     coverage.DroppedCapacityMax < 0 ||
                     coverage.DroppedAttemptsMax < 0 ||
                     coverage.DroppedAgeMax < 0))
                {
                    items.Add(Rejected(observation.ObservationId, "Coverage requires a matching producer epoch, report timestamp, and nonnegative counters."));
                    continue;
                }

                var eventUtc = NormalizeUtc(observation.EventUtc);
                if (now - eventUtc > LateEventThreshold)
                {
                    items.Add(Rejected(observation.ObservationId, "Event is too late for the accepted correction window."));
                    continue;
                }
                if (eventUtc - now > FutureEventThreshold)
                {
                    items.Add(Rejected(observation.ObservationId, "Event is beyond the accepted clock-skew allowance."));
                    continue;
                }
                if (observation.GamePlayerId?.Length > 50 || observation.MapName?.Length > 256)
                {
                    items.Add(Rejected(observation.ObservationId, "GamePlayerId or MapName exceeds the supported length."));
                    continue;
                }
                if (!await HasValidCanonicalPlayers(observation, cancellationToken).ConfigureAwait(false))
                {
                    items.Add(Rejected(observation.ObservationId, "Canonical player is unknown or belongs to another game."));
                    continue;
                }
                if (observation.ProducerEpoch is not null && observation.ProducerSequence is not null)
                {
                    var sequenceExists = await context.PlaytimeObservations
                        .AsNoTracking()
                        .AnyAsync(x => x.ServerId == observation.ServerId &&
                                       x.ProducerEpoch == observation.ProducerEpoch &&
                                       x.ProducerSequence == observation.ProducerSequence,
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (sequenceExists)
                    {
                        items.Add(Rejected(observation.ObservationId, "Producer sequence was reused."));
                        continue;
                    }
                }

                var fact = new PlaytimeObservation
                {
                    ObservationId = observation.ObservationId,
                    ServerId = observation.ServerId,
                    GameType = (int)observation.GameType,
                    Kind = (byte)observation.Kind,
                    EventUtc = eventUtc,
                    ReceivedUtc = now,
                    TimestampQuality = (byte)observation.TimestampQuality,
                    ProducerEpoch = observation.ProducerEpoch,
                    ProducerSequence = observation.ProducerSequence,
                    ConnectionCorrelationId = observation.ConnectionCorrelationId,
                    CanonicalPlayerId = observation.CanonicalPlayerId,
                    GamePlayerId = observation.GamePlayerId,
                    SlotId = observation.SlotId,
                    MapName = observation.MapName,
                    IsBot = observation.IsBot,
                    ProducerStartedUtc = observation.Coverage?.ProducerStartedUtc,
                    CoverageReportedUtc = observation.Coverage is null ? null : NormalizeUtc(observation.Coverage.ReportedUtc),
                    DroppedCapacityMax = observation.Coverage?.DroppedCapacityMax,
                    DroppedAttemptsMax = observation.Coverage?.DroppedAttemptsMax,
                    DroppedAgeMax = observation.Coverage?.DroppedAgeMax,
                    RosterCompleteness = (byte)observation.RosterCompleteness,
                    ResolvedRosterJson = SerializeRosterResolutions(
                        observation.Roster?.Select(x => new RosterResolution
                        {
                            GamePlayerId = x.GamePlayerId,
                            SlotId = x.SlotId,
                            CanonicalPlayerId = x.CanonicalPlayerId,
                            IsBot = x.IsBot
                        }).ToList() ?? []),
                    ResolutionDeadlineUtc = now.Add(LateEventThreshold),
                    PayloadHash = hash,
                    Outcome = (byte)PlaytimeObservationOutcome.Accepted
                };

                context.PlaytimeObservations.Add(fact);
                await ReconcileObservation(observation, now, cancellationToken).ConfigureAwait(false);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                items.Add(new PlaytimeObservationItemResultDto
                {
                    ObservationId = observation.ObservationId,
                    Outcome = PlaytimeObservationOutcome.Accepted
                });
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
        var result = new PlaytimeObservationBatchResultDto { Items = items };
        return new ApiResponse<PlaytimeObservationBatchResultDto>(result).ToApiResult();
    }

    [HttpGet("players/{playerId:guid}/sessions")]
    [ProducesResponseType<IReadOnlyList<PlayerSessionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerSessions(Guid playerId, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, [FromQuery] int skipEntries = 0, [FromQuery] int takeEntries = 100, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlaytimeApi)this).GetPlayerSessions(
            playerId,
            fromUtc,
            toUtc,
            skipEntries,
            takeEntries,
            cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<IReadOnlyList<PlayerSessionDto>>> IPlaytimeApi.GetPlayerSessions(
        Guid playerId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skipEntries,
        int takeEntries,
        CancellationToken cancellationToken)
    {
        if (!await context.Players.AsNoTracking().AnyAsync(x => x.PlayerId == playerId, cancellationToken).ConfigureAwait(false))
        {
            return new ApiResult<IReadOnlyList<PlayerSessionDto>>(HttpStatusCode.NotFound);
        }

        if (skipEntries < 0 || takeEntries is < 1 or > 100)
        {
            return new ApiResult<IReadOnlyList<PlayerSessionDto>>(HttpStatusCode.BadRequest);
        }

        if (fromUtc is not null && toUtc is not null && fromUtc >= toUtc)
        {
            return new ApiResult<IReadOnlyList<PlayerSessionDto>>(HttpStatusCode.BadRequest);
        }

        var query = context.PlayerSessions.AsNoTracking().Where(x => x.PlayerId == playerId);
        if (fromUtc is not null)
        {
            query = query.Where(x => x.EndUtc == null || x.EndUtc > fromUtc.Value);
        }

        if (toUtc is not null)
        {
            query = query.Where(x => x.StartUtc < toUtc.Value);
        }

        var sessions = await query
            .OrderByDescending(x => x.StartUtc)
            .ThenBy(x => x.SessionId)
            .Skip(skipEntries)
            .Take(takeEntries)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var knownSegments = new List<(Guid SessionId, DateTime StartUtc, DateTime EndUtc)>();
        if (sessions.Count > 0)
        {
            var segmentRows = await context.PlayerSessionMapSegments
                .AsNoTracking()
                .Where(x => !x.UnknownMap && sessions.Select(s => s.SessionId).Contains(x.SessionId))
                .Select(x => new { x.SessionId, x.StartUtc, x.EndUtc })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            knownSegments.AddRange(segmentRows.Select(x => (x.SessionId, x.StartUtc, x.EndUtc)));
        }

        var sessionsById = sessions.ToDictionary(x => x.SessionId);
        var measuredBySession = knownSegments
            .GroupBy(x => x.SessionId)
            .ToDictionary(
                group => group.Key,
                group => (long)(group.Sum(segment =>
                {
                    var session = sessionsById[group.Key];
                    var start = segment.StartUtc > session.StartUtc ? segment.StartUtc : session.StartUtc;
                    var sessionEnd = session.EndUtc ?? session.LastConfirmedUtc;
                    var end = segment.EndUtc < sessionEnd ? segment.EndUtc : sessionEnd;
                    return end > start ? (decimal)(end - start).Ticks : 0;
                }) / TimeSpan.TicksPerSecond));

        var dto = new List<PlayerSessionDto>(sessions.Count);
        foreach (var session in sessions)
        {
            var measured = session.IsBot == true
                ? 0
                : measuredBySession.GetValueOrDefault(session.SessionId);
            dto.Add(new PlayerSessionDto
            {
                SessionId = session.SessionId,
                ServerId = session.ServerId,
                GameType = (GameType)session.GameType,
                StartUtc = session.StartUtc,
                EndUtc = session.EndUtc,
                MeasuredSeconds = measured,
                ObservedSeconds = session.IsBot == true ? 0 : ObservedSeconds(session),
                LastConfirmedUtc = session.LastConfirmedUtc,
                IsOpen = session.EndUtc is null,
                IsBot = session.IsBot,
                StartQuality = (PlaytimeTimestampQuality)session.StartQuality,
                EndQuality = session.EndQuality is null ? null : (PlaytimeTimestampQuality)session.EndQuality
            });
        }

        return new ApiResponse<IReadOnlyList<PlayerSessionDto>>(dto).ToApiResult();
    }

    [HttpPost("servers/{serverId:guid}/playtime/current-sessions:batch")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<CurrentPlayerSessionResultDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentSessions(
        Guid serverId,
        [FromBody] CurrentPlayerSessionsRequestDto dto,
        CancellationToken cancellationToken)
    {
        var response = await ((IPlaytimeApi)this).GetCurrentSessions(serverId, dto, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<IReadOnlyList<CurrentPlayerSessionResultDto>>> IPlaytimeApi.GetCurrentSessions(
        Guid serverId,
        CurrentPlayerSessionsRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (serverId == Guid.Empty || dto?.PlayerIds is null || dto.PlayerIds.Count is < 1 or > 100 ||
            dto.PlayerIds.Any(x => x == Guid.Empty) || dto.PlayerIds.Distinct().Count() != dto.PlayerIds.Count)
        {
            return new ApiResult<IReadOnlyList<CurrentPlayerSessionResultDto>>(HttpStatusCode.BadRequest);
        }

        if (!await context.GameServers.AsNoTracking().AnyAsync(x => x.GameServerId == serverId, cancellationToken).ConfigureAwait(false))
        {
            return new ApiResult<IReadOnlyList<CurrentPlayerSessionResultDto>>(HttpStatusCode.NotFound);
        }

        var playerIds = dto.PlayerIds.ToArray();
        var groups = await context.PlayerSessions.AsNoTracking()
            .Where(x => x.ServerId == serverId && x.EndUtc == null && playerIds.Contains(x.PlayerId))
            .GroupBy(x => x.PlayerId)
            .Select(group => new
            {
                PlayerId = group.Key,
                Count = group.Count(),
                Session = group.OrderBy(x => x.SessionId).First()
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var byPlayer = groups.ToDictionary(x => x.PlayerId);
        var results = new List<CurrentPlayerSessionResultDto>(playerIds.Length);
        foreach (var playerId in playerIds)
        {
            if (!byPlayer.TryGetValue(playerId, out var group))
            {
                results.Add(new CurrentPlayerSessionResultDto { PlayerId = playerId, Status = CurrentPlayerSessionStatus.Unavailable });
            }
            else if (group.Count != 1)
            {
                results.Add(new CurrentPlayerSessionResultDto { PlayerId = playerId, Status = CurrentPlayerSessionStatus.Ambiguous });
            }
            else if (group.Session.IsBot == true || group.Session.LastConfirmedUtc < group.Session.StartUtc)
            {
                results.Add(new CurrentPlayerSessionResultDto { PlayerId = playerId, Status = CurrentPlayerSessionStatus.Unavailable });
            }
            else
            {
                var session = group.Session;
                results.Add(new CurrentPlayerSessionResultDto
                {
                    PlayerId = playerId,
                    Status = CurrentPlayerSessionStatus.Found,
                    Session = new CurrentPlayerSessionDto
                    {
                        SessionId = session.SessionId,
                        ServerId = session.ServerId,
                        GameType = (GameType)session.GameType,
                        ConnectionCorrelationId = session.ConnectionCorrelationId,
                        StartUtc = session.StartUtc,
                        LastConfirmedUtc = session.LastConfirmedUtc,
                        ObservedSeconds = ObservedSeconds(session),
                        IsBot = session.IsBot,
                        StartQuality = (PlaytimeTimestampQuality)session.StartQuality
                    }
                });
            }
        }

        return new ApiResponse<IReadOnlyList<CurrentPlayerSessionResultDto>>(results).ToApiResult();
    }

    private static long ObservedSeconds(PlayerSession session) =>
        session.LastConfirmedUtc > session.StartUtc
            ? (session.LastConfirmedUtc - session.StartUtc).Ticks / TimeSpan.TicksPerSecond
            : 0;

    [HttpGet("playtime/totals")]
    [ProducesResponseType<PlaytimeTotalsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTotals([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, [FromQuery] Guid? playerId, [FromQuery] Guid? serverId, [FromQuery] GameType? gameType, [FromQuery] string? mapName, CancellationToken cancellationToken)
    {
        var response = await ((IPlaytimeApi)this).GetTotals(
            fromUtc,
            toUtc,
            playerId,
            serverId,
            gameType,
            mapName,
            cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlaytimeTotalsDto>> IPlaytimeApi.GetTotals(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? playerId,
        Guid? serverId,
        GameType? gameType,
        string? mapName,
        CancellationToken cancellationToken)
    {
        if (fromUtc == default || toUtc == default || fromUtc >= toUtc)
        {
            return new ApiResult<PlaytimeTotalsDto>(HttpStatusCode.BadRequest);
        }

        var segments = context.PlayerSessionMapSegments
            .AsNoTracking()
            .Where(x => x.StartUtc < toUtc && x.EndUtc > fromUtc);

        if (playerId is not null)
        {
            segments = segments.Where(x => x.Session.PlayerId == playerId);
        }

        if (serverId is not null)
        {
            segments = segments.Where(x => x.Session.ServerId == serverId);
        }

        if (gameType is not null)
        {
            segments = segments.Where(x => x.Session.GameType == (int)gameType);
        }

        if (!string.IsNullOrWhiteSpace(mapName))
        {
            var normalizedMapName = NormalizeMapKey(mapName);
            segments = segments.Where(x => !x.UnknownMap &&
                ((x.MapRun!.Map != null &&
                  (x.MapRun.Map.MapName == mapName || x.MapRun.Map.DisplayName == mapName)) ||
                 x.MapRun!.NormalizedMapKey == normalizedMapName));
        }

        long knownSeconds = 0;
        long unknownSeconds = 0;
        long knownBotSeconds = 0;
        long unclassifiedSeconds = 0;
        bool hasKnownNonBot;
        bool hasUnclassified;
        if (context.Database.IsSqlServer())
        {
            var aggregates = await context.Database.SqlQueryRaw<PlaytimeTotalsAggregate>(
                    """
                    WITH clipped AS
                    (
                        SELECT session.IsBot,
                               segment.UnknownMap,
                               CASE WHEN segment.StartUtc < @FromUtc THEN @FromUtc ELSE segment.StartUtc END AS ClippedStartUtc,
                               CASE WHEN segment.EndUtc > @ToUtc THEN @ToUtc ELSE segment.EndUtc END AS ClippedEndUtc
                        FROM dbo.PlayerSessionMapSegment AS segment
                        INNER JOIN dbo.PlayerSession AS session ON session.SessionId = segment.SessionId
                        LEFT JOIN dbo.ServerMapRun AS mapRun ON mapRun.RunId = segment.MapRunId
                        LEFT JOIN dbo.Maps AS map ON map.MapId = mapRun.MapId
                        WHERE segment.StartUtc < @ToUtc
                          AND segment.EndUtc > @FromUtc
                          AND (@PlayerId IS NULL OR session.PlayerId = @PlayerId)
                          AND (@ServerId IS NULL OR session.ServerId = @ServerId)
                          AND (@GameType IS NULL OR session.GameType = @GameType)
                          AND (@MapName IS NULL OR
                               (segment.UnknownMap = 0 AND
                                (map.MapName = @MapName OR map.DisplayName = @MapName OR
                                 mapRun.NormalizedMapKey = @NormalizedMapName)))
                    ),
                    durations AS
                    (
                        SELECT IsBot,
                               UnknownMap,
                               CASE WHEN ClippedEndUtc > ClippedStartUtc
                                    THEN CONVERT(decimal(38, 0), DATEDIFF_BIG(NANOSECOND, ClippedStartUtc, ClippedEndUtc))
                                    ELSE CONVERT(decimal(38, 0), 0) END AS Nanoseconds
                        FROM clipped
                    )
                    , totals AS
                    (
                        SELECT COALESCE(SUM(CASE WHEN IsBot = 1 THEN Nanoseconds ELSE 0 END), 0) AS BotNs,
                               COALESCE(SUM(CASE WHEN IsBot IS NULL THEN Nanoseconds ELSE 0 END), 0) AS UnclassifiedNs,
                               COALESCE(SUM(CASE WHEN (IsBot = 0 OR IsBot IS NULL) AND UnknownMap = 1
                                                 THEN Nanoseconds ELSE 0 END), 0) AS UnknownNs,
                               COALESCE(SUM(CASE WHEN (IsBot = 0 OR IsBot IS NULL) AND UnknownMap = 0
                                                 THEN Nanoseconds ELSE 0 END), 0) AS KnownNs,
                               CAST(CASE WHEN MAX(CASE WHEN IsBot = 0 AND Nanoseconds > 0 THEN 1 ELSE 0 END) = 1
                                         THEN 1 ELSE 0 END AS bit) AS HasKnownNonBot
                        FROM durations
                    )
                    SELECT CONVERT(bigint, (BotNs - BotNs % 1000000000) / 1000000000) AS KnownBotSeconds,
                           CONVERT(bigint, (UnclassifiedNs - UnclassifiedNs % 1000000000) / 1000000000) AS UnclassifiedSeconds,
                           CONVERT(bigint, (UnknownNs - UnknownNs % 1000000000) / 1000000000) AS UnknownMapSeconds,
                           CONVERT(bigint, (KnownNs - KnownNs % 1000000000) / 1000000000) AS KnownSeconds,
                           HasKnownNonBot,
                           CAST(CASE WHEN UnclassifiedNs > 0 THEN 1 ELSE 0 END AS bit) AS HasUnclassified
                    FROM totals
                    """,
                    new SqlParameter("@FromUtc", SqlDbType.DateTime2) { Value = fromUtc },
                    new SqlParameter("@ToUtc", SqlDbType.DateTime2) { Value = toUtc },
                    new SqlParameter("@PlayerId", SqlDbType.UniqueIdentifier) { Value = (object?)playerId ?? DBNull.Value },
                    new SqlParameter("@ServerId", SqlDbType.UniqueIdentifier) { Value = (object?)serverId ?? DBNull.Value },
                    new SqlParameter("@GameType", SqlDbType.Int)
                    {
                        Value = gameType is null ? DBNull.Value : (int)gameType.Value
                    },
                    new SqlParameter("@MapName", SqlDbType.NVarChar, -1)
                    {
                        Value = string.IsNullOrWhiteSpace(mapName) ? DBNull.Value : mapName
                    },
                    new SqlParameter("@NormalizedMapName", SqlDbType.NVarChar, -1)
                    {
                        Value = string.IsNullOrWhiteSpace(mapName) ? DBNull.Value : NormalizeMapKey(mapName)
                    })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var aggregate = aggregates.Single();
            knownSeconds = aggregate.KnownSeconds;
            unknownSeconds = aggregate.UnknownMapSeconds;
            knownBotSeconds = aggregate.KnownBotSeconds;
            unclassifiedSeconds = aggregate.UnclassifiedSeconds;
            hasKnownNonBot = aggregate.HasKnownNonBot;
            hasUnclassified = aggregate.HasUnclassified;
        }
        else
        {
            decimal knownTicks = 0;
            decimal unknownTicks = 0;
            decimal knownBotTicks = 0;
            decimal unclassifiedTicks = 0;
            var list = await segments
                .Include(x => x.Session)
                .Include(x => x.MapRun)
                .ThenInclude(x => x!.Map)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            hasKnownNonBot = false;
            foreach (var segment in list)
            {
                var start = segment.StartUtc < fromUtc ? fromUtc : segment.StartUtc;
                var end = segment.EndUtc > toUtc ? toUtc : segment.EndUtc;
                if (end <= start)
                {
                    continue;
                }

                var ticks = (end - start).Ticks;
                if (segment.Session.IsBot == true)
                {
                    knownBotTicks += ticks;
                    continue;
                }
                if (segment.Session.IsBot is null)
                {
                    unclassifiedTicks += ticks;
                }
                else
                {
                    hasKnownNonBot = true;
                }
                if (segment.UnknownMap)
                {
                    unknownTicks += ticks;
                }
                else
                {
                    knownTicks += ticks;
                }
            }
            knownSeconds = (long)(knownTicks / TimeSpan.TicksPerSecond);
            unknownSeconds = (long)(unknownTicks / TimeSpan.TicksPerSecond);
            knownBotSeconds = (long)(knownBotTicks / TimeSpan.TicksPerSecond);
            unclassifiedSeconds = (long)(unclassifiedTicks / TimeSpan.TicksPerSecond);
            hasUnclassified = unclassifiedTicks > 0;
        }

        var dataThrough = await context.PlaytimeObservations
            .AsNoTracking()
            .Where(x => x.Outcome == (byte)PlaytimeObservationOutcome.Accepted)
            .Where(x => (!serverId.HasValue || x.ServerId == serverId) &&
                        (!gameType.HasValue || x.GameType == (int)gameType))
            .Select(x => (DateTime?)x.EventUtc)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);

        var coverageReports = await context.PlaytimeObservations
            .AsNoTracking()
            .Where(x => x.ProducerEpoch != null && x.CoverageReportedUtc != null &&
                        (x.DroppedCapacityMax != null ||
                         x.DroppedAttemptsMax != null ||
                         x.DroppedAgeMax != null))
            .Where(x => (!serverId.HasValue || x.ServerId == serverId) &&
                        (!gameType.HasValue || x.GameType == (int)gameType))
            .GroupBy(x => new { x.ServerId, ProducerEpoch = x.ProducerEpoch!.Value })
            .Select(group => new PlaytimeCoverageReportDto
            {
                ServerId = group.Key.ServerId,
                ProducerEpoch = group.Key.ProducerEpoch,
                ProducerStartedUtc = group
                        .OrderByDescending(x => x.CoverageReportedUtc)
                        .ThenByDescending(x => x.ObservationId)
                        .Select(x => x.ProducerStartedUtc)
                        .FirstOrDefault(),
                ReportedUtc = group.Max(x => x.CoverageReportedUtc)!.Value,
                DroppedCapacityMax = group.Max(x => x.DroppedCapacityMax ?? 0),
                DroppedAttemptsMax = group.Max(x => x.DroppedAttemptsMax ?? 0),
                DroppedAgeMax = group.Max(x => x.DroppedAgeMax ?? 0)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        coverageReports = coverageReports
            .OrderBy(x => x.ReportedUtc)
            .ThenBy(x => x.ServerId)
            .ThenBy(x => x.ProducerEpoch)
            .ToList();

        var result = new PlaytimeTotalsDto
        {
            MeasuredSeconds = knownSeconds,
            UnknownMapSeconds = unknownSeconds,
            KnownBotSeconds = knownBotSeconds,
            UnclassifiedPlayerSeconds = unclassifiedSeconds,
            HumanOnlyCoverage = hasKnownNonBot && !hasUnclassified,
            DataThroughUtc = dataThrough ?? DateTime.UnixEpoch,
            PlayerId = playerId,
            ServerId = serverId,
            GameType = gameType,
            MapName = mapName,
            CoverageReports = coverageReports
        };
        return new ApiResponse<PlaytimeTotalsDto>(result).ToApiResult();
    }

    private async Task ReconcileObservation(
        PlaytimeObservationDto observation,
        DateTime receivedUtc,
        CancellationToken cancellationToken,
        bool isReplay = false)
    {
        var eventUtc = NormalizeUtc(observation.EventUtc);
        if (observation.Kind == PlaytimeObservationKind.PlayerJoined)
        {
            if (observation.CanonicalPlayerId is null)
            {
                return;
            }

            await EnsureSession(observation.CanonicalPlayerId.Value, observation, eventUtc, isReplay: isReplay, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        else if (observation.Kind == PlaytimeObservationKind.PlayerLeft)
        {
            if (observation.CanonicalPlayerId is null)
            {
                return;
            }

            await CloseSession(observation.CanonicalPlayerId.Value, observation.ServerId, eventUtc,
                (byte)observation.TimestampQuality,
                1,
                observation.ConnectionCorrelationId,
                observation.IsBot,
                cancellationToken)
                .ConfigureAwait(false);
        }
        else if (observation.Kind == PlaytimeObservationKind.MapChanged)
        {
            await ChangeMapRun(observation, eventUtc, cancellationToken).ConfigureAwait(false);
        }
        else if (observation.Kind == PlaytimeObservationKind.ServerRoster)
        {
            await ReconcileRoster(observation, eventUtc, receivedUtc, cancellationToken).ConfigureAwait(false);
            await ChangeMapRun(observation, eventUtc, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EnsureSession(
        Guid playerId,
        PlaytimeObservationDto observation,
        DateTime eventUtc,
        bool allowCreate = true,
        bool isReplay = false,
        CancellationToken cancellationToken = default)
    {
        PlayerSession? correlated = null;
        if (observation.ConnectionCorrelationId is Guid correlationId)
        {
            var correlatedSessions = context.PlayerSessions
                .Where(x => x.ConnectionCorrelationId == correlationId &&
                            x.PlayerId == playerId &&
                            x.ServerId == observation.ServerId);
            correlated = await correlatedSessions
                .Where(x => x.StartUtc <= eventUtc &&
                            (x.EndUtc == null
                                ? x.LastConfirmedUtc >= eventUtc
                                : x.EndUtc > eventUtc ||
                                  (x.EndUtc == eventUtc &&
                                   (observation.Kind != PlaytimeObservationKind.PlayerJoined ||
                                    (isReplay && x.StartUtc == eventUtc)))))
                .OrderByDescending(x => x.StartUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            correlated ??= await correlatedSessions
                .Where(x => x.StartUtc > eventUtc && x.StartUtc <= eventUtc.Add(LateEventThreshold))
                .OrderBy(x => x.StartUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        if (correlated is null && !string.IsNullOrWhiteSpace(observation.GamePlayerId))
        {
            var matchingGamePlayerSessions = context.PlayerSessions
                .Where(x => x.PlayerId == playerId &&
                            x.ServerId == observation.ServerId &&
                            x.GamePlayerId == observation.GamePlayerId);
            correlated = await matchingGamePlayerSessions
                .Where(x => x.StartUtc <= eventUtc &&
                            (x.EndUtc == null
                                ? x.LastConfirmedUtc >= eventUtc
                                : x.EndUtc > eventUtc ||
                                  (x.EndUtc == eventUtc &&
                                   (observation.Kind != PlaytimeObservationKind.PlayerJoined ||
                                    (isReplay && x.StartUtc == eventUtc)))) &&
                            (observation.ConnectionCorrelationId == null ||
                             x.ConnectionCorrelationId == null ||
                             x.ConnectionCorrelationId == observation.ConnectionCorrelationId))
                .OrderByDescending(x => x.StartUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            correlated ??= await matchingGamePlayerSessions
                .Where(x => x.StartUtc > eventUtc &&
                            x.StartUtc <= eventUtc.Add(LateEventThreshold) &&
                            (observation.ConnectionCorrelationId == null ||
                             x.ConnectionCorrelationId == null ||
                             x.ConnectionCorrelationId == observation.ConnectionCorrelationId))
                .OrderBy(x => x.StartUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        if (correlated is null &&
            observation.ConnectionCorrelationId is null &&
            string.IsNullOrWhiteSpace(observation.GamePlayerId))
        {
            var matchingSessions = await context.PlayerSessions
                .Where(x => x.PlayerId == playerId &&
                            x.ServerId == observation.ServerId &&
                            (x.StartUtc == eventUtc ||
                             (x.StartUtc < eventUtc &&
                              (x.EndUtc == null ? x.LastConfirmedUtc >= eventUtc : x.EndUtc > eventUtc))) &&
                            (x.EndUtc == null || x.EndUtc > eventUtc ||
                             (x.EndUtc == eventUtc &&
                              (observation.Kind != PlaytimeObservationKind.PlayerJoined ||
                               (isReplay && x.StartUtc == eventUtc)))))
                .OrderByDescending(x => x.SessionId)
                .Take(2)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (matchingSessions.Count == 1)
            {
                correlated = matchingSessions[0];
            }
            if (matchingSessions.Count == 0)
            {
                var nextSessions = await context.PlayerSessions
                    .Where(x => x.PlayerId == playerId && x.ServerId == observation.ServerId &&
                                x.StartUtc > eventUtc)
                    .OrderBy(x => x.StartUtc)
                    .ThenBy(x => x.SessionId)
                    .Take(2)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (nextSessions.Count > 0 &&
                    (nextSessions.Count == 1 || nextSessions[0].StartUtc < nextSessions[1].StartUtc))
                {
                    var next = nextSessions[0];
                    var boundary = await HasAcceptedSessionBoundaryBetween(
                        playerId, observation, eventUtc, next.StartUtc, next.ConnectionCorrelationId, cancellationToken)
                        .ConfigureAwait(false);
                    if (!boundary && next.StartUtc - eventUtc <= ReconnectGapThreshold)
                    {
                        correlated = next;
                    }
                    else if (allowCreate)
                    {
                        await CreateHistoricalSession(playerId, observation, eventUtc, next.StartUtc, cancellationToken)
                            .ConfigureAwait(false);
                        return;
                    }
                }
            }
        }
        if (correlated is not null &&
            correlated.StartUtc > eventUtc &&
            await HasAcceptedSessionBoundaryBetween(
                playerId,
                observation,
                eventUtc,
                correlated.StartUtc,
                correlated.ConnectionCorrelationId,
                cancellationToken).ConfigureAwait(false))
        {
            correlated = null;
        }
        if (correlated?.EndUtc is not null && eventUtc <= correlated.EndUtc.Value)
        {
            if (eventUtc < correlated.StartUtc)
            {
                correlated.StartUtc = eventUtc;
                correlated.StartQuality = (byte)observation.TimestampQuality;
            }
            if (observation.IsBot is not null && correlated.IsBot is null)
            {
                correlated.IsBot = observation.IsBot;
            }
            correlated.LastConfirmedUtc = correlated.LastConfirmedUtc > eventUtc
                ? correlated.LastConfirmedUtc
                : eventUtc;
            await ApplyRetainedLeave(playerId, observation, correlated, cancellationToken).ConfigureAwait(false);
            await RebuildSegments(correlated, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!allowCreate)
        {
            if (correlated is null)
            {
                var staleOpenSessions = context.PlayerSessions
                    .Where(x => x.PlayerId == playerId &&
                                x.ServerId == observation.ServerId &&
                                x.EndUtc == null &&
                                x.StartUtc <= eventUtc);
                if (observation.ConnectionCorrelationId is Guid staleCorrelationId)
                {
                    staleOpenSessions = staleOpenSessions.Where(x =>
                        x.ConnectionCorrelationId == staleCorrelationId ||
                        (x.ConnectionCorrelationId == null &&
                         !string.IsNullOrWhiteSpace(observation.GamePlayerId) &&
                         x.GamePlayerId == observation.GamePlayerId));
                }
                else if (!string.IsNullOrWhiteSpace(observation.GamePlayerId))
                {
                    staleOpenSessions = staleOpenSessions.Where(x =>
                        x.GamePlayerId == observation.GamePlayerId);
                }
                else
                {
                    staleOpenSessions = staleOpenSessions.Where(_ => false);
                }

                correlated = await staleOpenSessions
                    .OrderByDescending(x => x.StartUtc)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            if (correlated is not null)
            {
                if (eventUtc < correlated.StartUtc)
                {
                    correlated.StartUtc = eventUtc;
                    correlated.StartQuality = (byte)observation.TimestampQuality;
                }
                if (observation.IsBot is not null && correlated.IsBot is null)
                {
                    correlated.IsBot = observation.IsBot;
                }
                correlated.LastConfirmedUtc = correlated.LastConfirmedUtc > eventUtc
                    ? correlated.LastConfirmedUtc
                    : eventUtc;
                correlated.ConnectionCorrelationId ??= observation.ConnectionCorrelationId;
                correlated.GamePlayerId ??= observation.GamePlayerId;
                await RebuildSegments(correlated, cancellationToken).ConfigureAwait(false);
            }
            return;
        }

        var open = await context.PlayerSessions
            .FirstOrDefaultAsync(
                x => x.PlayerId == playerId && x.ServerId == observation.ServerId && x.EndUtc == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (open is not null &&
            open.StartUtc > eventUtc &&
            await HasAcceptedSessionBoundaryBetween(
                playerId,
                observation,
                eventUtc,
                open.StartUtc,
                open.ConnectionCorrelationId,
                cancellationToken).ConfigureAwait(false))
        {
            await CreateHistoricalSession(playerId, observation, eventUtc, open.StartUtc, cancellationToken)
                .ConfigureAwait(false);
            return;
        }
        if (open is null)
        {
            var inferredQuery = context.PlayerSessions
                .Where(x => x.PlayerId == playerId &&
                            x.ServerId == observation.ServerId &&
                            x.EndUtc != null &&
                            x.ClosureReason == 3 &&
                            !x.IsFinal &&
                            x.EndUtc < eventUtc &&
                            x.EndUtc >= eventUtc.Subtract(ReconnectGapThreshold));
            if (observation.ConnectionCorrelationId is Guid inferredCorrelationId)
            {
                inferredQuery = inferredQuery.Where(x => x.ConnectionCorrelationId == inferredCorrelationId);
            }
            else if (!string.IsNullOrWhiteSpace(observation.GamePlayerId))
            {
                inferredQuery = inferredQuery.Where(x =>
                    x.ConnectionCorrelationId == null && x.GamePlayerId == observation.GamePlayerId);
            }
            else
            {
                inferredQuery = inferredQuery.Where(_ => false);
            }

            var inferred = await inferredQuery
                .OrderByDescending(x => x.EndUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (inferred is not null)
            {
                var boundary = await FindAcceptedSessionBoundaryBetween(
                    playerId,
                    observation,
                    inferred.EndUtc!.Value,
                    eventUtc,
                    inferred.ConnectionCorrelationId,
                    cancellationToken,
                    includeStartBoundary: inferred.EndUtc > inferred.StartUtc).ConfigureAwait(false);
                if (boundary is null)
                {
                    inferred.EndUtc = null;
                    inferred.EndQuality = null;
                    inferred.ClosureReason = null;
                    inferred.IsFinal = false;
                    inferred.LastConfirmedUtc = eventUtc;
                    inferred.GamePlayerId ??= observation.GamePlayerId;
                    inferred.IsBot ??= observation.IsBot;
                    await ApplyRetainedLeave(playerId, observation, inferred, cancellationToken).ConfigureAwait(false);
                    await RebuildSegments(inferred, cancellationToken).ConfigureAwait(false);
                    return;
                }

                CloseSessionAtBoundary(inferred, boundary);
                await RebuildSegments(inferred, cancellationToken).ConfigureAwait(false);
            }
        }

        if (open is not null && eventUtc > open.LastConfirmedUtc)
        {
            var boundary = await FindAcceptedSessionBoundaryBetween(
                playerId,
                observation,
                open.LastConfirmedUtc,
                eventUtc,
                open.ConnectionCorrelationId,
                cancellationToken,
                includeStartBoundary: open.LastConfirmedUtc > open.StartUtc).ConfigureAwait(false);
            if (boundary is not null)
            {
                CloseSessionAtBoundary(open, boundary);
                await RebuildSegments(open, cancellationToken).ConfigureAwait(false);
                open = null;
            }
        }

        if (open is not null)
        {
            var changedKnownConnection = observation.ConnectionCorrelationId is not null &&
                                         open.ConnectionCorrelationId is not null &&
                                         observation.ConnectionCorrelationId != open.ConnectionCorrelationId;
            if (changedKnownConnection)
            {
                if (eventUtc < open.StartUtc)
                {
                    await CreateHistoricalSession(playerId, observation, eventUtc, open.StartUtc, cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                open.EndUtc = open.LastConfirmedUtc;
                open.EndQuality = (byte)PlaytimeTimestampQuality.InferredTimestamp;
                open.ClosureReason = 3;
                open.IsFinal = false;
                await RebuildSegments(open, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var sustainedGap = eventUtc - open.LastConfirmedUtc > ReconnectGapThreshold;
                if (sustainedGap)
                {
                    open.EndUtc = open.LastConfirmedUtc;
                    open.EndQuality = (byte)PlaytimeTimestampQuality.InferredTimestamp;
                    open.ClosureReason = 3;
                    open.IsFinal = false;
                    await RebuildSegments(open, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (observation.IsBot is not null && open.IsBot is null)
                    {
                        open.IsBot = observation.IsBot;
                    }
                    if (eventUtc < open.StartUtc)
                    {
                        open.StartUtc = eventUtc;
                        open.StartQuality = (byte)observation.TimestampQuality;
                    }
                    open.LastConfirmedUtc = open.LastConfirmedUtc > eventUtc ? open.LastConfirmedUtc : eventUtc;
                    open.ConnectionCorrelationId ??= observation.ConnectionCorrelationId;
                    open.GamePlayerId ??= observation.GamePlayerId;
                    await ApplyRetainedLeave(playerId, observation, open, cancellationToken).ConfigureAwait(false);
                    await RebuildSegments(open, cancellationToken).ConfigureAwait(false);
                    return;
                }
            }
        }

        var session = new PlayerSession
        {
            SessionId = Guid.NewGuid(),
            PlayerId = playerId,
            ServerId = observation.ServerId,
            ConnectionCorrelationId = observation.ConnectionCorrelationId,
            GameType = (int)observation.GameType,
            GamePlayerId = observation.GamePlayerId,
            IsBot = observation.IsBot,
            StartUtc = eventUtc,
            LastConfirmedUtc = eventUtc,
            StartQuality = (byte)observation.TimestampQuality,
            IsFinal = false
        };
        context.PlayerSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await ApplyRetainedLeave(playerId, observation, session, cancellationToken).ConfigureAwait(false);
        await RebuildSegments(session, cancellationToken).ConfigureAwait(false);
    }

    private async Task CreateHistoricalSession(
        Guid playerId,
        PlaytimeObservationDto observation,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var session = new PlayerSession
        {
            SessionId = Guid.NewGuid(),
            PlayerId = playerId,
            ServerId = observation.ServerId,
            ConnectionCorrelationId = observation.ConnectionCorrelationId,
            GameType = (int)observation.GameType,
            GamePlayerId = observation.GamePlayerId,
            IsBot = observation.IsBot,
            StartUtc = startUtc,
            LastConfirmedUtc = startUtc,
            EndUtc = endUtc,
            StartQuality = (byte)observation.TimestampQuality,
            EndQuality = (byte)PlaytimeTimestampQuality.InferredTimestamp,
            ClosureReason = 3,
            IsFinal = false
        };
        context.PlayerSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await ApplyRetainedLeave(playerId, observation, session, cancellationToken).ConfigureAwait(false);
        await RebuildSegments(session, cancellationToken).ConfigureAwait(false);
    }

    private async Task ApplyRetainedLeave(
        Guid playerId,
        PlaytimeObservationDto join,
        PlayerSession session,
        CancellationToken cancellationToken)
    {
        if (session.IsFinal)
        {
            return;
        }

        var gamePlayerId = !string.IsNullOrWhiteSpace(join.GamePlayerId)
            ? join.GamePlayerId
            : session.GamePlayerId;
        var hasGamePlayerId = !string.IsNullOrWhiteSpace(gamePlayerId);
        var nextSessionStartUtc = await context.PlayerSessions
            .Where(x => x.PlayerId == playerId &&
                        x.ServerId == session.ServerId &&
                        x.SessionId != session.SessionId &&
                        x.StartUtc > session.StartUtc)
            .OrderBy(x => x.StartUtc)
            .Select(x => (DateTime?)x.StartUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var leaves = context.PlaytimeObservations
            .AsNoTracking()
            .Where(x => x.ServerId == session.ServerId &&
                        x.Kind == (byte)PlaytimeObservationKind.PlayerLeft &&
                        x.Outcome == (byte)PlaytimeObservationOutcome.Accepted &&
                        x.EventUtc > session.StartUtc &&
                        x.EventUtc <= session.StartUtc.Add(LateEventThreshold) &&
                        (!nextSessionStartUtc.HasValue || x.EventUtc < nextSessionStartUtc.Value) &&
                        (x.ConnectionCorrelationId == null ||
                         join.ConnectionCorrelationId == null ||
                         x.ConnectionCorrelationId == join.ConnectionCorrelationId) &&
                        (x.ConnectionCorrelationId == null ||
                         session.ConnectionCorrelationId == null ||
                         x.ConnectionCorrelationId == session.ConnectionCorrelationId) &&
                        (join.ConnectionCorrelationId == null ||
                         session.ConnectionCorrelationId == null ||
                         join.ConnectionCorrelationId == session.ConnectionCorrelationId) &&
                        (x.CanonicalPlayerId == playerId ||
                         (hasGamePlayerId &&
                          x.CanonicalPlayerId == null &&
                          x.GamePlayerId == gamePlayerId)));
        var leave = await leaves
            .OrderBy(x => x.EventUtc)
            .ThenBy(x => x.ObservationId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (leave is not null)
        {
            var endUtc = leave.EventUtc > session.LastConfirmedUtc
                ? leave.EventUtc
                : session.LastConfirmedUtc;
            session.EndUtc = endUtc;
            session.EndQuality = leave.TimestampQuality;
            session.ClosureReason = 1;
            session.IsFinal = true;
            session.IsBot ??= leave.IsBot;
            return;
        }

        var rosterAbsence = await FindAcceptedCompleteRosterAbsence(
            playerId,
            join,
            session.StartUtc,
            session.StartUtc.Add(LateEventThreshold),
            cancellationToken,
            nextSessionStartUtc).ConfigureAwait(false);
        if (rosterAbsence is null ||
            session.LastConfirmedUtc > rosterAbsence.EventUtc ||
            (session.EndUtc is DateTime currentEndUtc && currentEndUtc < rosterAbsence.EventUtc))
        {
            return;
        }

        session.EndUtc = rosterAbsence.EventUtc;
        session.EndQuality = (byte)PlaytimeTimestampQuality.RosterSnapshotTimestamp;
        session.ClosureReason = 2;
        session.IsFinal = false;
    }

    private async Task<bool> HasAcceptedSessionBoundaryBetween(
        Guid playerId,
        PlaytimeObservationDto join,
        DateTime startUtc,
        DateTime endUtc,
        Guid? sessionCorrelationId,
        CancellationToken cancellationToken)
    {
        if (endUtc <= startUtc)
        {
            return false;
        }

        return await FindAcceptedSessionBoundaryBetween(
            playerId,
            join,
            startUtc,
            endUtc,
            sessionCorrelationId,
            cancellationToken).ConfigureAwait(false) is not null;
    }

    private async Task<PlaytimeObservation?> FindAcceptedSessionBoundaryBetween(
        Guid playerId,
        PlaytimeObservationDto join,
        DateTime startUtc,
        DateTime endUtc,
        Guid? sessionCorrelationId,
        CancellationToken cancellationToken,
        bool includeStartBoundary = false)
    {
        var leave = await FindAcceptedLeaveBetween(
            playerId,
            join,
            startUtc,
            endUtc,
            sessionCorrelationId,
            cancellationToken,
            includeStartBoundary).ConfigureAwait(false);
        var rosterAbsence = await FindAcceptedCompleteRosterAbsence(
            playerId,
            join,
            startUtc,
            endUtc,
            cancellationToken,
            includeStart: includeStartBoundary).ConfigureAwait(false);
        if (leave is null)
        {
            return rosterAbsence;
        }

        return rosterAbsence is null || leave.EventUtc <= rosterAbsence.EventUtc
            ? leave
            : rosterAbsence;
    }

    private async Task<PlaytimeObservation?> FindAcceptedLeaveBetween(
        Guid playerId,
        PlaytimeObservationDto join,
        DateTime startUtc,
        DateTime endUtc,
        Guid? sessionCorrelationId,
        CancellationToken cancellationToken,
        bool includeStartLeave = false)
    {
        var hasGamePlayerId = !string.IsNullOrWhiteSpace(join.GamePlayerId);
        return await context.PlaytimeObservations
            .AsNoTracking()
            .Where(
                x => x.ServerId == join.ServerId &&
                     x.Kind == (byte)PlaytimeObservationKind.PlayerLeft &&
                     x.Outcome == (byte)PlaytimeObservationOutcome.Accepted &&
                     (x.EventUtc > startUtc || (includeStartLeave && x.EventUtc == startUtc)) &&
                     x.EventUtc <= endUtc &&
                     (x.ConnectionCorrelationId == null ||
                      join.ConnectionCorrelationId == null ||
                      x.ConnectionCorrelationId == join.ConnectionCorrelationId) &&
                     (x.ConnectionCorrelationId == null ||
                      sessionCorrelationId == null ||
                      x.ConnectionCorrelationId == sessionCorrelationId) &&
                     (join.ConnectionCorrelationId == null ||
                      sessionCorrelationId == null ||
                      join.ConnectionCorrelationId == sessionCorrelationId) &&
                     (x.CanonicalPlayerId == playerId ||
                      (hasGamePlayerId &&
                       x.CanonicalPlayerId == null &&
                       x.GamePlayerId == join.GamePlayerId)))
            .OrderBy(x => x.EventUtc)
            .ThenBy(x => x.ObservationId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<PlaytimeObservation?> FindAcceptedCompleteRosterAbsence(
        Guid playerId,
        PlaytimeObservationDto join,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken,
        DateTime? exclusiveEndUtc = null,
        bool includeStart = false)
    {
        if (endUtc <= startUtc)
        {
            return null;
        }

        var processingUtc = timeProvider.GetUtcNow().UtcDateTime;
        var boundedEndUtc = endUtc < startUtc.Add(LateEventThreshold)
            ? endUtc
            : startUtc.Add(LateEventThreshold);
        var snapshots = await context.PlaytimeObservations
            .AsNoTracking()
            .Where(x => x.ServerId == join.ServerId &&
                        x.Kind == (byte)PlaytimeObservationKind.ServerRoster &&
                        x.Outcome == (byte)PlaytimeObservationOutcome.Accepted &&
                        x.RosterCompleteness == (byte)PlaytimeRosterCompleteness.Complete &&
                        x.TimestampQuality == (byte)PlaytimeTimestampQuality.RosterSnapshotTimestamp &&
                        !x.IdentifiersRedacted &&
                        x.ResolvedRosterJson != null &&
                        x.EventUtc <= processingUtc &&
                        (x.EventUtc > startUtc || (includeStart && x.EventUtc == startUtc)) &&
                        x.EventUtc <= boundedEndUtc &&
                        (!exclusiveEndUtc.HasValue || x.EventUtc < exclusiveEndUtc.Value))
            .OrderBy(x => x.EventUtc)
            .ThenBy(x => x.ObservationId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var snapshot in snapshots)
        {
            var supersededAtReceipt = await context.PlaytimeObservations
                .AsNoTracking()
                .AnyAsync(x => x.ObservationId != snapshot.ObservationId &&
                               x.ServerId == snapshot.ServerId &&
                               x.Kind == (byte)PlaytimeObservationKind.ServerRoster &&
                               x.Outcome == (byte)PlaytimeObservationOutcome.Accepted &&
                               x.ReceivedUtc <= snapshot.ReceivedUtc &&
                               (x.EventUtc > snapshot.EventUtc ||
                                (x.EventUtc == snapshot.EventUtc &&
                                 (x.ProducerEpoch != snapshot.ProducerEpoch ||
                                  x.ProducerSequence == null ||
                                  snapshot.ProducerSequence == null ||
                                  x.ProducerSequence > snapshot.ProducerSequence))),
                    cancellationToken)
                .ConfigureAwait(false);
            if (supersededAtReceipt)
            {
                continue;
            }

            var processingAge = processingUtc - snapshot.EventUtc;
            if (processingAge < TimeSpan.Zero || processingAge > CompleteRosterFreshnessThreshold)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(snapshot.ResolvedRosterJson))
            {
                continue;
            }

            var roster = DeserializeRosterResolutions(snapshot.ResolvedRosterJson);
            if (roster.Any(x => x.CanonicalPlayerId is null && x.IsBot != true) ||
                roster.Any(x => x.CanonicalPlayerId == playerId))
            {
                continue;
            }

            return snapshot;
        }

        return null;
    }

    private static void CloseSessionAtBoundary(PlayerSession session, PlaytimeObservation boundary)
    {
        session.EndUtc = boundary.EventUtc;
        session.EndQuality = boundary.TimestampQuality;
        session.ClosureReason = boundary.Kind == (byte)PlaytimeObservationKind.PlayerLeft ? (byte)1 : (byte)2;
        session.IsFinal = boundary.Kind == (byte)PlaytimeObservationKind.PlayerLeft;
    }

    private async Task CloseSession(
        Guid playerId,
        Guid serverId,
        DateTime eventUtc,
        byte endQuality,
        byte closureReason,
        Guid? connectionCorrelationId,
        bool? isBot,
        CancellationToken cancellationToken)
    {
        var session = connectionCorrelationId is Guid correlationId
            ? await context.PlayerSessions
                .Where(x => x.ConnectionCorrelationId == correlationId &&
                                          x.PlayerId == playerId &&
                                          x.ServerId == serverId &&
                                          x.StartUtc <= eventUtc)
                .OrderByDescending(x => x.StartUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false)
            : null;
        if (session is null)
        {
            var openSessions = context.PlayerSessions
                .Where(x => x.PlayerId == playerId &&
                            x.ServerId == serverId &&
                            x.EndUtc == null &&
                            x.StartUtc <= eventUtc);
            if (connectionCorrelationId is not null)
            {
                openSessions = openSessions.Where(x =>
                    x.ConnectionCorrelationId == null ||
                    x.ConnectionCorrelationId == connectionCorrelationId);
            }
            session = await openSessions
                .OrderByDescending(x => x.StartUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        if (session is null)
        {
            var correctionStartUtc = eventUtc.Subtract(LateEventThreshold);
            var latestSession = await context.PlayerSessions
                .Where(x => x.PlayerId == playerId &&
                            x.ServerId == serverId &&
                            x.StartUtc <= eventUtc)
                .OrderByDescending(x => x.StartUtc)
                .ThenByDescending(x => x.SessionId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (latestSession?.EndUtc is not null &&
                latestSession.LastConfirmedUtc >= correctionStartUtc &&
                !latestSession.IsFinal &&
                (connectionCorrelationId == null ||
                 latestSession.ConnectionCorrelationId == null ||
                 latestSession.ConnectionCorrelationId == connectionCorrelationId) &&
                latestSession.ClosureReason is 2 or 3)
            {
                session = latestSession;
            }
        }
        if (session is null)
        {
            return;
        }

        if (eventUtc < session.StartUtc)
        {
            return;
        }

        if (isBot is not null && session.IsBot is null)
        {
            session.IsBot = isBot;
        }
        if (session.IsFinal)
        {
            return;
        }

        if (session.GameType is (int)GameType.CallOfDuty2 or (int)GameType.CallOfDuty4 or (int)GameType.CallOfDuty5 &&
            endQuality == (byte)PlaytimeTimestampQuality.ParseTime &&
            connectionCorrelationId is null &&
            session.ConnectionCorrelationId is null &&
            session.EndUtc is null &&
            session.LastConfirmedUtc > eventUtc &&
            await SplitReadTimeReconnect(session, eventUtc, isBot, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var endUtc = session.LastConfirmedUtc > eventUtc ? session.LastConfirmedUtc : eventUtc;
        var nextSessionStart = await context.PlayerSessions
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId && x.ServerId == serverId &&
                        x.SessionId != session.SessionId && x.StartUtc > session.StartUtc)
            .OrderBy(x => x.StartUtc)
            .Select(x => (DateTime?)x.StartUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (nextSessionStart is DateTime nextStart && endUtc > nextStart)
        {
            session.EndUtc = nextStart;
            session.EndQuality = (byte)PlaytimeTimestampQuality.InferredTimestamp;
            session.ClosureReason = 3;
            session.IsFinal = false;
            await RebuildSegments(session, cancellationToken).ConfigureAwait(false);
            return;
        }
        session.EndUtc = endUtc;
        session.EndQuality = endQuality;
        session.ClosureReason = closureReason;
        session.IsFinal = closureReason != 3;
        await RebuildSegments(session, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> SplitReadTimeReconnect(
        PlayerSession session,
        DateTime leaveUtc,
        bool? isBot,
        CancellationToken cancellationToken)
    {
        var split = await TrySplitReadTimeReconnect(session, leaveUtc, isBot, cancellationToken).ConfigureAwait(false);
        if (split is null)
        {
            return false;
        }

        while (true)
        {
            var (current, join) = split.Value;
            var leave = await FindAcceptedLeaveBetween(
                current.PlayerId, join, current.StartUtc, current.LastConfirmedUtc, null, cancellationToken)
                .ConfigureAwait(false);
            if (leave is not null &&
                leave.TimestampQuality == (byte)PlaytimeTimestampQuality.ParseTime &&
                leave.ConnectionCorrelationId is null &&
                leave.EventUtc < current.LastConfirmedUtc)
            {
                var next = await TrySplitReadTimeReconnect(current, leave.EventUtc, leave.IsBot, cancellationToken)
                    .ConfigureAwait(false);
                if (next is not null)
                {
                    split = next;
                    continue;
                }
            }

            await ApplyRetainedLeave(current.PlayerId, join, current, cancellationToken).ConfigureAwait(false);
            await RebuildSegments(current, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }

    private async Task<(PlayerSession Session, PlaytimeObservationDto Join)?> TrySplitReadTimeReconnect(
        PlayerSession session,
        DateTime leaveUtc,
        bool? isBot,
        CancellationToken cancellationToken)
    {
        var reconnect = await context.PlaytimeObservations.AsNoTracking()
            .Where(x => x.ServerId == session.ServerId &&
                        x.CanonicalPlayerId == session.PlayerId &&
                        x.Kind == (byte)PlaytimeObservationKind.PlayerJoined &&
                        x.TimestampQuality == (byte)PlaytimeTimestampQuality.ParseTime &&
                        x.ConnectionCorrelationId == null &&
                        x.Outcome == (byte)PlaytimeObservationOutcome.Accepted &&
                        x.EventUtc > leaveUtc && x.EventUtc <= session.LastConfirmedUtc)
            .OrderBy(x => x.EventUtc)
            .ThenBy(x => x.ObservationId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (reconnect is null)
        {
            return null;
        }

        var nextSessionExists = await context.PlayerSessions.AsNoTracking()
            .AnyAsync(x => x.SessionId != session.SessionId &&
                           x.ServerId == session.ServerId &&
                           x.PlayerId == session.PlayerId &&
                           x.StartUtc > session.StartUtc && x.StartUtc <= session.LastConfirmedUtc,
                cancellationToken).ConfigureAwait(false);
        if (nextSessionExists)
        {
            return null;
        }

        var previousJoin = await context.PlaytimeObservations.AsNoTracking()
            .Where(x => x.ServerId == session.ServerId &&
                        x.CanonicalPlayerId == session.PlayerId &&
                        x.Kind == (byte)PlaytimeObservationKind.PlayerJoined &&
                        x.ConnectionCorrelationId == null &&
                        x.Outcome == (byte)PlaytimeObservationOutcome.Accepted &&
                        x.EventUtc >= session.StartUtc && x.EventUtc <= leaveUtc)
            .MaxAsync(x => (DateTime?)x.EventUtc, cancellationToken).ConfigureAwait(false);
        var priorConfirmation = previousJoin ?? session.StartUtc;
        var previousRosters = context.PlaytimeObservations.AsNoTracking()
            .Where(x => x.ServerId == session.ServerId &&
                        x.Kind == (byte)PlaytimeObservationKind.ServerRoster &&
                        x.Outcome == (byte)PlaytimeObservationOutcome.Accepted &&
                        x.RosterCompleteness != (byte)PlaytimeRosterCompleteness.Failed &&
                        x.RosterCompleteness != (byte)PlaytimeRosterCompleteness.NotApplicable &&
                        x.ResolvedRosterJson != null &&
                        x.EventUtc > priorConfirmation && x.EventUtc <= leaveUtc)
            .OrderByDescending(x => x.EventUtc)
            .Select(x => new { x.EventUtc, x.ResolvedRosterJson })
            .AsAsyncEnumerable();
        await foreach (var roster in previousRosters.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (DeserializeRosterResolutions(roster.ResolvedRosterJson)
                .Any(player => player.CanonicalPlayerId == session.PlayerId))
            {
                priorConfirmation = roster.EventUtc;
                break;
            }
        }
        var missingRosterEvidence = await context.PlaytimeObservations.AsNoTracking()
            .AnyAsync(x => x.ServerId == session.ServerId &&
                           x.Kind == (byte)PlaytimeObservationKind.ServerRoster &&
                           x.Outcome == (byte)PlaytimeObservationOutcome.Accepted &&
                           x.RosterCompleteness != (byte)PlaytimeRosterCompleteness.Failed &&
                           x.RosterCompleteness != (byte)PlaytimeRosterCompleteness.NotApplicable &&
                           x.ResolvedRosterJson == null &&
                           x.EventUtc > priorConfirmation && x.EventUtc <= leaveUtc,
                cancellationToken).ConfigureAwait(false);
        if (missingRosterEvidence)
        {
            logger?.LogWarning(
                "Cannot split read-time session {SessionId} at {LeaveUtc} because preceding roster evidence has expired.",
                session.SessionId, leaveUtc);
            return null;
        }

        var reconnectStart = reconnect.EventUtc;
        var reconnectQuality = reconnect.TimestampQuality;
        var laterRosters = context.PlaytimeObservations.AsNoTracking()
            .Where(x => x.ServerId == session.ServerId &&
                        x.Kind == (byte)PlaytimeObservationKind.ServerRoster &&
                        x.Outcome == (byte)PlaytimeObservationOutcome.Accepted &&
                        x.RosterCompleteness != (byte)PlaytimeRosterCompleteness.Failed &&
                        x.RosterCompleteness != (byte)PlaytimeRosterCompleteness.NotApplicable &&
                        x.EventUtc > leaveUtc && x.EventUtc < reconnect.EventUtc)
            .OrderBy(x => x.EventUtc)
            .Select(x => new { x.EventUtc, x.TimestampQuality, x.ResolvedRosterJson })
            .AsAsyncEnumerable();
        await foreach (var roster in laterRosters.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (roster.ResolvedRosterJson is null)
            {
                logger?.LogWarning(
                    "Cannot split read-time session {SessionId} at {LeaveUtc} because subsequent roster evidence has expired.",
                    session.SessionId, leaveUtc);
                return null;
            }
            if (DeserializeRosterResolutions(roster.ResolvedRosterJson)
                .Any(player => player.CanonicalPlayerId == session.PlayerId))
            {
                reconnectStart = roster.EventUtc;
                reconnectQuality = roster.TimestampQuality;
                break;
            }
        }

        var lastConfirmed = session.LastConfirmedUtc;
        session.LastConfirmedUtc = priorConfirmation;
        session.EndUtc = leaveUtc;
        session.EndQuality = (byte)PlaytimeTimestampQuality.ParseTime;
        session.ClosureReason = 1;
        session.IsFinal = true;
        session.IsBot ??= isBot;
        await RebuildSegments(session, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var next = new PlayerSession
        {
            SessionId = Guid.NewGuid(),
            PlayerId = session.PlayerId,
            ServerId = session.ServerId,
            GameType = session.GameType,
            GamePlayerId = reconnect.GamePlayerId ?? session.GamePlayerId,
            IsBot = reconnect.IsBot ?? session.IsBot,
            StartUtc = reconnectStart,
            LastConfirmedUtc = lastConfirmed,
            StartQuality = reconnectQuality,
            IsFinal = false
        };
        context.PlayerSessions.Add(next);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var join = new PlaytimeObservationDto
        {
            ObservationId = reconnect.ObservationId,
            ServerId = reconnect.ServerId,
            GameType = (GameType)reconnect.GameType,
            Kind = PlaytimeObservationKind.PlayerJoined,
            EventUtc = reconnect.EventUtc,
            TimestampQuality = (PlaytimeTimestampQuality)reconnect.TimestampQuality,
            GamePlayerId = next.GamePlayerId
        };
        return (next, join);
    }

    private async Task<bool> IsStaleRosterObservation(
        PlaytimeObservationDto observation,
        DateTime eventUtc,
        CancellationToken cancellationToken)
    {
        var latest = await context.PlaytimeObservations
            .AsNoTracking()
            .Where(x => x.ServerId == observation.ServerId &&
                        x.Kind == (byte)PlaytimeObservationKind.ServerRoster &&
                        x.Outcome == (byte)PlaytimeObservationOutcome.Accepted)
            .OrderByDescending(x => x.EventUtc)
            .ThenByDescending(x => x.ProducerSequence)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (latest is null)
        {
            return false;
        }

        if (latest.EventUtc > eventUtc)
        {
            return true;
        }

        if (latest.EventUtc == eventUtc &&
            latest.ProducerEpoch == observation.ProducerEpoch &&
            latest.ProducerSequence is not null &&
            observation.ProducerSequence is not null &&
            latest.ProducerSequence > observation.ProducerSequence)
        {
            return true;
        }

        return false;
    }

    private async Task ReconcileRoster(
        PlaytimeObservationDto observation,
        DateTime eventUtc,
        DateTime processingUtc,
        CancellationToken cancellationToken)
    {
        if (observation.RosterCompleteness is PlaytimeRosterCompleteness.Failed or PlaytimeRosterCompleteness.NotApplicable)
        {
            return;
        }

        var stale = await IsStaleRosterObservation(observation, eventUtc, cancellationToken).ConfigureAwait(false);
        var processingAge = processingUtc - eventUtc;
        var hasTrustedCaptureTime = observation.TimestampQuality == PlaytimeTimestampQuality.RosterSnapshotTimestamp;
        var freshAtProcessing = hasTrustedCaptureTime &&
                                processingAge >= TimeSpan.Zero &&
                                processingAge <= CompleteRosterFreshnessThreshold;

        var ids = observation.Roster?
            .Where(x => x.CanonicalPlayerId is not null)
            .Select(x => x.CanonicalPlayerId!.Value)
            .ToHashSet() ?? [];

        foreach (var player in observation.Roster ?? [])
        {
            if (player.CanonicalPlayerId is not null)
            {
                await EnsureSession(player.CanonicalPlayerId.Value,
                    observation with
                    {
                        CanonicalPlayerId = player.CanonicalPlayerId,
                        GamePlayerId = player.GamePlayerId,
                        SlotId = player.SlotId,
                        IsBot = player.IsBot
                    },
                    eventUtc,
                    allowCreate: !stale,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        var identityIncomplete = observation.Roster?.Any(x => x.CanonicalPlayerId is null && x.IsBot != true) == true;
        if (stale ||
            !freshAtProcessing ||
            observation.RosterCompleteness != PlaytimeRosterCompleteness.Complete ||
            identityIncomplete)
        {
            if (observation.RosterCompleteness == PlaytimeRosterCompleteness.Complete &&
                (stale || !freshAtProcessing || identityIncomplete))
            {
                logger?.LogWarning(
                    "Complete roster {ObservationId} from server {ServerId} was applied positive-only: superseded {Superseded}, identity incomplete {IdentityIncomplete}, timestamp quality {TimestampQuality}, processing age {ProcessingAgeSeconds} seconds; absence reconciliation requires a roster snapshot timestamp and age between 0 and {MaximumAgeSeconds} seconds.",
                    observation.ObservationId,
                    observation.ServerId,
                    stale,
                    identityIncomplete,
                    observation.TimestampQuality,
                    processingAge.TotalSeconds,
                    CompleteRosterFreshnessThreshold.TotalSeconds);
            }

            return;
        }

        var open = await context.PlayerSessions
            .Where(x => x.ServerId == observation.ServerId && x.EndUtc == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var session in open)
        {
            if (ids.Contains(session.PlayerId))
            {
                continue;
            }
            if (session.LastConfirmedUtc > eventUtc)
            {
                continue;
            }

            session.EndUtc = eventUtc;
            session.EndQuality = (byte)PlaytimeTimestampQuality.RosterSnapshotTimestamp;
            session.ClosureReason = 2;
            session.IsFinal = false;
            await RebuildSegments(session, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ChangeMapRun(
        PlaytimeObservationDto observation,
        DateTime eventUtc,
        CancellationToken cancellationToken,
        bool replayLaterObservations = true)
    {
        if (string.IsNullOrWhiteSpace(observation.MapName))
        {
            return;
        }

        if (observation.ProducerEpoch is Guid producerEpoch &&
            observation.ProducerSequence is long producerSequence &&
            await context.PlaytimeObservations
                .AsNoTracking()
                .AnyAsync(x => x.ServerId == observation.ServerId &&
                               x.EventUtc == eventUtc &&
                               x.ProducerEpoch == producerEpoch &&
                               x.ProducerSequence > producerSequence &&
                               x.Outcome == (byte)PlaytimeObservationOutcome.Accepted &&
                               (x.Kind == (byte)PlaytimeObservationKind.MapChanged ||
                                (x.Kind == (byte)PlaytimeObservationKind.ServerRoster &&
                                 x.MapName != null &&
                                 x.MapName.Trim().Length > 0)),
                    cancellationToken)
                .ConfigureAwait(false))
        {
            return;
        }

        var normalized = NormalizeMapKey(observation.MapName);
        var activeRun = await context.ServerMapRuns
            .Where(x => x.ServerId == observation.ServerId &&
                        x.StartUtc <= eventUtc &&
                        (x.EndUtc == null || x.EndUtc > eventUtc))
            .OrderByDescending(x => x.StartUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (activeRun is not null &&
            string.Equals(activeRun.NormalizedMapKey, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var mapGameType = (int)observation.GameType.GetMapGameType();
        var map = await context.Maps
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GameType == mapGameType &&
                                      (x.MapName == observation.MapName || x.DisplayName == observation.MapName),
                cancellationToken)
            .ConfigureAwait(false);

        DateTime? rebuildEnd;
        if (activeRun is not null && activeRun.StartUtc == eventUtc)
        {
            activeRun.GameType = (int)observation.GameType;
            activeRun.MapId = map?.MapId;
            activeRun.NormalizedMapKey = normalized;
            activeRun.AttributionQuality = map is null
                ? (byte)PlaytimeTimestampQuality.Unknown
                : (byte)observation.TimestampQuality;
            rebuildEnd = activeRun.EndUtc;
        }
        else
        {
            if (activeRun is not null)
            {
                activeRun.EndUtc = eventUtc;
                activeRun.IsFinal = true;
            }

            var nextRun = await context.ServerMapRuns
                .Where(x => x.ServerId == observation.ServerId && x.StartUtc > eventUtc)
                .OrderBy(x => x.StartUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            var run = new ServerMapRun
            {
                RunId = Guid.NewGuid(),
                ServerId = observation.ServerId,
                GameType = (int)observation.GameType,
                MapId = map?.MapId,
                NormalizedMapKey = normalized,
                StartUtc = eventUtc,
                EndUtc = nextRun?.StartUtc,
                AttributionQuality = map is null
                    ? (byte)PlaytimeTimestampQuality.Unknown
                    : (byte)observation.TimestampQuality,
                IsFinal = nextRun is not null
            };
            context.ServerMapRuns.Add(run);
            rebuildEnd = run.EndUtc;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (replayLaterObservations)
        {
            await ReplayLaterMapObservations(
                observation.ServerId,
                eventUtc,
                rebuildEnd,
                cancellationToken).ConfigureAwait(false);
        }

        var sessionsQuery = context.PlayerSessions
            .Where(x => x.ServerId == observation.ServerId &&
                        x.StartUtc < (rebuildEnd ?? DateTime.MaxValue) &&
                        (x.EndUtc ?? x.LastConfirmedUtc) > eventUtc);
        var skip = 0;
        while (true)
        {
            var batch = await sessionsQuery
                .OrderBy(x => x.SessionId)
                .Select(x => x.SessionId)
                .Skip(skip)
                .Take(100)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            if (batch.Count == 0)
            {
                break;
            }

            var sessions = await context.PlayerSessions
                .Where(x => batch.Contains(x.SessionId))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var session in sessions)
            {
                await RebuildSegments(session, cancellationToken).ConfigureAwait(false);
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            context.ChangeTracker.Clear();
            skip += batch.Count;
        }
    }

    private async Task ReplayLaterMapObservations(
        Guid serverId,
        DateTime eventUtc,
        DateTime? throughUtc,
        CancellationToken cancellationToken)
    {
        var observationsQuery = context.PlaytimeObservations
            .AsNoTracking()
            .Where(x => x.ServerId == serverId &&
                        x.EventUtc > eventUtc &&
                        x.Outcome == (byte)PlaytimeObservationOutcome.Accepted &&
                        (x.Kind == (byte)PlaytimeObservationKind.MapChanged ||
                         (x.Kind == (byte)PlaytimeObservationKind.ServerRoster &&
                          x.MapName != null &&
                          x.MapName.Trim().Length > 0)));
        if (throughUtc is DateTime endUtc)
        {
            observationsQuery = observationsQuery.Where(x => x.EventUtc <= endUtc);
        }

        var laterObservations = await observationsQuery
            .OrderBy(x => x.EventUtc)
            .ThenBy(x => x.ProducerEpoch)
            .ThenBy(x => x.ProducerSequence)
            .ThenBy(x => x.ObservationId)
            .Select(x => new PlaytimeObservationDto
            {
                ObservationId = x.ObservationId,
                ServerId = x.ServerId,
                GameType = (GameType)x.GameType,
                Kind = (PlaytimeObservationKind)x.Kind,
                EventUtc = x.EventUtc,
                TimestampQuality = (PlaytimeTimestampQuality)x.TimestampQuality,
                ProducerEpoch = x.ProducerEpoch,
                ProducerSequence = x.ProducerSequence,
                MapName = x.MapName
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var laterObservation in laterObservations)
        {
            await ChangeMapRun(
                laterObservation,
                NormalizeUtc(laterObservation.EventUtc),
                cancellationToken,
                replayLaterObservations: false).ConfigureAwait(false);
        }
    }

    private async Task RebuildSegments(PlayerSession session, CancellationToken cancellationToken)
    {
        var localSegments = context.PlayerSessionMapSegments.Local
            .Where(x => x.SessionId == session.SessionId)
            .ToList();
        context.PlayerSessionMapSegments.RemoveRange(localSegments);
        var old = await context.PlayerSessionMapSegments
            .Where(x => x.SessionId == session.SessionId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        context.PlayerSessionMapSegments.RemoveRange(old);
        var end = session.EndUtc ?? session.LastConfirmedUtc;
        if (end <= session.StartUtc)
        {
            return;
        }

        var runs = await context.ServerMapRuns
            .AsNoTracking()
            .Where(x => x.ServerId == session.ServerId &&
                        x.StartUtc < end &&
                        (x.EndUtc == null || x.EndUtc > session.StartUtc))
            .OrderBy(x => x.StartUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var cursor = session.StartUtc;
        foreach (var run in runs)
        {
            var runEnd = run.EndUtc ?? end;
            var start = run.StartUtc > cursor ? run.StartUtc : cursor;
            var finish = runEnd < end ? runEnd : end;
            if (start > cursor)
            {
                context.PlayerSessionMapSegments.Add(new PlayerSessionMapSegment
                {
                    SegmentId = Guid.NewGuid(),
                    SessionId = session.SessionId,
                    StartUtc = cursor,
                    EndUtc = start,
                    UnknownMap = true
                });
                cursor = start;
            }
            if (finish > start)
            {
                context.PlayerSessionMapSegments.Add(new PlayerSessionMapSegment
                {
                    SegmentId = Guid.NewGuid(),
                    SessionId = session.SessionId,
                    MapRunId = run.RunId,
                    StartUtc = start,
                    EndUtc = finish,
                    UnknownMap = false
                });
                cursor = finish;
            }
            if (cursor >= end)
            {
                break;
            }
        }
        if (cursor < end)
        {
            context.PlayerSessionMapSegments.Add(new PlayerSessionMapSegment
            {
                SegmentId = Guid.NewGuid(),
                SessionId = session.SessionId,
                StartUtc = cursor,
                EndUtc = end,
                UnknownMap = true
            });
        }
    }

    private static PlaytimeObservationItemResultDto Rejected(Guid id, string reason) => new()
    {
        ObservationId = id,
        Outcome = PlaytimeObservationOutcome.Rejected,
        Reason = reason
    };

    private async Task<bool> HasValidCanonicalPlayers(
        PlaytimeObservationDto observation,
        CancellationToken cancellationToken)
    {
        var playerIds = (observation.Roster ?? [])
            .Where(x => x.CanonicalPlayerId is not null)
            .Select(x => x.CanonicalPlayerId!.Value)
            .Concat(observation.CanonicalPlayerId is Guid playerId ? [playerId] : [])
            .ToList();
        if (playerIds.Contains(Guid.Empty))
        {
            return false;
        }

        var distinctPlayerIds = playerIds.Distinct().ToList();
        return distinctPlayerIds.Count == 0 ||
               await context.Players.CountAsync(x => distinctPlayerIds.Contains(x.PlayerId) &&
                                                      x.GameType == (int)observation.GameType,
                   cancellationToken)
                   .ConfigureAwait(false) == distinctPlayerIds.Count;
    }

    private async Task<PlaytimeObservationDto> FilterRedactedReplay(
        PlaytimeObservation existing,
        PlaytimeObservationDto replay,
        IReadOnlyCollection<RosterResolution> previousRoster,
        CancellationToken cancellationToken)
    {
        var retainedKeys = previousRoster
            .Select(x => (x.GamePlayerId, x.SlotId))
            .ToHashSet();
        var retainedRoster = replay.Roster?
            .Where(x => retainedKeys.Contains((x.GamePlayerId, x.SlotId)))
            .ToList();
        var candidatePlayerIds = (retainedRoster ?? [])
            .Where(x => x.CanonicalPlayerId is not null)
            .Select(x => x.CanonicalPlayerId!.Value)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
        var validPlayerIds = candidatePlayerIds.Count == 0
            ? new HashSet<Guid>()
            : (await context.Players
                .AsNoTracking()
                .Where(x => candidatePlayerIds.Contains(x.PlayerId) &&
                            x.GameType == (int)replay.GameType)
                .Select(x => x.PlayerId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false))
                .ToHashSet();

        return replay with
        {
            CanonicalPlayerId = existing.CanonicalPlayerId,
            GamePlayerId = existing.GamePlayerId,
            IsBot = existing.IsBot ?? replay.IsBot,
            RosterCompleteness = replay.Kind == PlaytimeObservationKind.ServerRoster &&
                                 replay.RosterCompleteness == PlaytimeRosterCompleteness.Complete
                ? PlaytimeRosterCompleteness.Partial
                : replay.RosterCompleteness,
            Roster = retainedRoster?.Select(x => x with
            {
                CanonicalPlayerId = x.CanonicalPlayerId is Guid rosterPlayerId &&
                                    validPlayerIds.Contains(rosterPlayerId)
                    ? rosterPlayerId
                    : null
            }).ToList()
        };
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

    private static string NormalizeMapKey(string mapName) =>
        mapName.Trim().ToLowerInvariant().Replace(' ', '_');

    private sealed record RosterResolution
    {
        public required string GamePlayerId { get; init; }
        public int SlotId { get; init; }
        public Guid? CanonicalPlayerId { get; init; }
        public bool? IsBot { get; init; }
    }

    private sealed class PlaytimeTotalsAggregate
    {
        public long KnownSeconds { get; set; }
        public long UnknownMapSeconds { get; set; }
        public long KnownBotSeconds { get; set; }
        public long UnclassifiedSeconds { get; set; }
        public bool HasKnownNonBot { get; set; }
        public bool HasUnclassified { get; set; }
    }

    private static List<RosterResolution> DeserializeRosterResolutions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<RosterResolution>>(json) ?? [];
    }

    private static List<RosterResolution> MergeRosterResolutions(
        IReadOnlyCollection<RosterResolution> previous,
        IReadOnlyCollection<PlaytimeRosterPlayerDto> incoming)
    {
        var merged = previous
            .GroupBy(x => (x.GamePlayerId, x.SlotId))
            .ToDictionary(x => x.Key, x => x.Last());

        foreach (var player in incoming)
        {
            var key = (player.GamePlayerId, player.SlotId);
            if (merged.TryGetValue(key, out var existing))
            {
                merged[key] = existing with
                {
                    CanonicalPlayerId = existing.CanonicalPlayerId ?? player.CanonicalPlayerId,
                    IsBot = existing.IsBot ?? player.IsBot
                };
            }
            else
            {
                merged[key] = new RosterResolution
                {
                    GamePlayerId = player.GamePlayerId,
                    SlotId = player.SlotId,
                    CanonicalPlayerId = player.CanonicalPlayerId,
                    IsBot = player.IsBot
                };
            }
        }

        return merged.Values
            .OrderBy(x => x.SlotId)
            .ThenBy(x => x.GamePlayerId)
            .ToList();
    }

    private static bool AreRosterResolutionsEqual(
        IReadOnlyCollection<RosterResolution> previous,
        IReadOnlyCollection<RosterResolution> merged)
    {
        if (previous.Count != merged.Count)
        {
            return false;
        }

        var prior = previous
            .GroupBy(x => (x.GamePlayerId, x.SlotId))
            .ToDictionary(x => x.Key, x => x.Last());
        foreach (var resolution in merged)
        {
            if (!prior.TryGetValue((resolution.GamePlayerId, resolution.SlotId), out var existing))
            {
                return false;
            }
            if (existing.CanonicalPlayerId != resolution.CanonicalPlayerId ||
                existing.IsBot != resolution.IsBot)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasConflictingResolution(
        PlaytimeObservation existing,
        PlaytimeObservationDto incoming,
        IReadOnlyCollection<RosterResolution> previousRoster)
    {
        if ((existing.CanonicalPlayerId is not null &&
             incoming.CanonicalPlayerId is not null &&
             existing.CanonicalPlayerId != incoming.CanonicalPlayerId) ||
            (existing.IsBot is not null && incoming.IsBot is not null && existing.IsBot != incoming.IsBot))
        {
            return true;
        }

        var previous = previousRoster
            .GroupBy(x => (x.GamePlayerId, x.SlotId))
            .ToDictionary(x => x.Key, x => x.Last());
        return incoming.Roster?.Any(player =>
            previous.TryGetValue((player.GamePlayerId, player.SlotId), out var resolved) &&
            ((resolved.CanonicalPlayerId is not null &&
              player.CanonicalPlayerId is not null &&
              resolved.CanonicalPlayerId != player.CanonicalPlayerId) ||
             (resolved.IsBot is not null && player.IsBot is not null && resolved.IsBot != player.IsBot))) == true;
    }

    private static string SerializeRosterResolutions(IReadOnlyCollection<RosterResolution> resolutions) =>
        JsonSerializer.Serialize(resolutions);

    private static byte[] ComputeHash(PlaytimeObservationDto observation)
    {
        var payload = JsonSerializer.Serialize(new
        {
            observation.ObservationId,
            observation.ServerId,
            observation.GameType,
            observation.Kind,
            EventUtc = NormalizeUtc(observation.EventUtc),
            observation.TimestampQuality,
            observation.ProducerEpoch,
            observation.ProducerSequence,
            observation.ConnectionCorrelationId,
            observation.GamePlayerId,
            observation.SlotId,
            observation.MapName,
            observation.RosterCompleteness,
            Roster = observation.Roster?.Select(x => new
            {
                x.GamePlayerId,
                x.SlotId
            }),
            Coverage = observation.Coverage is null
                ? null
                : new
                {
                    observation.Coverage.ProducerEpoch,
                    observation.Coverage.ProducerStartedUtc,
                    observation.Coverage.ReportedUtc,
                    observation.Coverage.DroppedCapacityMax,
                    observation.Coverage.DroppedAttemptsMax,
                    observation.Coverage.DroppedAgeMax
                }
        });
        return SHA256.HashData(Encoding.UTF8.GetBytes(payload));
    }
}
