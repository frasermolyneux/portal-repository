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
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RecentPlayers;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class RecentPlayersController : ControllerBase, IRecentPlayersApi
    {
        private readonly PortalDbContext context;

        public RecentPlayersController(PortalDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves a paginated list of recent players with optional filtering and sorting.
        /// </summary>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="gameServerId">Optional filter by game server identifier.</param>
        /// <param name="cutoff">Optional filter by timestamp cutoff (limited to last 48 hours).</param>
        /// <param name="filter">Optional filter criteria for recent players.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A paginated collection of recent players.</returns>
        [HttpGet("recent-players")]
        [ProducesResponseType<CollectionModel<RecentPlayerDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecentPlayers(
            [FromQuery] GameType? gameType = null,
            [FromQuery] Guid? gameServerId = null,
            [FromQuery] DateTime? cutoff = null,
            [FromQuery] RecentPlayersFilter? filter = null,
            [FromQuery] int? skipEntries = null,
            [FromQuery] int? takeEntries = null,
            [FromQuery] RecentPlayersOrder? order = null,
            CancellationToken cancellationToken = default)
        {
            var skip = skipEntries ?? 0;
            var take = takeEntries ?? 20;

            if (cutoff.HasValue && cutoff.Value < DateTime.UtcNow.AddHours(-48))
                cutoff = DateTime.UtcNow.AddHours(-48);

            var response = await ((IRecentPlayersApi)this).GetRecentPlayers(gameType, gameServerId, cutoff, filter, skip, take, order, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of recent players with optional filtering and sorting.
        /// </summary>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="gameServerId">Optional filter by game server identifier.</param>
        /// <param name="cutoff">Optional filter by timestamp cutoff.</param>
        /// <param name="filter">Optional filter criteria for recent players.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of recent players.</returns>
        async Task<ApiResult<CollectionModel<RecentPlayerDto>>> IRecentPlayersApi.GetRecentPlayers(GameType? gameType, Guid? gameServerId, DateTime? cutoff, RecentPlayersFilter? filter, int skipEntries, int takeEntries, RecentPlayersOrder? order, CancellationToken cancellationToken)
        {
            var baseQuery = context.RecentPlayers
                .Include(rp => rp.Player)
                .AsNoTracking()
                .AsQueryable();

            // Calculate total count before applying filters
            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Apply filters
            var filteredQuery = ApplyFilter(baseQuery, gameType, gameServerId, cutoff, filter);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken);

            // Apply ordering and pagination
            var orderedQuery = ApplyOrderAndLimits(filteredQuery, skipEntries, takeEntries, order);
            var results = await orderedQuery.ToListAsync(cancellationToken);

            var entries = results.Select(rp => rp.ToDto()).ToList();

            var data = new CollectionModel<RecentPlayerDto>(entries);

            return new ApiResponse<CollectionModel<RecentPlayerDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        /// <summary>
        /// Creates or updates recent player records in bulk.
        /// </summary>
        /// <param name="createRecentPlayerDtos">List of recent player data to create or update.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response indicating the recent players were processed.</returns>
        [HttpPost("recent-players")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRecentPlayers([FromBody] List<CreateRecentPlayerDto> createRecentPlayerDtos, CancellationToken cancellationToken = default)
        {
            if (createRecentPlayerDtos == null || !createRecentPlayerDtos.Any())
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();

            var response = await ((IRecentPlayersApi)this).CreateRecentPlayers(createRecentPlayerDtos, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates or updates recent player records in bulk.
        /// </summary>
        /// <param name="createRecentPlayerDtos">List of recent player data to create or update.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the recent players were processed.</returns>
        async Task<ApiResult> IRecentPlayersApi.CreateRecentPlayers(List<CreateRecentPlayerDto> createRecentPlayerDtos, CancellationToken cancellationToken)
        {
            if (createRecentPlayerDtos == null || !createRecentPlayerDtos.Any())
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage)));

            var playerIds = createRecentPlayerDtos.Select(dto => dto.PlayerId).ToList();

            // Fetch all existing recent players in one query for better performance
            var existingPlayers = await context.RecentPlayers
                .Where(rp => rp.PlayerId.HasValue && playerIds.Contains(rp.PlayerId.Value))
                .ToListAsync(cancellationToken);

            var existingPlayerDict = existingPlayers.ToDictionary(rp => rp.PlayerId!.Value);

            foreach (var createRecentPlayerDto in createRecentPlayerDtos)
            {
                if (existingPlayerDict.TryGetValue(createRecentPlayerDto.PlayerId, out var recentPlayer))
                {
                    // Update existing player
                    createRecentPlayerDto.ApplyTo(recentPlayer);
                    recentPlayer.Timestamp = DateTime.UtcNow;
                }
                else
                {
                    // Create new player
                    recentPlayer = createRecentPlayerDto.ToEntity();
                    recentPlayer.Timestamp = DateTime.UtcNow;
                    context.RecentPlayers.Add(recentPlayer);
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult();
        }

        private static IQueryable<RecentPlayer> ApplyFilter(IQueryable<RecentPlayer> query, GameType? gameType, Guid? gameServerId, DateTime? cutoff, RecentPlayersFilter? filter)
        {
            if (gameType.HasValue)
                query = query.Where(rp => rp.GameType == gameType.Value.ToGameTypeInt());

            if (gameServerId.HasValue)
                query = query.Where(rp => rp.GameServerId == gameServerId);

            if (cutoff.HasValue)
                query = query.Where(rp => rp.Timestamp > cutoff);

            if (filter.HasValue)
            {
                query = filter.Value switch
                {
                    RecentPlayersFilter.GeoLocated => query.Where(rp => rp.Lat != 0 && rp.Long != 0),
                    _ => query
                };
            }

            return query;
        }

        private static IQueryable<RecentPlayer> ApplyOrderAndLimits(IQueryable<RecentPlayer> query, int skipEntries, int takeEntries, RecentPlayersOrder? order)
        {
            // Apply ordering
            var orderedQuery = order switch
            {
                RecentPlayersOrder.TimestampAsc => query.OrderBy(rp => rp.Timestamp),
                RecentPlayersOrder.TimestampDesc => query.OrderByDescending(rp => rp.Timestamp),
                _ => query.OrderByDescending(rp => rp.Timestamp)
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }
    }
}

