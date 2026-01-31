using System.Net;
using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    /// <summary>
    /// Controller for managing live players on game servers.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class LivePlayersController : ControllerBase, ILivePlayersApi
    {
        private readonly PortalDbContext context;

        /// <summary>
        /// Initializes a new instance of the LivePlayersController.
        /// </summary>
        /// <param name="context">The database context for portal operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        public LivePlayersController(PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        /// <summary>
        /// Retrieves a paginated list of live players with optional filtering and sorting.
        /// </summary>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="gameServerId">Optional filter by game server identifier.</param>
        /// <param name="filter">Optional filter criteria for live players.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A paginated collection of live players.</returns>
        [HttpGet("live-players")]
        [ProducesResponseType<CollectionModel<LivePlayerDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLivePlayers(
            [FromQuery] GameType? gameType = null,
            [FromQuery] Guid? gameServerId = null,
            [FromQuery] LivePlayerFilter? filter = null,
            [FromQuery] int skipEntries = 0,
            [FromQuery] int takeEntries = 20,
            [FromQuery] LivePlayersOrder? order = null,
            CancellationToken cancellationToken = default)
        {
            var response = await ((ILivePlayersApi)this).GetLivePlayers(gameType, gameServerId, filter, skipEntries, takeEntries, order, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of live players with optional filtering and sorting.
        /// </summary>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="gameServerId">Optional filter by game server identifier.</param>
        /// <param name="filter">Optional filter criteria for live players.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of live players.</returns>
        async Task<ApiResult<CollectionModel<LivePlayerDto>>> ILivePlayersApi.GetLivePlayers(GameType? gameType, Guid? gameServerId, LivePlayerFilter? filter, int skipEntries, int takeEntries, LivePlayersOrder? order, CancellationToken cancellationToken)
        {
            var baseQuery = context.LivePlayers
                .Include(lp => lp.Player)
                .AsNoTracking()
                .AsQueryable();

            // Calculate total count before applying filters
            var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            // Apply filters
            var filteredQuery = ApplyFilter(baseQuery, gameType, gameServerId, filter);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            // Apply ordering and pagination
            var orderedQuery = ApplyOrderAndLimits(filteredQuery, skipEntries, takeEntries, order);
            var livePlayers = await orderedQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = livePlayers.Select(lp => lp.ToDto()).ToList();

            var data = new CollectionModel<LivePlayerDto>(entries);

            return new ApiResponse<CollectionModel<LivePlayerDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        /// <summary>
        /// Sets the live players for a specific game server, replacing any existing data.
        /// </summary>
        /// <param name="gameServerId">The unique identifier of the game server.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A success response if the operation completed successfully.</returns>
        [HttpPost("live-players/{gameServerId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetLivePlayersForGameServer(Guid gameServerId, CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync().ConfigureAwait(false);

            List<CreateLivePlayerDto>? createLivePlayerDtos;
            try
            {
                createLivePlayerDtos = JsonConvert.DeserializeObject<List<CreateLivePlayerDto>>(requestBody);
            }
            catch
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.InvalidRequestBody, ApiErrorMessages.InvalidRequestBodyMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();
            }

            if (createLivePlayerDtos == null)
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();
            }

            var response = await ((ILivePlayersApi)this).SetLivePlayersForGameServer(gameServerId, createLivePlayerDtos, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Sets the live players for a specific game server, replacing any existing data.
        /// </summary>
        /// <param name="gameServerId">The unique identifier of the game server.</param>
        /// <param name="createLivePlayerDtos">The list of live players to set for the game server.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the operation was successful.</returns>
        async Task<ApiResult> ILivePlayersApi.SetLivePlayersForGameServer(Guid gameServerId, List<CreateLivePlayerDto> createLivePlayerDtos, CancellationToken cancellationToken)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[LivePlayers] WHERE [GameServerId] = {gameServerId}", cancellationToken).ConfigureAwait(false);

            var livePlayers = createLivePlayerDtos.Select(lp => lp.ToEntity()).ToList();

            await context.LivePlayers.AddRangeAsync(livePlayers, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Applies filtering criteria to the live players query.
        /// </summary>
        /// <param name="query">The base query to apply filters to.</param>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="gameServerId">Optional filter by game server identifier.</param>
        /// <param name="filter">Optional filter criteria for live players.</param>
        /// <returns>The filtered query.</returns>
        private IQueryable<LivePlayer> ApplyFilter(IQueryable<LivePlayer> query, GameType? gameType, Guid? gameServerId, LivePlayerFilter? filter)
        {
            if (gameType.HasValue)
                query = query.Where(lp => lp.GameType == gameType.Value.ToGameTypeInt());

            if (gameServerId.HasValue)
                query = query.Where(lp => lp.GameServerId == gameServerId);

            if (filter.HasValue)
            {
                query = filter.Value switch
                {
                    LivePlayerFilter.GeoLocated => query.Where(lp => lp.Lat != null && lp.Long != null),
                    _ => query
                };
            }

            return query;
        }

        /// <summary>
        /// Applies ordering and pagination to the live players query.
        /// </summary>
        /// <param name="query">The query to apply ordering and pagination to.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <returns>The ordered and paginated query.</returns>
        private IQueryable<LivePlayer> ApplyOrderAndLimits(IQueryable<LivePlayer> query, int skipEntries, int takeEntries, LivePlayersOrder? order)
        {
            // Apply ordering
            var orderedQuery = order switch
            {
                LivePlayersOrder.ScoreAsc => query.OrderBy(lp => lp.Score),
                LivePlayersOrder.ScoreDesc => query.OrderByDescending(lp => lp.Score),
                _ => query.OrderBy(lp => lp.PlayerId) // Default ordering for consistency
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }
    }
}

