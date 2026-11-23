using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public partial class SiegeModeController
{
    [HttpDelete("data-maintenance/prune-siege-mode")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> PruneSiegeMode(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-365);
        var completed = await BoundedPruner.RunAsync([
            token => ExpireBatchAsync(now, token),
            token => context.SiegeBaselineGuids
                .Where(g => g.Session.EndedAtUtc != null || g.Session.ExpiresAtUtc <= now)
                .OrderBy(g => g.SessionId).ThenBy(g => g.PlayerGuid)
                .Take(BoundedPruner.BatchSize).ExecuteDeleteAsync(token),
            token => context.SiegeBaselineIps
                .Where(i => i.Session.EndedAtUtc != null || i.Session.ExpiresAtUtc <= now)
                .OrderBy(i => i.SessionId).ThenBy(i => i.Address)
                .Take(BoundedPruner.BatchSize).ExecuteDeleteAsync(token),
            token => context.SiegeActions.Where(a => a.Session.EndedAtUtc < cutoff)
                .OrderBy(a => a.ActionId).Take(BoundedPruner.BatchSize).ExecuteDeleteAsync(token),
            token => context.SiegeChecks.Where(c => c.Session.EndedAtUtc < cutoff)
                .OrderBy(c => c.CheckId).Take(BoundedPruner.BatchSize).ExecuteDeleteAsync(token),
            token => context.SiegeSessionEvents.Where(e => e.Session.EndedAtUtc < cutoff)
                .OrderBy(e => e.OperationId).Take(BoundedPruner.BatchSize).ExecuteDeleteAsync(token),
            token => context.SiegeSessions.Where(s => s.EndedAtUtc < cutoff &&
                !s.SiegeBaselineGuids.Any() && !s.SiegeBaselineIps.Any() &&
                !s.SiegeActions.Any() && !s.SiegeChecks.Any() && !s.SiegeSessionEvents.Any())
                .OrderBy(s => s.SessionId).Take(BoundedPruner.BatchSize).ExecuteDeleteAsync(token)
        ], cancellationToken).ConfigureAwait(false);
        return new ApiResponse().ToApiResult(completed ? HttpStatusCode.OK : HttpStatusCode.Accepted).ToHttpResult();
    }

    private async Task<int> ExpireBatchAsync(DateTime now, CancellationToken cancellationToken)
    {
        var candidates = await context.SiegeSessions.AsNoTracking()
            .Where(s => s.EndedAtUtc == null && s.ExpiresAtUtc <= now)
            .OrderBy(s => s.ExpiresAtUtc).ThenBy(s => s.SessionId)
            .Select(s => new { s.SessionId, s.GameServerId })
            .Take(100).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var candidate in candidates)
        {
            await ExecuteMutationAsync(async () =>
            {
                await LockServerAsync(candidate.GameServerId, cancellationToken).ConfigureAwait(false);
                var session = await context.SiegeSessions.SingleOrDefaultAsync(
                    s => s.SessionId == candidate.SessionId, cancellationToken).ConfigureAwait(false);
                if (session is null || session.EndedAtUtc is not null || session.ExpiresAtUtc > now)
                {
                    return false;
                }
                session.EndedAtUtc = session.ExpiresAtUtc;
                context.SiegeSessionEvents.Add(new SiegeSessionEvent
                {
                    OperationId = Guid.NewGuid(),
                    SessionId = session.SessionId,
                    EventType = "Expired",
                    Actor = "system",
                    OccurredAtUtc = session.ExpiresAtUtc,
                    ExpiresAtUtc = session.ExpiresAtUtc
                });
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }, cancellationToken).ConfigureAwait(false);
        }
        return candidates.Count;
    }
}
