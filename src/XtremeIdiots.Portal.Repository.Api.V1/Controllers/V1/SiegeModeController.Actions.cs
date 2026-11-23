using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.SiegeMode;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public partial class SiegeModeController
{
    [HttpPost("game-servers/{gameServerId:guid}/siege-sessions/{sessionId:guid}/actions")]
    [ProducesResponseType<ApiResponse<SiegeActionClaimDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClaimAction(Guid gameServerId, Guid sessionId, ClaimSiegeActionDto request, CancellationToken cancellationToken = default)
    {
        if (request is null || request.ActionId == Guid.Empty || request.ConnectionId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.PlayerGuid) || request.PlayerGuid.Length > 50 || request.SlotId is null or < 0 ||
            string.IsNullOrWhiteSpace(request.RuleId) || request.RuleId.Length > 64 || request.BaselineRevision is null or < 0)
        {
            return new ApiResult<SiegeActionClaimDto>(HttpStatusCode.BadRequest).ToHttpResult();
        }
        var result = await ExecuteMutationAsync(async () =>
        {
            await LockServerAsync(gameServerId, cancellationToken).ConfigureAwait(false);
            var now = DateTime.UtcNow;
            var session = await context.SiegeSessions.SingleOrDefaultAsync(
                s => s.GameServerId == gameServerId && s.SessionId == sessionId, cancellationToken).ConfigureAwait(false);
            if (session is null)
            {
                return new ApiResult<SiegeActionClaimDto>(HttpStatusCode.NotFound);
            }
            if (session.EndedAtUtc is not null || session.ExpiresAtUtc <= now || session.BaselineRevision != request.BaselineRevision)
            {
                return new ApiResult<SiegeActionClaimDto>(HttpStatusCode.Conflict);
            }
            if (!await context.GameServers.AnyAsync(s => s.GameServerId == gameServerId && !s.Deleted &&
                s.GameType == session.GameType, cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<SiegeActionClaimDto>(HttpStatusCode.Conflict);
            }
            var settings = DeserializeSettings(session.SettingsJson);
            if (!settings.Rules.Any(r => r.Enabled && string.Equals(r.Id.Trim(), request.RuleId, StringComparison.OrdinalIgnoreCase)))
            {
                return new ApiResult<SiegeActionClaimDto>(HttpStatusCode.BadRequest);
            }
            var existing = await context.SiegeActions.SingleOrDefaultAsync(
                a => a.SessionId == sessionId && a.ConnectionId == request.ConnectionId, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                if (existing.ActionId != request.ActionId || existing.SlotId != request.SlotId ||
                    !string.Equals(existing.PlayerGuid, request.PlayerGuid.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(existing.RuleId, request.RuleId, StringComparison.OrdinalIgnoreCase))
                {
                    return new ApiResult<SiegeActionClaimDto>(HttpStatusCode.Conflict);
                }
                return new ApiResponse<SiegeActionClaimDto>(new SiegeActionClaimDto { Action = ActionDto(existing, session), ShouldDispatch = false }).ToApiResult();
            }
            if (await context.SiegeActions.AnyAsync(a => a.ActionId == request.ActionId, cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<SiegeActionClaimDto>(HttpStatusCode.Conflict);
            }
            var action = new SiegeAction
            {
                ActionId = request.ActionId,
                SessionId = sessionId,
                ConnectionId = request.ConnectionId,
                PlayerGuid = request.PlayerGuid.Trim(),
                SlotId = request.SlotId.Value,
                RuleId = request.RuleId,
                ClaimedAtUtc = now,
                Outcome = session.Mode == nameof(SiegeSessionMode.DryRun) ? SiegeActionOutcomes.WouldKick : SiegeActionOutcomes.Pending,
                OutcomeAtUtc = session.Mode == nameof(SiegeSessionMode.DryRun) ? now : null
            };
            if (context.Database.IsSqlServer())
            {
                var inserted = await context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO dbo.SiegeActions
                        (ActionId, SessionId, ConnectionId, PlayerGuid, SlotId, RuleId, ClaimedAtUtc, Outcome, OutcomeAtUtc)
                    SELECT {action.ActionId}, {action.SessionId}, {action.ConnectionId}, {action.PlayerGuid},
                        {action.SlotId}, {action.RuleId}, SYSUTCDATETIME(), {action.Outcome},
                        CASE WHEN {session.Mode} = 'DryRun' THEN SYSUTCDATETIME() ELSE NULL END
                    FROM dbo.SiegeSessions
                    WHERE SessionId = {sessionId} AND GameServerId = {gameServerId}
                        AND EndedAtUtc IS NULL AND ExpiresAtUtc > SYSUTCDATETIME()
                        AND BaselineRevision = {request.BaselineRevision}
                        AND EXISTS (SELECT 1 FROM dbo.GameServers WHERE GameServerId = {gameServerId}
                            AND Deleted = 0 AND GameType = {session.GameType});
                    """, cancellationToken).ConfigureAwait(false);
                if (inserted != 1)
                {
                    return new ApiResult<SiegeActionClaimDto>(HttpStatusCode.Conflict);
                }
                action = await context.SiegeActions.SingleAsync(a => a.ActionId == action.ActionId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                context.SiegeActions.Add(action);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            return new ApiResponse<SiegeActionClaimDto>(new SiegeActionClaimDto
            {
                Action = ActionDto(action, session),
                ShouldDispatch = session.Mode == nameof(SiegeSessionMode.Enforce)
            }).ToApiResult();
        }, cancellationToken).ConfigureAwait(false);
        if (result.Result?.Data?.Action.Outcome == SiegeActionOutcomes.WouldKick)
        {
            logger?.LogInformation("Siege session {SessionId} recorded would-kick action {ActionId} in DryRun mode",
                sessionId, request.ActionId);
        }
        return result.ToHttpResult();
    }

    [HttpPut("game-servers/{gameServerId:guid}/siege-sessions/{sessionId:guid}/actions/{actionId:guid}/outcome")]
    [ProducesResponseType<ApiResponse<SiegeActionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateActionOutcome(Guid gameServerId, Guid sessionId, Guid actionId, UpdateSiegeActionOutcomeDto request, CancellationToken cancellationToken = default)
    {
        if (request is null || request.ConnectionId == Guid.Empty || request.Detail?.Length > 1024 ||
            request.Outcome is not (SiegeActionOutcomes.Queued or SiegeActionOutcomes.Dispatched or
                SiegeActionOutcomes.Applied or SiegeActionOutcomes.Failed or SiegeActionOutcomes.Stale))
        {
            return new ApiResult<SiegeActionDto>(HttpStatusCode.BadRequest).ToHttpResult();
        }
        var result = await ExecuteMutationAsync(async () =>
        {
            await LockServerAsync(gameServerId, cancellationToken).ConfigureAwait(false);
            var action = await context.SiegeActions.Include(a => a.Session).SingleOrDefaultAsync(
                a => a.ActionId == actionId && a.SessionId == sessionId && a.Session.GameServerId == gameServerId,
                cancellationToken).ConfigureAwait(false);
            if (action is null)
            {
                return new ApiResult<SiegeActionDto>(HttpStatusCode.NotFound);
            }
            if (action.ConnectionId != request.ConnectionId)
            {
                return new ApiResult<SiegeActionDto>(HttpStatusCode.Conflict);
            }
            if (action.Outcome != SiegeActionOutcomes.Pending)
            {
                return action.Outcome == request.Outcome && action.Detail == request.Detail
                    ? new ApiResponse<SiegeActionDto>(ActionDto(action, action.Session)).ToApiResult()
                    : new ApiResult<SiegeActionDto>(HttpStatusCode.Conflict);
            }
            action.Outcome = request.Outcome;
            action.Detail = request.Detail;
            action.OutcomeAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse<SiegeActionDto>(ActionDto(action, action.Session)).ToApiResult();
        }, cancellationToken).ConfigureAwait(false);
        return result.ToHttpResult();
    }

    [HttpPost("game-servers/{gameServerId:guid}/siege-sessions/{sessionId:guid}/checks")]
    [ProducesResponseType<ApiResponse<SiegeCheckDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordCheck(Guid gameServerId, Guid sessionId, RecordSiegeCheckDto request, CancellationToken cancellationToken = default)
    {
        if (request is null || request.CheckId == Guid.Empty || request.ConnectionId == Guid.Empty ||
            request.PlayerGuid?.Length > 50 || string.IsNullOrWhiteSpace(request.Detail) || request.Detail.Length > 1024 ||
            request.Outcome is not ("Unknown" or "Unavailable" or "Failed" or "Suppressed"))
        {
            return new ApiResult<SiegeCheckDto>(HttpStatusCode.BadRequest).ToHttpResult();
        }
        var result = await ExecuteMutationAsync(async () =>
        {
            await LockServerAsync(gameServerId, cancellationToken).ConfigureAwait(false);
            var previous = await context.SiegeChecks.Include(c => c.Session).SingleOrDefaultAsync(c => c.CheckId == request.CheckId, cancellationToken).ConfigureAwait(false);
            if (previous is not null)
            {
                return previous.SessionId == sessionId && previous.Session.GameServerId == gameServerId &&
                    previous.ConnectionId == request.ConnectionId && previous.PlayerGuid == request.PlayerGuid?.Trim() &&
                    previous.Outcome == request.Outcome && previous.Detail == request.Detail
                    ? new ApiResponse<SiegeCheckDto>(CheckDto(previous, previous.Session)).ToApiResult()
                    : new ApiResult<SiegeCheckDto>(HttpStatusCode.Conflict);
            }
            var session = await context.SiegeSessions.SingleOrDefaultAsync(
                s => s.GameServerId == gameServerId && s.SessionId == sessionId, cancellationToken).ConfigureAwait(false);
            if (session is null)
            {
                return new ApiResult<SiegeCheckDto>(HttpStatusCode.NotFound);
            }
            if (session.EndedAtUtc is not null || session.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return new ApiResult<SiegeCheckDto>(HttpStatusCode.Conflict);
            }
            var check = new SiegeCheck
            {
                CheckId = request.CheckId,
                SessionId = sessionId,
                ConnectionId = request.ConnectionId,
                PlayerGuid = request.PlayerGuid?.Trim(),
                OccurredAtUtc = DateTime.UtcNow,
                Outcome = request.Outcome,
                Detail = request.Detail
            };
            context.SiegeChecks.Add(check);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse<SiegeCheckDto>(CheckDto(check, session)).ToApiResult();
        }, cancellationToken).ConfigureAwait(false);
        return result.ToHttpResult();
    }

    private static SiegeActionDto ActionDto(SiegeAction action, SiegeSession session) => new()
    {
        ActionId = action.ActionId,
        SessionId = action.SessionId,
        ConnectionId = action.ConnectionId,
        GameServerId = session.GameServerId,
        GameType = session.GameType,
        Mode = Enum.Parse<SiegeSessionMode>(session.Mode),
        PlayerGuid = action.PlayerGuid,
        SlotId = action.SlotId,
        RuleId = action.RuleId,
        ClaimedAtUtc = DateTime.SpecifyKind(action.ClaimedAtUtc, DateTimeKind.Utc),
        Outcome = action.Outcome,
        OutcomeAtUtc = action.OutcomeAtUtc is { } time ? DateTime.SpecifyKind(time, DateTimeKind.Utc) : null,
        Detail = action.Detail
    };

    private static SiegeCheckDto CheckDto(SiegeCheck check, SiegeSession session) => new()
    {
        CheckId = check.CheckId,
        SessionId = check.SessionId,
        ConnectionId = check.ConnectionId,
        GameServerId = session.GameServerId,
        GameType = session.GameType,
        Mode = Enum.Parse<SiegeSessionMode>(session.Mode),
        PlayerGuid = check.PlayerGuid,
        OccurredAtUtc = DateTime.SpecifyKind(check.OccurredAtUtc, DateTimeKind.Utc),
        Outcome = check.Outcome,
        Detail = check.Detail
    };
}
