using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Playtime;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public sealed class PlaytimeController : ControllerBase, IPlaytimeApi
{
    private static readonly TimeSpan LateEventThreshold = TimeSpan.FromHours(24);
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
        var response = await ((IPlaytimeApi)this).BatchObservations(dto).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlaytimeObservationBatchResultDto>> IPlaytimeApi.BatchObservations(PlaytimeObservationBatchDto dto)
    {
        if (dto?.Observations is null)
        {
            return new ApiResult<PlaytimeObservationBatchResultDto>(HttpStatusCode.BadRequest);
        }

        if (dto.Observations.Count == 0 || dto.Observations.Count > 100)
        {
            return new ApiResult<PlaytimeObservationBatchResultDto>(HttpStatusCode.BadRequest);
        }

        var items = new List<PlaytimeObservationItemResultDto>(dto.Observations.Count);
        await using var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false);

        foreach (var observation in dto.Observations)
        {
            var now = DateTime.UtcNow;
            var hash = ComputeHash(observation);
            var existing = await context.PlaytimeObservations
                .FirstOrDefaultAsync(x => x.ObservationId == observation.ObservationId)
                .ConfigureAwait(false);

            if (existing is not null)
            {
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
                if (outcome == PlaytimeObservationOutcome.Duplicate)
                {
                    var identityResolved = observation.CanonicalPlayerId is not null &&
                                           existing.CanonicalPlayerId is null;
                    var botResolved = observation.IsBot is not null &&
                                      existing.IsBot is null;
                    var rosterResolutionsChanged = false;
                    if (observation.Roster is not null)
                    {
                        var previous = DeserializeRosterResolutions(existing.ResolvedRosterJson);
                        var merged = MergeRosterResolutions(previous, observation.Roster);
                        rosterResolutionsChanged = !AreRosterResolutionsEqual(previous, merged);
                        existing.ResolvedRosterJson = SerializeRosterResolutions(merged);
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
                        await ReconcileObservation(observation, now).ConfigureAwait(false);
                        await context.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                continue;
            }

            var server = await context.GameServers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.GameServerId == observation.ServerId)
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

            if (now - observation.EventUtc > LateEventThreshold)
            {
                items.Add(Rejected(observation.ObservationId, "Event is too late for the accepted correction window."));
                continue;
            }

            if (observation.ProducerEpoch is not null && observation.ProducerSequence is not null)
            {
                var sequenceExists = await context.PlaytimeObservations
                    .AsNoTracking()
                    .AnyAsync(x => x.ServerId == observation.ServerId &&
                                   x.ProducerEpoch == observation.ProducerEpoch &&
                                   x.ProducerSequence == observation.ProducerSequence)
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
                EventUtc = NormalizeUtc(observation.EventUtc),
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
                PayloadHash = hash,
                Outcome = (byte)PlaytimeObservationOutcome.Accepted
            };

            context.PlaytimeObservations.Add(fact);
            await ReconcileObservation(observation, now).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
            items.Add(new PlaytimeObservationItemResultDto
            {
                ObservationId = observation.ObservationId,
                Outcome = PlaytimeObservationOutcome.Accepted
            });
        }

        await transaction.CommitAsync().ConfigureAwait(false);
        var result = new PlaytimeObservationBatchResultDto { Items = items };
        return new ApiResponse<PlaytimeObservationBatchResultDto>(result).ToApiResult();
    }

    [HttpGet("players/{playerId:guid}/sessions")]
    [ProducesResponseType<IReadOnlyList<PlayerSessionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerSessions(Guid playerId, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, [FromQuery] int skipEntries = 0, [FromQuery] int takeEntries = 100, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlaytimeApi)this).GetPlayerSessions(playerId, fromUtc, toUtc, skipEntries, takeEntries).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<IReadOnlyList<PlayerSessionDto>>> IPlaytimeApi.GetPlayerSessions(Guid playerId, DateTime? fromUtc, DateTime? toUtc, int skipEntries, int takeEntries)
    {
        if (!await context.Players.AsNoTracking().AnyAsync(x => x.PlayerId == playerId).ConfigureAwait(false))
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
            query = query.Where(x => x.StartUtc < fromUtc.Value || x.EndUtc > fromUtc.Value);
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
            .ToListAsync()
            .ConfigureAwait(false);

        var dto = new List<PlayerSessionDto>(sessions.Count);
        foreach (var session in sessions)
        {
            var end = session.EndUtc ?? session.LastConfirmedUtc;
            var measured = session.IsBot == true
                ? 0
                : await GetSessionMeasuredSeconds(session.SessionId, session.StartUtc, end).ConfigureAwait(false);
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
        var response = await ((IPlaytimeApi)this).GetTotals(fromUtc, toUtc, playerId, serverId, gameType, mapName).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlaytimeTotalsDto>> IPlaytimeApi.GetTotals(DateTime fromUtc, DateTime toUtc, Guid? playerId, Guid? serverId, GameType? gameType, string? mapName)
    {
        if (fromUtc == default || toUtc == default || fromUtc >= toUtc)
        {
            return new ApiResult<PlaytimeTotalsDto>(HttpStatusCode.BadRequest);
        }

        var segments = context.PlayerSessionMapSegments
            .AsNoTracking()
            .Include(x => x.Session)
            .Include(x => x.MapRun)
            .ThenInclude(x => x!.Map)
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
            segments = segments.Where(x => !x.UnknownMap &&
                ((x.MapRun!.Map != null && x.MapRun.Map.MapName == mapName) ||
                 x.MapRun!.NormalizedMapKey == mapName));
        }

        var list = await segments.ToListAsync().ConfigureAwait(false);
        long knownSeconds = 0;
        long unknownSeconds = 0;
        long knownBotSeconds = 0;
        long unclassifiedSeconds = 0;
        var hasKnownNonBot = false;
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

        var dataThrough = await context.PlaytimeObservations
            .AsNoTracking()
            .Where(x => x.Outcome == (byte)PlaytimeObservationOutcome.Accepted)
            .Where(x => (!serverId.HasValue || x.ServerId == serverId) &&
                        (!gameType.HasValue || x.GameType == (int)gameType))
            .Select(x => (DateTime?)x.EventUtc)
            .MaxAsync()
            .ConfigureAwait(false);

        var coverageFacts = await context.PlaytimeObservations
            .AsNoTracking()
            .Where(x => x.ProducerEpoch != null &&
                        (x.DroppedCapacityMax != null ||
                         x.DroppedAttemptsMax != null ||
                         x.DroppedAgeMax != null))
            .Where(x => (!serverId.HasValue || x.ServerId == serverId) &&
                        (!gameType.HasValue || x.GameType == (int)gameType))
            .ToListAsync()
            .ConfigureAwait(false);
        var coverageReports = coverageFacts
            .GroupBy(x => x.ProducerEpoch!.Value)
            .Select(group =>
            {
                var latest = group
                    .OrderByDescending(x => x.EventUtc)
                    .ThenByDescending(x => x.ObservationId)
                    .First();
                return new PlaytimeCoverageCountersDto
                {
                    ProducerEpoch = group.Key,
                    ProducerStartedUtc = latest.ProducerStartedUtc,
                    ReportedUtc = latest.EventUtc,
                    DroppedCapacityMax = group.Max(x => x.DroppedCapacityMax ?? 0),
                    DroppedAttemptsMax = group.Max(x => x.DroppedAttemptsMax ?? 0),
                    DroppedAgeMax = group.Max(x => x.DroppedAgeMax ?? 0)
                };
            })
            .OrderBy(x => x.ReportedUtc)
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

    private async Task ReconcileObservation(PlaytimeObservationDto observation, DateTime receivedUtc)
    {
        var eventUtc = NormalizeUtc(observation.EventUtc);
        if (observation.Kind == PlaytimeObservationKind.PlayerJoined)
        {
            if (observation.CanonicalPlayerId is null)
            {
                return;
            }

            await EnsureSession(observation.CanonicalPlayerId.Value, observation, eventUtc).ConfigureAwait(false);
        }
        else if (observation.Kind == PlaytimeObservationKind.PlayerLeft)
        {
            if (observation.CanonicalPlayerId is null)
            {
                return;
            }

            await CloseSession(observation.CanonicalPlayerId.Value, observation.ServerId, eventUtc,
                (byte)observation.TimestampQuality, 1).ConfigureAwait(false);
        }
        else if (observation.Kind == PlaytimeObservationKind.MapChanged)
        {
            await ChangeMapRun(observation, eventUtc).ConfigureAwait(false);
        }
        else if (observation.Kind == PlaytimeObservationKind.ServerRoster)
        {
            await ReconcileRoster(observation, eventUtc).ConfigureAwait(false);
            await ChangeMapRun(observation, eventUtc).ConfigureAwait(false);
        }
    }

    private async Task EnsureSession(Guid playerId, PlaytimeObservationDto observation, DateTime eventUtc)
    {
        var open = await context.PlayerSessions
            .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.ServerId == observation.ServerId && x.EndUtc == null)
            .ConfigureAwait(false);
        if (open is not null)
        {
            if (observation.IsBot is not null)
            {
                open.IsBot = observation.IsBot;
            }

            var sustainedGap = eventUtc - open.LastConfirmedUtc > ReconnectGapThreshold;
            if (sustainedGap)
            {
                open.EndUtc = open.LastConfirmedUtc;
                open.EndQuality = (byte)PlaytimeTimestampQuality.InferredTimestamp;
                open.ClosureReason = 3;
                open.IsFinal = false;
                await RebuildSegments(open).ConfigureAwait(false);
            }
            else
            {
                open.LastConfirmedUtc = open.LastConfirmedUtc > eventUtc ? open.LastConfirmedUtc : eventUtc;
                await RebuildSegments(open).ConfigureAwait(false);
                return;
            }
        }

        var session = new PlayerSession
        {
            SessionId = observation.ConnectionCorrelationId ?? Guid.NewGuid(),
            PlayerId = playerId,
            ServerId = observation.ServerId,
            GameType = (int)observation.GameType,
            GamePlayerId = observation.GamePlayerId,
            IsBot = observation.IsBot,
            StartUtc = eventUtc,
            LastConfirmedUtc = eventUtc,
            StartQuality = (byte)observation.TimestampQuality,
            IsFinal = false
        };
        context.PlayerSessions.Add(session);
        await context.SaveChangesAsync().ConfigureAwait(false);
        await RebuildSegments(session).ConfigureAwait(false);
    }

    private async Task CloseSession(Guid playerId, Guid serverId, DateTime eventUtc, byte endQuality, byte closureReason)
    {
        var open = await context.PlayerSessions
            .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.ServerId == serverId && x.EndUtc == null)
            .ConfigureAwait(false);
        if (open is null)
        {
            return;
        }

        if (eventUtc < open.StartUtc)
        {
            return;
        }

        open.LastConfirmedUtc = open.LastConfirmedUtc > eventUtc ? open.LastConfirmedUtc : eventUtc;
        open.EndUtc = open.LastConfirmedUtc;
        open.EndQuality = endQuality;
        open.ClosureReason = closureReason;
        open.IsFinal = closureReason != 3;
        await RebuildSegments(open).ConfigureAwait(false);
    }

    private async Task<bool> IsStaleRosterObservation(PlaytimeObservationDto observation, DateTime eventUtc)
    {
        var latest = await context.PlaytimeObservations
            .AsNoTracking()
            .Where(x => x.ServerId == observation.ServerId &&
                        x.Kind == (byte)PlaytimeObservationKind.ServerRoster &&
                        x.Outcome == (byte)PlaytimeObservationOutcome.Accepted)
            .OrderByDescending(x => x.EventUtc)
            .ThenByDescending(x => x.ProducerSequence)
            .FirstOrDefaultAsync()
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

    private async Task ReconcileRoster(PlaytimeObservationDto observation, DateTime eventUtc)
    {
        if (observation.RosterCompleteness is PlaytimeRosterCompleteness.Failed or PlaytimeRosterCompleteness.NotApplicable)
        {
            return;
        }

        if (await IsStaleRosterObservation(observation, eventUtc).ConfigureAwait(false))
        {
            return;
        }

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
                    eventUtc).ConfigureAwait(false);
            }
        }

        var identityIncomplete = observation.Roster?.Any(x => x.CanonicalPlayerId is null) == true;
        if (observation.RosterCompleteness != PlaytimeRosterCompleteness.Complete || identityIncomplete)
        {
            return;
        }

        var open = await context.PlayerSessions
            .Where(x => x.ServerId == observation.ServerId && x.EndUtc == null)
            .ToListAsync().ConfigureAwait(false);
        foreach (var session in open)
        {
            if (ids.Contains(session.PlayerId))
            {
                continue;
            }

            session.LastConfirmedUtc = eventUtc;
            session.EndUtc = eventUtc;
            session.EndQuality = (byte)PlaytimeTimestampQuality.RosterSnapshotTimestamp;
            session.ClosureReason = 2;
            session.IsFinal = false;
            await RebuildSegments(session).ConfigureAwait(false);
        }
    }

    private async Task ChangeMapRun(PlaytimeObservationDto observation, DateTime eventUtc)
    {
        if (string.IsNullOrWhiteSpace(observation.MapName))
        {
            return;
        }

        var openRun = await context.ServerMapRuns
            .Where(x => x.ServerId == observation.ServerId && x.EndUtc == null)
            .OrderByDescending(x => x.StartUtc)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        var normalized = NormalizeMapKey(observation.MapName);
        if (openRun is not null &&
            string.Equals(openRun.NormalizedMapKey, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (openRun is not null)
        {
            openRun.EndUtc = eventUtc < openRun.StartUtc ? openRun.StartUtc : eventUtc;
            openRun.IsFinal = true;
        }

        var map = await context.Maps
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GameType == (int)observation.GameType &&
                                      (x.MapName == observation.MapName || x.DisplayName == observation.MapName))
            .ConfigureAwait(false);
        var run = new ServerMapRun
        {
            RunId = Guid.NewGuid(),
            ServerId = observation.ServerId,
            GameType = (int)observation.GameType,
            MapId = map?.MapId,
            NormalizedMapKey = normalized,
            StartUtc = eventUtc,
            AttributionQuality = map is null
                ? (byte)PlaytimeTimestampQuality.Unknown
                : (byte)observation.TimestampQuality,
            IsFinal = false
        };
        context.ServerMapRuns.Add(run);
        await context.SaveChangesAsync().ConfigureAwait(false);

        var sessions = await context.PlayerSessions
            .Where(x => x.ServerId == observation.ServerId && x.StartUtc < eventUtc)
            .ToListAsync().ConfigureAwait(false);
        foreach (var session in sessions)
        {
            await RebuildSegments(session).ConfigureAwait(false);
        }
    }

    private async Task RebuildSegments(PlayerSession session)
    {
        var end = session.EndUtc ?? session.LastConfirmedUtc;
        if (end <= session.StartUtc)
        {
            return;
        }

        var old = await context.PlayerSessionMapSegments
            .Where(x => x.SessionId == session.SessionId)
            .ToListAsync().ConfigureAwait(false);
        context.PlayerSessionMapSegments.RemoveRange(old);

        var runs = await context.ServerMapRuns
            .AsNoTracking()
            .Where(x => x.ServerId == session.ServerId &&
                        x.StartUtc < end &&
                        (x.EndUtc == null || x.EndUtc > session.StartUtc))
            .OrderBy(x => x.StartUtc)
            .ToListAsync().ConfigureAwait(false);

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

    private async Task<long> GetSessionMeasuredSeconds(Guid sessionId, DateTime start, DateTime end)
    {
        var segments = await context.PlayerSessionMapSegments
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId && !x.UnknownMap)
            .ToListAsync().ConfigureAwait(false);
        long seconds = 0;
        foreach (var segment in segments)
        {
            var from = segment.StartUtc > start ? segment.StartUtc : start;
            var to = segment.EndUtc < end ? segment.EndUtc : end;
            if (to > from)
            {
                seconds += (long)Math.Floor((to - from).TotalSeconds);
            }
        }
        return seconds;
    }

    private static PlaytimeObservationItemResultDto Rejected(Guid id, string reason) => new()
    {
        ObservationId = id,
        Outcome = PlaytimeObservationOutcome.Rejected,
        Reason = reason
    };

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
        var merged = previous.ToDictionary(
            x => (x.GamePlayerId, x.SlotId),
            x => x);

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

        var prior = previous.ToDictionary(x => (x.GamePlayerId, x.SlotId), x => x);
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
