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
