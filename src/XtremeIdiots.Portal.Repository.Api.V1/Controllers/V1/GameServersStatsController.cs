
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
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class GameServersStatsController : ControllerBase, IGameServersStatsApi
    {
        private readonly PortalDbContext context;


        public GameServersStatsController(
            PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        /// <summary>
        /// Creates new game server statistics entries.
        /// </summary>
        /// <param name="createGameServerStatDtos">The game server statistics data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A success response indicating the game server statistics were created.</returns>
        [HttpPost("game-servers-stats")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateGameServerStats([FromBody] List<CreateGameServerStatDto> createGameServerStatDtos, CancellationToken cancellationToken = default)
        {
            if (createGameServerStatDtos == null || !createGameServerStatDtos.Any())
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();

            var response = await ((IGameServersStatsApi)this).CreateGameServerStats(createGameServerStatDtos, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates new game server statistics entries.
        /// </summary>
        /// <param name="createGameServerStatDtos">The game server statistics data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the game server statistics were created.</returns>
        async Task<ApiResult> IGameServersStatsApi.CreateGameServerStats(List<CreateGameServerStatDto> createGameServerStatDtos, CancellationToken cancellationToken)
        {
            List<GameServerStat> gameServerStats = [];

            foreach (var createGameServerStatDto in createGameServerStatDtos)
            {
                var lastStat = await context.GameServerStats
                    .AsNoTracking()
                    .Where(gss => gss.GameServerId == createGameServerStatDto.GameServerId)
                    .OrderBy(gss => gss.Timestamp)
                    .LastOrDefaultAsync(cancellationToken).ConfigureAwait(false);

                if (lastStat == null || lastStat.PlayerCount != createGameServerStatDto.PlayerCount || lastStat.MapName != createGameServerStatDto.MapName)
                {
                    var gameServerStat = createGameServerStatDto.ToEntity();
                    gameServerStat.Timestamp = DateTime.UtcNow;

                    gameServerStats.Add(gameServerStat);
                }
            }

            await context.GameServerStats.AddRangeAsync(gameServerStats, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Retrieves game server statistics for a specific game server.
        /// </summary>
        /// <param name="gameServerId">The unique identifier of the game server.</param>
        /// <param name="cutoff">The cutoff date for statistics retrieval (optional, defaults to 2 days ago).</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A collection of game server statistics for the specified period.</returns>
        [HttpGet("game-servers-stats/{gameServerId:guid}")]
        [ProducesResponseType<CollectionModel<GameServerStatDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGameServerStatusStats(Guid gameServerId, [FromQuery] DateTime? cutoff = null, CancellationToken cancellationToken = default)
        {
            cutoff ??= DateTime.UtcNow.AddDays(-2);

            if (cutoff.Value < DateTime.UtcNow.AddDays(-2))
                cutoff = DateTime.UtcNow.AddDays(-2);

            var response = await ((IGameServersStatsApi)this).GetGameServerStatusStats(gameServerId, cutoff.Value, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves game server statistics for a specific game server.
        /// </summary>
        /// <param name="gameServerId">The unique identifier of the game server.</param>
        /// <param name="cutoff">The cutoff date for statistics retrieval.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a collection of game server statistics for the specified period.</returns>
        async Task<ApiResult<CollectionModel<GameServerStatDto>>> IGameServersStatsApi.GetGameServerStatusStats(Guid gameServerId, DateTime cutoff, CancellationToken cancellationToken)
        {
            var gameServerStats = await context.GameServerStats
                .AsNoTracking()
                .Where(gss => gss.GameServerId == gameServerId && gss.Timestamp >= cutoff)
                .OrderBy(gss => gss.Timestamp)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = gameServerStats.Select(r => r.ToDto()).ToList();
            var result = new CollectionModel<GameServerStatDto>(entries);

            return new ApiResponse<CollectionModel<GameServerStatDto>>(result)
            {
                Pagination = new ApiPagination(gameServerStats.Count, gameServerStats.Count, 0, gameServerStats.Count)
            }.ToApiResult();
        }
    }
}

