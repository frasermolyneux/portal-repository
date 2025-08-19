using System.Net;
using Asp.Versioning;
using AutoMapper;

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

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    /// <summary>
    /// API controller for managing ban file monitors.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class BanFileMonitorsController : ControllerBase, IBanFileMonitorsApi
    {
        private readonly PortalDbContext context;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="BanFileMonitorsController"/> class.
        /// </summary>
        /// <param name="context">The database context for data access.</param>
        /// <param name="mapper">The AutoMapper instance for object mapping.</param>
        /// <exception cref="ArgumentNullException">Thrown when context or mapper is null.</exception>
        public BanFileMonitorsController(
            PortalDbContext context,
            IMapper mapper)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Retrieves a specific ban file monitor by its unique identifier.
        /// </summary>
        /// <param name="banFileMonitorId">The unique identifier of the ban file monitor to retrieve.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The requested ban file monitor if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("ban-file-monitors/{banFileMonitorId:guid}")]
        [ProducesResponseType<BanFileMonitorDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default)
        {
            var response = await ((IBanFileMonitorsApi)this).GetBanFileMonitor(banFileMonitorId, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a specific ban file monitor by its unique identifier.
        /// </summary>
        /// <param name="banFileMonitorId">The unique identifier of the ban file monitor to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the ban file monitor details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<BanFileMonitorDto>> IBanFileMonitorsApi.GetBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken)
        {
            var banFileMonitor = await context.BanFileMonitors
                .Include(bfm => bfm.GameServer)
                .Where(bfm => !bfm.GameServer.Deleted)
                .AsNoTracking()
                .FirstOrDefaultAsync(bfm => bfm.BanFileMonitorId == banFileMonitorId, cancellationToken);

            if (banFileMonitor == null)
                return new ApiResult<BanFileMonitorDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<BanFileMonitorDto>(banFileMonitor);
            return new ApiResponse<BanFileMonitorDto>(result).ToApiResult();
        }

        /// <summary>
        /// Retrieves a paginated collection of ban file monitors with optional filtering and sorting.
        /// </summary>
        /// <param name="gameTypes">Comma-separated list of game types to filter by.</param>
        /// <param name="banFileMonitorIds">Comma-separated list of ban file monitor IDs to filter by.</param>
        /// <param name="gameServerId">The game server ID to filter by.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="order">The order to sort the results by.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A paginated collection of ban file monitors.</returns>
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

            var response = await ((IBanFileMonitorsApi)this).GetBanFileMonitors(gameTypesFilter, banFileMonitorsIdFilter, gameServerId, skipEntries, takeEntries, order, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated collection of ban file monitors with optional filtering and sorting.
        /// </summary>
        /// <param name="gameTypes">Optional filter by game types.</param>
        /// <param name="banFileMonitorIds">Optional filter by ban file monitor identifiers.</param>
        /// <param name="gameServerId">Optional filter by game server identifier.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of ban file monitors.</returns>
        async Task<ApiResult<CollectionModel<BanFileMonitorDto>>> IBanFileMonitorsApi.GetBanFileMonitors(GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId, int skipEntries, int takeEntries, BanFileMonitorOrder? order, CancellationToken cancellationToken)
        {
            var baseQuery = context.BanFileMonitors
                .Include(bfm => bfm.GameServer)
                .Where(bfm => !bfm.GameServer.Deleted)
                .AsNoTracking()
                .AsQueryable();

            // Apply filters first
            var filteredQuery = ApplyFilters(baseQuery, gameTypes, banFileMonitorIds, gameServerId);

            // Calculate counts after filtering but before pagination
            var totalCount = await baseQuery.CountAsync(cancellationToken);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken);

            // Apply ordering and pagination
            var orderedQuery = ApplyOrderingAndPagination(filteredQuery, skipEntries, takeEntries, order);
            var results = await orderedQuery.ToListAsync(cancellationToken);

            var entries = results.Select(bfm => mapper.Map<BanFileMonitorDto>(bfm)).ToList();

            var data = new CollectionModel<BanFileMonitorDto>
            {
                Items = entries,
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };

            return new ApiResponse<CollectionModel<BanFileMonitorDto>>(data).ToApiResult();
        }

        /// <summary>
        /// Creates a new ban file monitor.
        /// </summary>
        /// <param name="createBanFileMonitorDto">The ban file monitor data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The result of the creation operation.</returns>
        [HttpPost("ban-file-monitors")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateBanFileMonitor([FromBody] CreateBanFileMonitorDto createBanFileMonitorDto, CancellationToken cancellationToken = default)
        {
            var response = await ((IBanFileMonitorsApi)this).CreateBanFileMonitor(createBanFileMonitorDto, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates a new ban file monitor.
        /// </summary>
        /// <param name="createBanFileMonitorDto">The ban file monitor creation data.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the success or failure of the creation operation.</returns>
        async Task<ApiResult> IBanFileMonitorsApi.CreateBanFileMonitor(CreateBanFileMonitorDto createBanFileMonitorDto, CancellationToken cancellationToken)
        {
            var banFileMonitor = mapper.Map<BanFileMonitor>(createBanFileMonitorDto);
            banFileMonitor.LastSync = DateTime.UtcNow.AddHours(-4);

            context.BanFileMonitors.Add(banFileMonitor);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Updates an existing ban file monitor.
        /// </summary>
        /// <param name="banFileMonitorId">The unique identifier of the ban file monitor to update.</param>
        /// <param name="editBanFileMonitorDto">The ban file monitor update data.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The result of the update operation.</returns>
        [HttpPut("ban-file-monitors/{banFileMonitorId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateBanFileMonitor(Guid banFileMonitorId, [FromBody] EditBanFileMonitorDto editBanFileMonitorDto, CancellationToken cancellationToken = default)
        {
            if (editBanFileMonitorDto.BanFileMonitorId != banFileMonitorId)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.EntityIdMismatch, ApiErrorMessages.BanFileMonitorIdMismatchMessage))).ToHttpResult();

            var response = await ((IBanFileMonitorsApi)this).UpdateBanFileMonitor(editBanFileMonitorDto, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates an existing ban file monitor.
        /// </summary>
        /// <param name="editBanFileMonitorDto">The ban file monitor update data.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the success or failure of the update operation.</returns>
        async Task<ApiResult> IBanFileMonitorsApi.UpdateBanFileMonitor(EditBanFileMonitorDto editBanFileMonitorDto, CancellationToken cancellationToken)
        {
            var banFileMonitor = await context.BanFileMonitors
                .FirstOrDefaultAsync(bfm => bfm.BanFileMonitorId == editBanFileMonitorDto.BanFileMonitorId, cancellationToken);

            if (banFileMonitor == null)
                return new ApiResult(HttpStatusCode.NotFound);

            mapper.Map(editBanFileMonitorDto, banFileMonitor);

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Deletes a ban file monitor.
        /// </summary>
        /// <param name="banFileMonitorId">The unique identifier of the ban file monitor to delete.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The result of the deletion operation.</returns>
        [HttpDelete("ban-file-monitors/{banFileMonitorId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default)
        {
            var response = await ((IBanFileMonitorsApi)this).DeleteBanFileMonitor(banFileMonitorId, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Deletes a ban file monitor.
        /// </summary>
        /// <param name="banFileMonitorId">The unique identifier of the ban file monitor to delete.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the success or failure of the deletion operation.</returns>
        async Task<ApiResult> IBanFileMonitorsApi.DeleteBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken)
        {
            var banFileMonitor = await context.BanFileMonitors
                .FirstOrDefaultAsync(bfm => bfm.BanFileMonitorId == banFileMonitorId, cancellationToken);

            if (banFileMonitor == null)
                return new ApiResult(HttpStatusCode.NotFound);

            context.BanFileMonitors.Remove(banFileMonitor);

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Applies filtering criteria to the ban file monitors query.
        /// </summary>
        /// <param name="query">The base query to apply filters to.</param>
        /// <param name="gameTypes">Optional filter by game types.</param>
        /// <param name="banFileMonitorIds">Optional filter by ban file monitor identifiers.</param>
        /// <param name="gameServerId">Optional filter by game server identifier.</param>
        /// <returns>The filtered query.</returns>
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

        /// <summary>
        /// Applies ordering and pagination to the ban file monitors query.
        /// </summary>
        /// <param name="query">The query to apply ordering and pagination to.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria.</param>
        /// <returns>The ordered and paginated query.</returns>
        private static IQueryable<BanFileMonitor> ApplyOrderingAndPagination(IQueryable<BanFileMonitor> query, int skipEntries, int takeEntries, BanFileMonitorOrder? order)
        {
            // Apply ordering using modern switch expression
            var orderedQuery = order switch
            {
                BanFileMonitorOrder.BannerServerListPosition => query.OrderBy(bfm => bfm.GameServer.ServerListPosition),
                BanFileMonitorOrder.GameType => query.OrderBy(bfm => bfm.GameServer.GameType),
                _ => query.OrderBy(bfm => bfm.BanFileMonitorId)
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }
    }
}

