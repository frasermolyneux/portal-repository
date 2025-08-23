using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    /// <summary>
    /// Controller for player analytics operations providing statistical data about players.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class PlayerAnalyticsController : ControllerBase, IPlayerAnalyticsApi
    {
        private readonly PortalDbContext context;

        /// <summary>
        /// Initializes a new instance of the PlayerAnalyticsController class.
        /// </summary>
        /// <param name="context">The database context for player data access.</param>
        public PlayerAnalyticsController(PortalDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves cumulative daily player statistics based on a cutoff date.
        /// </summary>
        /// <param name="cutoff">The cutoff date to filter players who first connected after this date.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A collection of cumulative daily player statistics.</returns>
        [HttpGet("player-analytics/cumulative-daily-players")]
        [ProducesResponseType<CollectionModel<PlayerAnalyticEntryDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCumulativeDailyPlayers([FromQuery] DateTime cutoff, CancellationToken cancellationToken = default)
        {
            var response = await ((IPlayerAnalyticsApi)this).GetCumulativeDailyPlayers(cutoff, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves cumulative daily player statistics based on a cutoff date.
        /// </summary>
        /// <param name="cutoff">The cutoff date to filter players who first connected after this date.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a collection of cumulative daily player statistics.</returns>
        async Task<ApiResult<CollectionModel<PlayerAnalyticEntryDto>>> IPlayerAnalyticsApi.GetCumulativeDailyPlayers(DateTime cutoff, CancellationToken cancellationToken)
        {
            var cumulative = await context.Players
                .AsNoTracking()
                .CountAsync(p => p.FirstSeen < cutoff, cancellationToken);

            var players = await context.Players
                .AsNoTracking()
                .Where(p => p.FirstSeen > cutoff)
                .Select(p => p.FirstSeen)
                .OrderBy(p => p)
                .ToListAsync(cancellationToken);

            var groupedPlayers = players.GroupBy(p => new DateTime(p.Year, p.Month, p.Day))
                .Select(g => new PlayerAnalyticEntryDto
                {
                    Created = g.Key,
                    Count = cumulative += g.Count()
                })
                .ToList();

            var result = new CollectionModel<PlayerAnalyticEntryDto>
            {
                TotalCount = groupedPlayers.Count,
                FilteredCount = groupedPlayers.Count,
                Items = groupedPlayers
            };

            return new ApiResponse<CollectionModel<PlayerAnalyticEntryDto>>(result)
            {
                Pagination = new ApiPagination(groupedPlayers.Count, groupedPlayers.Count, 0, groupedPlayers.Count)
            }.ToApiResult();
        }

        /// <summary>
        /// Retrieves new daily player statistics per game based on a cutoff date.
        /// </summary>
        /// <param name="cutoff">The cutoff date to filter players who first connected after this date.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A collection of new daily player statistics grouped by game type.</returns>
        [HttpGet("player-analytics/new-daily-players-per-game")]
        [ProducesResponseType<CollectionModel<PlayerAnalyticPerGameEntryDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetNewDailyPlayersPerGame([FromQuery] DateTime cutoff, CancellationToken cancellationToken = default)
        {
            var response = await ((IPlayerAnalyticsApi)this).GetNewDailyPlayersPerGame(cutoff, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves new daily player statistics per game based on a cutoff date.
        /// </summary>
        /// <param name="cutoff">The cutoff date to filter players who first connected after this date.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a collection of new daily player statistics grouped by game type.</returns>
        async Task<ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>> IPlayerAnalyticsApi.GetNewDailyPlayersPerGame(DateTime cutoff, CancellationToken cancellationToken)
        {
            var players = await context.Players
                .AsNoTracking()
                .Where(p => p.FirstSeen > cutoff)
                .Select(p => new { p.FirstSeen, p.GameType })
                .OrderBy(p => p.FirstSeen)
                .ToListAsync(cancellationToken);

            var groupedPlayers = players.GroupBy(p => new DateTime(p.FirstSeen.Year, p.FirstSeen.Month, p.FirstSeen.Day))
                .Select(g => new PlayerAnalyticPerGameEntryDto
                {
                    Created = g.Key,
                    GameCounts = g.GroupBy(i => i.GameType)
                        .Select(i => new { Type = i.Key, Count = i.Count() })
                        .ToDictionary(a => a.Type.ToGameType(), a => a.Count)
                }).ToList();

            var result = new CollectionModel<PlayerAnalyticPerGameEntryDto>
            {
                TotalCount = groupedPlayers.Count,
                FilteredCount = groupedPlayers.Count,
                Items = groupedPlayers
            };

            return new ApiResponse<CollectionModel<PlayerAnalyticPerGameEntryDto>>(result)
            {
                Pagination = new ApiPagination(groupedPlayers.Count, groupedPlayers.Count, 0, groupedPlayers.Count)
            }.ToApiResult();
        }

        /// <summary>
        /// Retrieves player drop-off statistics per game based on a cutoff date.
        /// </summary>
        /// <param name="cutoff">The cutoff date to filter players who were last seen after this date.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A collection of player drop-off statistics grouped by game type.</returns>
        [HttpGet("player-analytics/players-drop-off-per-game")]
        [ProducesResponseType<CollectionModel<PlayerAnalyticPerGameEntryDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPlayersDropOffPerGameJson([FromQuery] DateTime cutoff, CancellationToken cancellationToken = default)
        {
            var response = await ((IPlayerAnalyticsApi)this).GetPlayersDropOffPerGameJson(cutoff, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves player drop-off statistics per game based on a cutoff date.
        /// </summary>
        /// <param name="cutoff">The cutoff date to filter players who were last seen after this date.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a collection of player drop-off statistics grouped by game type.</returns>
        async Task<ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>> IPlayerAnalyticsApi.GetPlayersDropOffPerGameJson(DateTime cutoff, CancellationToken cancellationToken)
        {
            var players = await context.Players
                .AsNoTracking()
                .Where(p => p.LastSeen > cutoff)
                .Select(p => new { p.LastSeen, p.GameType })
                .OrderBy(p => p.LastSeen)
                .ToListAsync(cancellationToken);

            var groupedPlayers = players.GroupBy(p => new DateTime(p.LastSeen.Year, p.LastSeen.Month, p.LastSeen.Day))
                .Select(g => new PlayerAnalyticPerGameEntryDto
                {
                    Created = g.Key,
                    GameCounts = g.GroupBy(i => i.GameType)
                        .Select(i => new { Type = i.Key, Count = i.Count() })
                        .ToDictionary(a => a.Type.ToGameType(), a => a.Count)
                }).ToList();

            var result = new CollectionModel<PlayerAnalyticPerGameEntryDto>
            {
                TotalCount = groupedPlayers.Count,
                FilteredCount = groupedPlayers.Count,
                Items = groupedPlayers
            };

            return new ApiResponse<CollectionModel<PlayerAnalyticPerGameEntryDto>>(result)
            {
                Pagination = new ApiPagination(groupedPlayers.Count, groupedPlayers.Count, 0, groupedPlayers.Count)
            }.ToApiResult();
        }
    }
}
