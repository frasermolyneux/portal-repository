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
    private static readonly TimeSpan ReconnectGapThreshold = TimeSpan.FromMinutes(15);
    private readonly PortalDbContext context;

    public PlaytimeController(PortalDbContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
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
            foreach (var serverId in dto.Observations
                         .Select(x => x.ServerId)
                         .Distinct()
                         .OrderBy(x => x))
            {
                await PlaytimeServerLock.AcquireAsync(context, serverId, cancellationToken)
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

                var now = DateTime.UtcNow;
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
                    if (outcome == PlaytimeObservationOutcome.Duplicate &&
                        !await HasValidCanonicalPlayers(observation, cancellationToken).ConfigureAwait(false))
                    {
                        items[^1] = Rejected(
                            observation.ObservationId,
                            "Canonical player is unknown or belongs to another game.");
                        continue;
                    }
                    if (outcome == PlaytimeObservationOutcome.Duplicate && existing.IdentifiersRedacted)
                    {
                        continue;
                    }
                    if (outcome == PlaytimeObservationOutcome.Duplicate)
                    {
                        var previousRoster = DeserializeRosterResolutions(existing.ResolvedRosterJson);
                        if (HasConflictingResolution(existing, observation, previousRoster))
                        {
                            items[^1] = Rejected(
                                observation.ObservationId,
                                "Replay conflicts with a previously resolved player identity or bot classification.");
                            continue;
                        }

                        var identityResolved = observation.CanonicalPlayerId is not null &&
                                               existing.CanonicalPlayerId is null;
                        var botResolved = observation.IsBot is not null &&
                                          existing.IsBot is null;
                        var rosterResolutionsChanged = false;
                        var mergedRoster = previousRoster;
                        if (observation.Roster is not null)
                        {
                            var merged = MergeRosterResolutions(mergedRoster, observation.Roster);
                            rosterResolutionsChanged = !AreRosterResolutionsEqual(mergedRoster, merged);
                            mergedRoster = merged;
                            existing.ResolvedRosterJson = SerializeRosterResolutions(mergedRoster);
                        }
                        if (identityResolved)
                        {
                            if (observation.CanonicalPlayerId is not null)
                            {
                                existing.CanonicalPlayerId = observation.CanonicalPlayerId;
                            }
                        }
                        if (botResolved)
                        {
                            existing.IsBot = observation.IsBot;
                        }
                        if (identityResolved || botResolved || rosterResolutionsChanged)
                        {
                            var resolvedObservation = observation with
                            {
                                CanonicalPlayerId = existing.CanonicalPlayerId,
                                IsBot = existing.IsBot,
                                Roster = observation.Roster is null
                                    ? null
                                    : mergedRoster.Select(x => new PlaytimeRosterPlayerDto
                                    {
                                        GamePlayerId = x.GamePlayerId,
                                        SlotId = x.SlotId,
                                        CanonicalPlayerId = x.CanonicalPlayerId,
                                        IsBot = x.IsBot
                                    }).ToList()
                            };
                            await ReconcileObservation(resolvedObservation, now, cancellationToken).ConfigureAwait(false);
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
                group => group.Sum(segment =>
                {
                    var session = sessionsById[group.Key];
                    var start = segment.StartUtc > session.StartUtc ? segment.StartUtc : session.StartUtc;
                    var sessionEnd = session.EndUtc ?? session.LastConfirmedUtc;
                    var end = segment.EndUtc < sessionEnd ? segment.EndUtc : sessionEnd;
                    return end > start ? (long)Math.Floor((end - start).TotalSeconds) : 0;
                }));

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
                IsOpen = session.EndUtc is null,
                IsBot = session.IsBot,
                StartQuality = (PlaytimeTimestampQuality)session.StartQuality,
                EndQuality = session.EndQuality is null ? null : (PlaytimeTimestampQuality)session.EndQuality
            });
        }

        return new ApiResponse<IReadOnlyList<PlayerSessionDto>>(dto).ToApiResult();
    }

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
                                    THEN DATEDIFF_BIG(NANOSECOND, ClippedStartUtc, ClippedEndUtc) / 1000000000
                                    ELSE 0 END AS Seconds
                        FROM clipped
                    )
                    SELECT COALESCE(SUM(CASE WHEN IsBot = 1 THEN Seconds ELSE 0 END), 0) AS KnownBotSeconds,
                           COALESCE(SUM(CASE WHEN IsBot IS NULL THEN Seconds ELSE 0 END), 0) AS UnclassifiedSeconds,
                           COALESCE(SUM(CASE WHEN (IsBot = 0 OR IsBot IS NULL) AND UnknownMap = 1
                                             THEN Seconds ELSE 0 END), 0) AS UnknownMapSeconds,
                           COALESCE(SUM(CASE WHEN (IsBot = 0 OR IsBot IS NULL) AND UnknownMap = 0
                                             THEN Seconds ELSE 0 END), 0) AS KnownSeconds,
                           CAST(CASE WHEN MAX(CASE WHEN IsBot = 0 AND Seconds > 0 THEN 1 ELSE 0 END) = 1
                                     THEN 1 ELSE 0 END AS bit) AS HasKnownNonBot
                    FROM durations
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
        }
        else
        {
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

                var seconds = (long)Math.Floor((end - start).TotalSeconds);
                if (segment.Session.IsBot == true)
                {
                    knownBotSeconds += seconds;
                    continue;
                }
                if (segment.Session.IsBot is null)
                {
                    unclassifiedSeconds += seconds;
                }
                else
                {
                    hasKnownNonBot = true;
                }
                if (segment.UnknownMap)
                {
                    unknownSeconds += seconds;
                }
                else
                {
                    knownSeconds += seconds;
                }
            }
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
            .Where(x => x.ProducerEpoch != null &&
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
                        .OrderByDescending(x => x.EventUtc)
                        .ThenByDescending(x => x.ObservationId)
                        .Select(x => x.ProducerStartedUtc)
                        .FirstOrDefault(),
                ReportedUtc = group.Max(x => x.EventUtc),
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
            HumanOnlyCoverage = hasKnownNonBot && unclassifiedSeconds == 0,
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
        CancellationToken cancellationToken)
    {
        var eventUtc = NormalizeUtc(observation.EventUtc);
        if (observation.Kind == PlaytimeObservationKind.PlayerJoined)
        {
            if (observation.CanonicalPlayerId is null)
            {
                return;
            }

            await EnsureSession(observation.CanonicalPlayerId.Value, observation, eventUtc, cancellationToken: cancellationToken)
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
            await ReconcileRoster(observation, eventUtc, cancellationToken).ConfigureAwait(false);
            await ChangeMapRun(observation, eventUtc, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EnsureSession(
        Guid playerId,
        PlaytimeObservationDto observation,
        DateTime eventUtc,
        bool allowCreate = true,
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
                            (x.EndUtc ?? x.LastConfirmedUtc) >= eventUtc)
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
                            (x.EndUtc ?? x.LastConfirmedUtc) >= eventUtc &&
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
        var leaves = context.PlaytimeObservations
            .AsNoTracking()
            .Where(x => x.ServerId == session.ServerId &&
                        x.Kind == (byte)PlaytimeObservationKind.PlayerLeft &&
                        x.Outcome == (byte)PlaytimeObservationOutcome.Accepted &&
                        x.EventUtc > session.StartUtc &&
                        x.EventUtc <= session.StartUtc.Add(LateEventThreshold) &&
                        x.ConnectionCorrelationId == session.ConnectionCorrelationId &&
                        (x.CanonicalPlayerId == playerId ||
                         (x.CanonicalPlayerId == null && x.GamePlayerId == join.GamePlayerId)));
        var leave = await leaves
            .OrderBy(x => x.EventUtc)
            .ThenBy(x => x.ObservationId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (leave is null)
        {
            return;
        }

        var endUtc = leave.EventUtc > session.LastConfirmedUtc
            ? leave.EventUtc
            : session.LastConfirmedUtc;
        session.EndUtc = endUtc;
        session.EndQuality = leave.TimestampQuality;
        session.ClosureReason = 1;
        session.IsFinal = true;
        session.IsBot ??= leave.IsBot;
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
                .Where(x => x.PlayerId == playerId && x.ServerId == serverId && x.EndUtc == null);
            if (connectionCorrelationId is not null)
            {
                openSessions = openSessions.Where(x =>
                    x.ConnectionCorrelationId == null ||
                    x.ConnectionCorrelationId == connectionCorrelationId);
            }
            session = await openSessions.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
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
        var endUtc = session.LastConfirmedUtc > eventUtc ? session.LastConfirmedUtc : eventUtc;
        session.EndUtc = endUtc;
        session.EndQuality = endQuality;
        session.ClosureReason = closureReason;
        session.IsFinal = closureReason != 3;
        await RebuildSegments(session, cancellationToken).ConfigureAwait(false);
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
        CancellationToken cancellationToken)
    {
        if (observation.RosterCompleteness is PlaytimeRosterCompleteness.Failed or PlaytimeRosterCompleteness.NotApplicable)
        {
            return;
        }

        var stale = await IsStaleRosterObservation(observation, eventUtc, cancellationToken).ConfigureAwait(false);

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

        var identityIncomplete = observation.Roster?.Any(x => x.CanonicalPlayerId is null) == true;
        if (stale ||
            observation.RosterCompleteness != PlaytimeRosterCompleteness.Complete ||
            identityIncomplete)
        {
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
        CancellationToken cancellationToken)
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
                                x.Kind == (byte)PlaytimeObservationKind.ServerRoster),
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
        var roster = observation.Roster is null
            ? ""
            : JsonSerializer.Serialize(observation.Roster.Select(x => new
            {
                x.GamePlayerId,
                x.SlotId
            }));
        var coverage = observation.Coverage is null
            ? ""
            : $"{observation.Coverage.ProducerStartedUtc:O}|{observation.Coverage.DroppedCapacityMax}|{observation.Coverage.DroppedAttemptsMax}|{observation.Coverage.DroppedAgeMax}";
        var payload = $"{observation.ObservationId}|{observation.ServerId}|{observation.GameType}|{observation.Kind}|{NormalizeUtc(observation.EventUtc):O}|{observation.TimestampQuality}|{observation.ProducerEpoch}|{observation.ProducerSequence}|{observation.ConnectionCorrelationId}|{observation.GamePlayerId}|{observation.SlotId}|{observation.MapName}|{observation.RosterCompleteness}|{roster}|{coverage}";
        return SHA256.HashData(Encoding.UTF8.GetBytes(payload));
    }
}
