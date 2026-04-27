using System.Net;
using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    /// <summary>
    /// API controller for managing ban file monitors. Monitors are now status snapshots
    /// owned by portal-server-agent — admins cannot create, edit, or delete them
    /// directly; the agent upserts via <see cref="UpsertBanFileMonitorStatusByGameServerId"/>.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("v{version:apiVersion}")]
    public class BanFileMonitorsController : ControllerBase, IBanFileMonitorsApi
    {
        private readonly PortalDbContext context;

        public BanFileMonitorsController(PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        [HttpGet("ban-file-monitors/{banFileMonitorId:guid}")]
        [ProducesResponseType<BanFileMonitorDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default)
        {
            var response = await ((IBanFileMonitorsApi)this).GetBanFileMonitor(banFileMonitorId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        async Task<ApiResult<BanFileMonitorDto>> IBanFileMonitorsApi.GetBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken)
        {
            var banFileMonitor = await context.BanFileMonitors
                .Include(bfm => bfm.GameServer)
                .Where(bfm => !bfm.GameServer.Deleted)
                .AsNoTracking()
                .FirstOrDefaultAsync(bfm => bfm.BanFileMonitorId == banFileMonitorId, cancellationToken).ConfigureAwait(false);

            if (banFileMonitor == null)
                return new ApiResult<BanFileMonitorDto>(HttpStatusCode.NotFound);

            var result = banFileMonitor.ToDto();
            return new ApiResponse<BanFileMonitorDto>(result).ToApiResult();
        }

        [HttpGet("ban-file-monitors")]
        [ProducesResponseType<CollectionModel<BanFileMonitorDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBanFileMonitors(
            [FromQuery] string? gameTypes = null,
            [FromQuery] string? banFileMonitorIds = null,
            [FromQuery] Guid? gameServerId = null,
            [FromQuery] int skipEntries = 0,
            [FromQuery] int takeEntries = 20,
            [FromQuery] BanFileMonitorOrder? order = null,
            CancellationToken cancellationToken = default)
        {
            GameType[]? gameTypesFilter = null;
            if (!string.IsNullOrWhiteSpace(gameTypes))
            {
                var split = gameTypes.Split(",");
                gameTypesFilter = split.Select(gt => Enum.Parse<GameType>(gt)).ToArray();
            }

            Guid[]? banFileMonitorsIdFilter = null;
            if (!string.IsNullOrWhiteSpace(banFileMonitorIds))
            {
                var split = banFileMonitorIds.Split(",");
                banFileMonitorsIdFilter = split.Select(id => Guid.Parse(id)).ToArray();
            }

            var response = await ((IBanFileMonitorsApi)this).GetBanFileMonitors(gameTypesFilter, banFileMonitorsIdFilter, gameServerId, skipEntries, takeEntries, order, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<BanFileMonitorDto>>> IBanFileMonitorsApi.GetBanFileMonitors(GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId, int skipEntries, int takeEntries, BanFileMonitorOrder? order, CancellationToken cancellationToken)
        {
            var baseQuery = context.BanFileMonitors
                .Include(bfm => bfm.GameServer)
                .Where(bfm => !bfm.GameServer.Deleted)
                .AsNoTracking()
                .AsQueryable();

            var filteredQuery = ApplyFilters(baseQuery, gameTypes, banFileMonitorIds, gameServerId);

            var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            var orderedQuery = ApplyOrderingAndPagination(filteredQuery, skipEntries, takeEntries, order);
            var results = await orderedQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = results.Select(bfm => bfm.ToDto()).ToList();

            var data = new CollectionModel<BanFileMonitorDto>(entries);

            return new ApiResponse<CollectionModel<BanFileMonitorDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        /// <summary>
        /// Upserts the ban file monitor status snapshot for a game server. Creates a
        /// row when none exists, updates otherwise. Called by portal-server-agent
        /// after each check cycle. This is the only supported way to write monitor status.
        /// </summary>
        [HttpPut("ban-file-monitors/by-game-server/{gameServerId:guid}/status")]
        [ProducesResponseType<BanFileMonitorDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<BanFileMonitorDto>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpsertBanFileMonitorStatusByGameServerId(
            Guid gameServerId,
            [FromBody] UpsertBanFileMonitorStatusDto upsertDto,
            CancellationToken cancellationToken = default)
        {
            if (upsertDto is null)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))).ToHttpResult();

            if (upsertDto.GameServerId != gameServerId)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.EntityIdMismatch, "GameServerId in the URL must match GameServerId in the request body"))).ToHttpResult();

            var response = await ((IBanFileMonitorsApi)this).UpsertBanFileMonitorStatus(upsertDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        async Task<ApiResult<BanFileMonitorDto>> IBanFileMonitorsApi.UpsertBanFileMonitorStatus(UpsertBanFileMonitorStatusDto upsertDto, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(upsertDto);

            var gameServerExists = await context.GameServers
                .AsNoTracking()
                .AnyAsync(gs => gs.GameServerId == upsertDto.GameServerId && !gs.Deleted, cancellationToken)
                .ConfigureAwait(false);

            if (!gameServerExists)
                return new ApiResult<BanFileMonitorDto>(HttpStatusCode.NotFound);

            // Pre-cleanup data may have multiple rows per game server. Order by LastCheckUtc
            // and keep the most-recently-touched one as the canonical row going forward.
            var existing = await context.BanFileMonitors
                .Where(bfm => bfm.GameServerId == upsertDto.GameServerId)
                .OrderByDescending(bfm => bfm.LastCheckUtc ?? DateTime.MinValue)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var created = false;
            if (existing is null)
            {
                existing = new BanFileMonitor
                {
                    GameServerId = upsertDto.GameServerId
                };
                context.BanFileMonitors.Add(existing);
                created = true;
            }

            upsertDto.ApplyStatus(existing);

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var saved = await context.BanFileMonitors
                .Include(bfm => bfm.GameServer)
                .AsNoTracking()
                .FirstAsync(bfm => bfm.BanFileMonitorId == existing.BanFileMonitorId, cancellationToken)
                .ConfigureAwait(false);

            return new ApiResponse<BanFileMonitorDto>(saved.ToDto()).ToApiResult(created ? HttpStatusCode.Created : HttpStatusCode.OK);
        }

        private static IQueryable<BanFileMonitor> ApplyFilters(IQueryable<BanFileMonitor> query, GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId)
        {
            if (gameTypes is { Length: > 0 })
            {
                var gameTypeInts = gameTypes.Select(gt => gt.ToGameTypeInt()).ToArray();
                query = query.Where(bfm => gameTypeInts.Contains(bfm.GameServer.GameType));
            }

            if (banFileMonitorIds is { Length: > 0 })
                query = query.Where(bfm => banFileMonitorIds.Contains(bfm.BanFileMonitorId));

            if (gameServerId.HasValue)
                query = query.Where(bfm => bfm.GameServerId == gameServerId.Value);

            return query;
        }

        private static IQueryable<BanFileMonitor> ApplyOrderingAndPagination(IQueryable<BanFileMonitor> query, int skipEntries, int takeEntries, BanFileMonitorOrder? order)
        {
            var orderedQuery = order switch
            {
                BanFileMonitorOrder.ServerListPosition => query.OrderBy(bfm => bfm.GameServer.ServerListPosition),
                BanFileMonitorOrder.GameType => query.OrderBy(bfm => bfm.GameServer.GameType),
                _ => query.OrderBy(bfm => bfm.BanFileMonitorId)
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }
    }
}
