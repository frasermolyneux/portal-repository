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
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class PlayerAnalyticsController : Controller, IPlayerAnalyticsApi
    {
        private readonly PortalDbContext context;

        public PlayerAnalyticsController(PortalDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        [Route("player-analytics/cumulative-daily-players")]
        public async Task<IActionResult> GetCumulativeDailyPlayers(DateTime cutoff, CancellationToken cancellationToken = default)
        {
            var response = await ((IPlayerAnalyticsApi)this).GetCumulativeDailyPlayers(cutoff, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<PlayerAnalyticEntryDto>>> IPlayerAnalyticsApi.GetCumulativeDailyPlayers(DateTime cutoff, CancellationToken cancellationToken)
        {
            var cumulative = await context.Players.CountAsync(p => p.FirstSeen < cutoff, cancellationToken);

            var players = await context.Players
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

            return new ApiResult<CollectionModel<PlayerAnalyticEntryDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PlayerAnalyticEntryDto>>(result));
        }

        [HttpGet]
        [Route("player-analytics/new-daily-players-per-game")]
        public async Task<IActionResult> GetNewDailyPlayersPerGame(DateTime cutoff, CancellationToken cancellationToken = default)
        {
            var response = await ((IPlayerAnalyticsApi)this).GetNewDailyPlayersPerGame(cutoff, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>> IPlayerAnalyticsApi.GetNewDailyPlayersPerGame(DateTime cutoff, CancellationToken cancellationToken)
        {
            var players = await context.Players
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

            return new ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PlayerAnalyticPerGameEntryDto>>(result));
        }

        [HttpGet]
        [Route("player-analytics/players-drop-off-per-game")]
        public async Task<IActionResult> GetPlayersDropOffPerGameJson(DateTime cutoff, CancellationToken cancellationToken = default)
        {
            var response = await ((IPlayerAnalyticsApi)this).GetPlayersDropOffPerGameJson(cutoff, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>> IPlayerAnalyticsApi.GetPlayersDropOffPerGameJson(DateTime cutoff, CancellationToken cancellationToken)
        {
            var players = await context.Players
                .Where(p => p.LastSeen > cutoff)
                .Select(p => new { p.LastSeen, p.GameType })
                .OrderBy(p => p.LastSeen)
                .ToListAsync(cancellationToken);

            var groupedPlayers = players.GroupBy(p => new DateTime(p.LastSeen.Year, p.LastSeen.Month, p.LastSeen.Day))
                .Select(g => new PlayerAnalyticPerGameEntryDto
                {
                    Created = g.Key,
                    GameCounts = g.GroupBy(i => i.GameType.ToString())
                        .Select(i => new { Type = i.Key, Count = i.Count() })
                        .ToDictionary(a => a.Type.ToGameType(), a => a.Count)
                }).ToList();

            var result = new CollectionModel<PlayerAnalyticPerGameEntryDto>
            {
                TotalCount = groupedPlayers.Count,
                FilteredCount = groupedPlayers.Count,
                Items = groupedPlayers
            };

            return new ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PlayerAnalyticPerGameEntryDto>>(result));
        }
    }
}
