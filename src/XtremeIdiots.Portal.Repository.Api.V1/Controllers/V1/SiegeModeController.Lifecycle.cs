using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.SiegeMode;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public partial class SiegeModeController
{
    [HttpPost("game-servers/{gameServerId:guid}/siege-sessions/{sessionId:guid}/end")]
    [ProducesResponseType<ApiResponse<SiegeSessionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EndSession(Guid gameServerId, Guid sessionId, ChangeSiegeSessionDto request, CancellationToken cancellationToken = default)
    {
        var result = await ChangeSessionAsync(gameServerId, sessionId, request, false, cancellationToken).ConfigureAwait(false);
        if (result.StatusCode == HttpStatusCode.OK)
        {
            logger?.LogInformation("Siege session {SessionId} ended on {GameServerId} in {Mode} mode",
                sessionId, gameServerId, result.Result?.Data?.Mode);
        }
        return result.ToHttpResult();
    }

    [HttpPost("game-servers/{gameServerId:guid}/siege-sessions/{sessionId:guid}/extend")]
    [ProducesResponseType<ApiResponse<SiegeSessionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExtendSession(Guid gameServerId, Guid sessionId, ChangeSiegeSessionDto request, CancellationToken cancellationToken = default)
    {
        var result = await ChangeSessionAsync(gameServerId, sessionId, request, true, cancellationToken).ConfigureAwait(false);
        if (result.StatusCode == HttpStatusCode.OK)
        {
            logger?.LogInformation("Siege session {SessionId} extended on {GameServerId} in {Mode} mode",
                sessionId, gameServerId, result.Result?.Data?.Mode);
        }
        return result.ToHttpResult();
    }

    private async Task<ApiResult<SiegeSessionDto>> ChangeSessionAsync(
        Guid gameServerId, Guid sessionId, ChangeSiegeSessionDto request, bool extend, CancellationToken cancellationToken)
    {
        if (gameServerId == Guid.Empty || sessionId == Guid.Empty || request is null ||
            request.OperationId == Guid.Empty || !ValidActor(request.Actor) ||
            (extend ? request.ExtensionMinutes is null or <= 0 : request.ExtensionMinutes is not null))
        {
            return new ApiResult<SiegeSessionDto>(HttpStatusCode.BadRequest);
        }

        return await ExecuteMutationAsync(async () =>
        {
            if (!extend)
            {
                await SiegeHistoryLock.AcquireAsync(context, cancellationToken).ConfigureAwait(false);
            }
            await LockServerAsync(gameServerId, cancellationToken).ConfigureAwait(false);
            var session = await context.SiegeSessions.SingleOrDefaultAsync(
                s => s.GameServerId == gameServerId && s.SessionId == sessionId, cancellationToken).ConfigureAwait(false);
            if (session is null)
            {
                return new ApiResult<SiegeSessionDto>(HttpStatusCode.NotFound);
            }

            var eventType = extend ? "Extended" : "Ended";
            var existing = await context.SiegeSessionEvents.FindAsync([request.OperationId], cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                return existing.SessionId == sessionId && existing.EventType == eventType &&
                    existing.Actor == request.Actor && existing.ExtensionMinutes == request.ExtensionMinutes
                    ? await ResultAsync(session, HttpStatusCode.OK, cancellationToken).ConfigureAwait(false)
                    : new ApiResult<SiegeSessionDto>(HttpStatusCode.Conflict);
            }

            var now = DateTime.UtcNow;
            if (session.EndedAtUtc is not null || session.ExpiresAtUtc <= now)
            {
                return new ApiResult<SiegeSessionDto>(HttpStatusCode.Conflict);
            }
            if (extend)
            {
                if (session.ExpiresAtUtc > DateTime.MaxValue.AddMinutes(-request.ExtensionMinutes!.Value))
                {
                    return new ApiResult<SiegeSessionDto>(HttpStatusCode.BadRequest);
                }
                session.ExpiresAtUtc = session.ExpiresAtUtc.AddMinutes(request.ExtensionMinutes.Value);
            }
            else
            {
                session.EndedAtUtc = now;
            }
            context.SiegeSessionEvents.Add(new SiegeSessionEvent
            {
                OperationId = request.OperationId,
                SessionId = sessionId,
                EventType = eventType,
                Actor = request.Actor,
                OccurredAtUtc = now,
                ExpiresAtUtc = session.ExpiresAtUtc,
                ExtensionMinutes = request.ExtensionMinutes
            });
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return await ResultAsync(session, HttpStatusCode.OK, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("game-servers/{gameServerId:guid}/siege-sessions/{sessionId:guid}/baseline")]
    [ProducesResponseType<ApiResponse<SiegeBaselineDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBaseline(Guid gameServerId, Guid sessionId, SiegeBaselineRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request is null || request.PlayerGuid?.Trim().Length > 50 || request.IpAddress?.Length > 60)
        {
            return new ApiResult<SiegeBaselineDto>(HttpStatusCode.BadRequest).ToHttpResult();
        }
        var result = await ExecuteMutationAsync(async () =>
        {
            await LockServerAsync(gameServerId, cancellationToken).ConfigureAwait(false);
            var now = DateTime.UtcNow;
            var session = await context.SiegeSessions.AsNoTracking().SingleOrDefaultAsync(
                s => s.GameServerId == gameServerId && s.SessionId == sessionId && !s.GameServer.Deleted &&
                     s.GameServer.GameType == s.GameType &&
                     s.EndedAtUtc == null && s.ExpiresAtUtc > now, cancellationToken).ConfigureAwait(false);
            if (session is null)
            {
                return new ApiResult<SiegeBaselineDto>(HttpStatusCode.NotFound);
            }
            var guid = request.PlayerGuid?.Trim();
            var ip = NormalizeIp(request.IpAddress);
            bool? newGuid = string.IsNullOrEmpty(guid) ? null :
                !await context.SiegeBaselineGuids.AsNoTracking().AnyAsync(
                    g => g.SessionId == sessionId && g.PlayerGuid == guid, cancellationToken).ConfigureAwait(false);
            bool? newIp = ip is null ? null :
                !await context.SiegeBaselineIps.AsNoTracking().AnyAsync(
                    a => a.SessionId == sessionId && a.Address == ip, cancellationToken).ConfigureAwait(false);
            return new ApiResponse<SiegeBaselineDto>(new SiegeBaselineDto
            {
                NewGuid = newGuid,
                NewIp = newIp,
                BaselineRevision = session.BaselineRevision
            }).ToApiResult();
        }, cancellationToken).ConfigureAwait(false);
        return result.ToHttpResult();
    }
}
