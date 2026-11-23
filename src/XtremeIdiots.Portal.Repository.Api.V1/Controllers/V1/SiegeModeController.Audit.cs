using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.SiegeMode;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public partial class SiegeModeController
{
    [HttpGet("game-servers/{gameServerId:guid}/siege-sessions")]
    [ProducesResponseType<ApiResponse<CollectionModel<SiegeSessionDto>>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetSessions(Guid gameServerId, int skipEntries = 0, int takeEntries = 20, CancellationToken cancellationToken = default) =>
        SearchSessions(new SiegeAuditQueryDto { GameServerIds = [gameServerId], SkipEntries = skipEntries, TakeEntries = takeEntries }, cancellationToken);

    [HttpPost("siege-sessions/search")]
    [ProducesResponseType<ApiResponse<CollectionModel<SiegeSessionDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSessions(SiegeAuditQueryDto request, CancellationToken cancellationToken = default)
    {
        if (!ValidAuditQuery(request))
        {
            return new ApiResult<CollectionModel<SiegeSessionDto>>(HttpStatusCode.BadRequest).ToHttpResult();
        }
        var query = context.SiegeSessions.AsNoTracking().Where(s => request.GameServerIds.Contains(s.GameServerId));
        if (request.FromUtc is { } from)
        {
            query = query.Where(s => s.StartedAtUtc >= from);
        }
        if (request.ToUtc is { } to)
        {
            query = query.Where(s => s.StartedAtUtc < to);
        }
        var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query.OrderByDescending(s => s.StartedAtUtc).ThenBy(s => s.SessionId)
            .Skip(request.SkipEntries).Take(request.TakeEntries)
            .Select(s => new { Session = s, ActionCount = s.SiegeActions.Count })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var data = new CollectionModel<SiegeSessionDto>(items.Select(s => ToDto(s.Session, s.ActionCount)).ToList());
        return new ApiResponse<CollectionModel<SiegeSessionDto>>(data)
        {
            Pagination = new ApiPagination(count, count, request.SkipEntries, request.TakeEntries)
        }.ToApiResult().ToHttpResult();
    }

    [HttpPost("siege-actions/search")]
    [ProducesResponseType<ApiResponse<CollectionModel<SiegeActionDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchActions(SiegeAuditQueryDto request, CancellationToken cancellationToken = default)
    {
        if (!ValidAuditQuery(request))
        {
            return new ApiResult<CollectionModel<SiegeActionDto>>(HttpStatusCode.BadRequest).ToHttpResult();
        }
        var query = context.SiegeActions.AsNoTracking().Where(a => request.GameServerIds.Contains(a.Session.GameServerId));
        if (request.FromUtc is { } from)
        {
            query = query.Where(a => a.ClaimedAtUtc >= from);
        }
        if (request.ToUtc is { } to)
        {
            query = query.Where(a => a.ClaimedAtUtc < to);
        }
        var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query.Include(a => a.Session)
            .OrderByDescending(a => a.ClaimedAtUtc).ThenBy(a => a.ActionId)
            .Skip(request.SkipEntries).Take(request.TakeEntries).ToListAsync(cancellationToken).ConfigureAwait(false);
        var data = new CollectionModel<SiegeActionDto>(items.Select(a => ActionDto(a, a.Session)).ToList());
        return new ApiResponse<CollectionModel<SiegeActionDto>>(data)
        {
            Pagination = new ApiPagination(count, count, request.SkipEntries, request.TakeEntries)
        }.ToApiResult().ToHttpResult();
    }

    [HttpPost("siege-checks/search")]
    [ProducesResponseType<ApiResponse<CollectionModel<SiegeCheckDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchChecks(SiegeAuditQueryDto request, CancellationToken cancellationToken = default)
    {
        if (!ValidAuditQuery(request))
        {
            return new ApiResult<CollectionModel<SiegeCheckDto>>(HttpStatusCode.BadRequest).ToHttpResult();
        }
        var query = context.SiegeChecks.AsNoTracking().Where(c => request.GameServerIds.Contains(c.Session.GameServerId));
        if (request.FromUtc is { } from)
        {
            query = query.Where(c => c.OccurredAtUtc >= from);
        }
        if (request.ToUtc is { } to)
        {
            query = query.Where(c => c.OccurredAtUtc < to);
        }
        var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query.Include(c => c.Session).OrderByDescending(c => c.OccurredAtUtc).ThenBy(c => c.CheckId)
            .Skip(request.SkipEntries).Take(request.TakeEntries).ToListAsync(cancellationToken).ConfigureAwait(false);
        var data = new CollectionModel<SiegeCheckDto>(items.Select(c => CheckDto(c, c.Session)).ToList());
        return new ApiResponse<CollectionModel<SiegeCheckDto>>(data)
        {
            Pagination = new ApiPagination(count, count, request.SkipEntries, request.TakeEntries)
        }.ToApiResult().ToHttpResult();
    }

    [HttpGet("game-servers/{gameServerId:guid}/siege-sessions/{sessionId:guid}/actions")]
    [ProducesResponseType<ApiResponse<CollectionModel<SiegeActionDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActions(Guid gameServerId, Guid sessionId, int skipEntries = 0, int takeEntries = 20, CancellationToken cancellationToken = default)
    {
        if (!ValidPage(skipEntries, takeEntries))
        {
            return new ApiResult<CollectionModel<SiegeActionDto>>(HttpStatusCode.BadRequest).ToHttpResult();
        }
        var session = await context.SiegeSessions.AsNoTracking().SingleOrDefaultAsync(
            s => s.GameServerId == gameServerId && s.SessionId == sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return new ApiResult<CollectionModel<SiegeActionDto>>(HttpStatusCode.NotFound).ToHttpResult();
        }
        var query = context.SiegeActions.AsNoTracking().Where(a => a.SessionId == sessionId);
        var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query.OrderByDescending(a => a.ClaimedAtUtc).ThenBy(a => a.ActionId)
            .Skip(skipEntries).Take(takeEntries).ToListAsync(cancellationToken).ConfigureAwait(false);
        var data = new CollectionModel<SiegeActionDto>(items.Select(a => ActionDto(a, session)).ToList());
        return new ApiResponse<CollectionModel<SiegeActionDto>>(data)
        {
            Pagination = new ApiPagination(count, count, skipEntries, takeEntries)
        }.ToApiResult().ToHttpResult();
    }

    [HttpGet("game-servers/{gameServerId:guid}/siege-sessions/{sessionId:guid}/checks")]
    [ProducesResponseType<ApiResponse<CollectionModel<SiegeCheckDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChecks(Guid gameServerId, Guid sessionId, int skipEntries = 0, int takeEntries = 20, CancellationToken cancellationToken = default)
    {
        if (!ValidPage(skipEntries, takeEntries))
        {
            return new ApiResult<CollectionModel<SiegeCheckDto>>(HttpStatusCode.BadRequest).ToHttpResult();
        }
        var session = await context.SiegeSessions.AsNoTracking().SingleOrDefaultAsync(
            s => s.GameServerId == gameServerId && s.SessionId == sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return new ApiResult<CollectionModel<SiegeCheckDto>>(HttpStatusCode.NotFound).ToHttpResult();
        }
        var query = context.SiegeChecks.AsNoTracking().Where(c => c.SessionId == sessionId);
        var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query.OrderByDescending(c => c.OccurredAtUtc).ThenBy(c => c.CheckId)
            .Skip(skipEntries).Take(takeEntries).ToListAsync(cancellationToken).ConfigureAwait(false);
        var data = new CollectionModel<SiegeCheckDto>(items.Select(c => CheckDto(c, session)).ToList());
        return new ApiResponse<CollectionModel<SiegeCheckDto>>(data)
        {
            Pagination = new ApiPagination(count, count, skipEntries, takeEntries)
        }.ToApiResult().ToHttpResult();
    }

    [HttpGet("game-servers/{gameServerId:guid}/siege-sessions/{sessionId:guid}/events")]
    [ProducesResponseType<ApiResponse<CollectionModel<SiegeSessionEventDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionEvents(Guid gameServerId, Guid sessionId, int skipEntries = 0, int takeEntries = 20, CancellationToken cancellationToken = default)
    {
        if (!ValidPage(skipEntries, takeEntries))
        {
            return new ApiResult<CollectionModel<SiegeSessionEventDto>>(HttpStatusCode.BadRequest).ToHttpResult();
        }
        var session = await context.SiegeSessions.AsNoTracking().SingleOrDefaultAsync(
            s => s.GameServerId == gameServerId && s.SessionId == sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return new ApiResult<CollectionModel<SiegeSessionEventDto>>(HttpStatusCode.NotFound).ToHttpResult();
        }
        var query = context.SiegeSessionEvents.AsNoTracking().Where(e => e.SessionId == sessionId);
        var count = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var events = await query.OrderByDescending(e => e.OccurredAtUtc).ThenBy(e => e.OperationId)
            .Skip(skipEntries).Take(takeEntries).ToListAsync(cancellationToken).ConfigureAwait(false);
        var items = events.Select(e => new SiegeSessionEventDto
        {
            OperationId = e.OperationId,
            SessionId = e.SessionId,
            Mode = Enum.Parse<SiegeSessionMode>(session.Mode),
            EventType = e.EventType,
            Actor = e.Actor,
            OccurredAtUtc = DateTime.SpecifyKind(e.OccurredAtUtc, DateTimeKind.Utc),
            ExpiresAtUtc = DateTime.SpecifyKind(e.ExpiresAtUtc, DateTimeKind.Utc),
            ExtensionMinutes = e.ExtensionMinutes
        }).ToList();
        return new ApiResponse<CollectionModel<SiegeSessionEventDto>>(new CollectionModel<SiegeSessionEventDto>(items))
        {
            Pagination = new ApiPagination(count, count, skipEntries, takeEntries)
        }.ToApiResult().ToHttpResult();
    }

    private static bool ValidPage(int skip, int take) => skip >= 0 && take is > 0 and <= 100;

    private static bool ValidAuditQuery(SiegeAuditQueryDto? request) =>
        request is not null && request.GameServerIds is { Length: > 0 and <= 1000 } &&
        request.GameServerIds.All(id => id != Guid.Empty) && ValidPage(request.SkipEntries, request.TakeEntries) &&
        (request.FromUtc is null || request.ToUtc is null || request.FromUtc < request.ToUtc);
}
