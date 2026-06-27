using System.Net;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}/analytics/maps")]
public class MapAnalyticsController : ControllerBase, IMapAnalyticsApi
{
    private readonly PortalDbContext context;

    public MapAnalyticsController(PortalDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.context = context;
    }

    [HttpGet("overview")]
    [ProducesResponseType<MapsOverviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOverview([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var response = await ((IMapAnalyticsApi)this).GetOverview(fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<MapsOverviewDto>> IMapAnalyticsApi.GetOverview(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<MapsOverviewDto>(HttpStatusCode.BadRequest);
        }

        var playStats = context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.MapName != null && s.MapName != "");

        var totalPlays = await playStats.CountAsync(cancellationToken).ConfigureAwait(false);
        var totalMaps = await playStats.Select(s => s.MapName).Distinct().CountAsync(cancellationToken).ConfigureAwait(false);
        var totalVotes = await context.MapVotes
            .AsNoTracking()
            .CountAsync(v => v.Timestamp >= fromUtc && v.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var dto = new MapsOverviewDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            TotalMaps = totalMaps,
            TotalPlays = totalPlays,
            TotalVotes = totalVotes
        };

        return new ApiResponse<MapsOverviewDto>(dto).ToApiResult();
    }

    [HttpGet("hotspots")]
    [ProducesResponseType<MapsHotspotsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHotspots([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var response = await ((IMapAnalyticsApi)this).GetHotspots(fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<MapsHotspotsDto>> IMapAnalyticsApi.GetHotspots(DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<MapsHotspotsDto>(HttpStatusCode.BadRequest);
        }

        var grouped = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.MapName != null && s.MapName != "" && s.GameServer != null && !s.GameServer.Deleted)
            .GroupBy(s => new { s.MapName, s.GameServer!.GameType })
            .Select(g => new
            {
                g.Key.MapName,
                g.Key.GameType,
                Avg = g.Average(x => (double)x.PlayerCount),
                Peak = g.Max(x => x.PlayerCount),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Avg)
            .Take(top)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var items = grouped.Select(x => new MapHotspotItemDto
        {
            MapName = x.MapName,
            GameType = x.GameType.ToGameType(),
            AvgPlayers = Math.Round(x.Avg, 2),
            PeakPlayers = x.Peak,
            SampleCount = x.Count
        }).ToList();

        var dto = new MapsHotspotsDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<MapsHotspotsDto>(dto).ToApiResult();
    }

    [HttpGet("top-played")]
    [ProducesResponseType<MapsTopPlayedDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTopPlayed([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var response = await ((IMapAnalyticsApi)this).GetTopPlayed(fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<MapsTopPlayedDto>> IMapAnalyticsApi.GetTopPlayed(DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<MapsTopPlayedDto>(HttpStatusCode.BadRequest);
        }

        var basePlays = context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.MapName != null && s.MapName != "" && s.GameServer != null && !s.GameServer.Deleted);

        var totalPlays = await basePlays.CountAsync(cancellationToken).ConfigureAwait(false);

        var grouped = await basePlays
            .GroupBy(s => new { s.MapName, s.GameServer!.GameType })
            .Select(g => new { g.Key.MapName, g.Key.GameType, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var items = grouped.Select(x => new MapTopPlayedItemDto
        {
            MapName = x.MapName,
            GameType = x.GameType.ToGameType(),
            PlaysCount = x.Count,
            SharePercent = totalPlays == 0 ? 0 : Math.Round((double)x.Count / totalPlays * 100d, 2)
        }).ToList();

        var dto = new MapsTopPlayedDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<MapsTopPlayedDto>(dto).ToApiResult();
    }

    [HttpGet("top-voted")]
    [ProducesResponseType<MapsTopVotedDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTopVoted([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var response = await ((IMapAnalyticsApi)this).GetTopVoted(fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<MapsTopVotedDto>> IMapAnalyticsApi.GetTopVoted(DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<MapsTopVotedDto>(HttpStatusCode.BadRequest);
        }

        var grouped = await context.MapVotes
            .AsNoTracking()
            .Where(v => v.Timestamp >= fromUtc && v.Timestamp < toUtc)
            .GroupBy(v => v.MapId)
            .Select(g => new { MapId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var mapIds = grouped.Select(x => x.MapId).ToList();
        var maps = await context.Maps
            .AsNoTracking()
            .Where(m => mapIds.Contains(m.MapId))
            .Select(m => new { m.MapId, m.MapName, m.GameType })
            .ToDictionaryAsync(m => m.MapId, cancellationToken).ConfigureAwait(false);

        var items = grouped.Select(x =>
        {
            maps.TryGetValue(x.MapId, out var m);
            return new MapTopVotedItemDto
            {
                MapId = x.MapId,
                MapName = string.IsNullOrWhiteSpace(m?.MapName) ? "Unknown" : m.MapName,
                GameType = (m?.GameType ?? 0).ToGameType(),
                VotesCount = x.Count
            };
        }).ToList();

        var dto = new MapsTopVotedDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<MapsTopVotedDto>(dto).ToApiResult();
    }

    [HttpGet("by-game")]
    [ProducesResponseType<MapsByGameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByGame([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var response = await ((IMapAnalyticsApi)this).GetByGame(fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<MapsByGameDto>> IMapAnalyticsApi.GetByGame(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<MapsByGameDto>(HttpStatusCode.BadRequest);
        }

        var basePlays = context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.MapName != null && s.MapName != "" && s.GameServer != null && !s.GameServer.Deleted);

        // Two translatable queries instead of COUNT + COUNT(DISTINCT) in a single grouped projection
        // (which EF Core does not reliably translate).
        var playsByGameType = await basePlays
            .GroupBy(s => s.GameServer!.GameType)
            .Select(g => new { GameType = g.Key, Plays = g.Count() })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var mapsByGameType = await basePlays
            .Select(s => new { GameType = s.GameServer!.GameType, s.MapName })
            .Distinct()
            .GroupBy(x => x.GameType)
            .Select(g => new { GameType = g.Key, Maps = g.Count() })
            .ToDictionaryAsync(x => x.GameType, x => x.Maps, cancellationToken).ConfigureAwait(false);

        var votesByGame = await context.MapVotes
            .AsNoTracking()
            .Where(v => v.Timestamp >= fromUtc && v.Timestamp < toUtc)
            .Join(context.Maps.AsNoTracking(), v => v.MapId, m => m.MapId, (v, m) => m.GameType)
            .GroupBy(gt => gt)
            .Select(g => new { GameType = g.Key, Votes = g.Count() })
            .ToDictionaryAsync(x => x.GameType, x => x.Votes, cancellationToken).ConfigureAwait(false);

        var items = playsByGameType
            .OrderByDescending(x => x.Plays)
            .Select(x => new MapsByGameItemDto
            {
                GameType = x.GameType.ToGameType(),
                MapsPlayed = mapsByGameType.GetValueOrDefault(x.GameType, 0),
                TotalPlays = x.Plays,
                TotalVotes = votesByGame.GetValueOrDefault(x.GameType, 0)
            })
            .ToList();

        var dto = new MapsByGameDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<MapsByGameDto>(dto).ToApiResult();
    }

    [HttpGet("by-server/{gameServerId:guid}")]
    [ProducesResponseType<MapsByServerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByServer(Guid gameServerId, [FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var response = await ((IMapAnalyticsApi)this).GetByServer(gameServerId, fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<MapsByServerDto>> IMapAnalyticsApi.GetByServer(Guid gameServerId, DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<MapsByServerDto>(HttpStatusCode.BadRequest);
        }

        var serverExists = await context.GameServers
            .AsNoTracking()
            .AnyAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken).ConfigureAwait(false);

        if (!serverExists)
        {
            return new ApiResult<MapsByServerDto>(HttpStatusCode.NotFound);
        }

        var samples = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.GameServerId == gameServerId && s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.MapName != null && s.MapName != "")
            .Select(s => new { s.MapName, s.PlayerCount })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var totalSamples = samples.Count;
        var items = samples
            .GroupBy(s => s.MapName)
            .Select(g => new MapsByServerItemDto
            {
                MapName = g.Key,
                PlaysCount = g.Count(),
                AvgPlayers = Math.Round(g.Average(x => (double)x.PlayerCount), 2),
                SharePercent = totalSamples == 0 ? 0 : Math.Round((double)g.Count() / totalSamples * 100d, 2)
            })
            .OrderByDescending(x => x.PlaysCount)
            .Take(top)
            .ToList();

        var dto = new MapsByServerDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            GameServerId = gameServerId,
            Items = items
        };

        return new ApiResponse<MapsByServerDto>(dto).ToApiResult();
    }

    [HttpGet("{mapId:guid}")]
    [ProducesResponseType<MapDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMapDetail(Guid mapId, [FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var response = await ((IMapAnalyticsApi)this).GetMapDetail(mapId, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<MapDetailDto>> IMapAnalyticsApi.GetMapDetail(Guid mapId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<MapDetailDto>(HttpStatusCode.BadRequest);
        }

        var map = await context.Maps
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MapId == mapId, cancellationToken).ConfigureAwait(false);

        if (map == null)
        {
            return new ApiResult<MapDetailDto>(HttpStatusCode.NotFound);
        }

        var votesCount = await context.MapVotes
            .AsNoTracking()
            .CountAsync(v => v.MapId == mapId && v.Timestamp >= fromUtc && v.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var playerSamples = await context.GameServerStats
            .AsNoTracking()
            .Where(s =>
                s.MapName == map.MapName
                && s.Timestamp >= fromUtc
                && s.Timestamp < toUtc
                && s.GameServer != null
                && !s.GameServer.Deleted
                && s.GameServer.GameType == map.GameType)
            .Select(s => s.PlayerCount)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var votesForTargetMap = await context.MapVotes
            .AsNoTracking()
            .Where(v => v.MapId == mapId && v.Timestamp >= fromUtc && v.Timestamp < toUtc && v.GameServerId != null)
            .Select(v => v.GameServerId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var voteCountsByServerAndMap = await context.MapVotes
            .AsNoTracking()
            .Where(v => v.Timestamp >= fromUtc && v.Timestamp < toUtc && v.GameServerId != null)
            .GroupBy(v => new { v.GameServerId, v.MapId })
            .Select(g => new { g.Key.GameServerId, g.Key.MapId, Count = g.Count() })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var targetVotesByServer = votesForTargetMap
            .Where(id => id != null)
            .GroupBy(id => id!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var averagePosition = targetVotesByServer
            .Select(server =>
            {
                var mapsForServer = voteCountsByServerAndMap
                    .Where(x => x.GameServerId == server.Key)
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.MapId)
                    .ToList();

                var rank = mapsForServer.FindIndex(x => x.MapId == mapId);
                return rank < 0 ? 0d : rank + 1d;
            })
            .DefaultIfEmpty(0d)
            .Average();

        var dto = new MapDetailDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            MapId = mapId,
            MapName = map.MapName ?? "Unknown",
            GameType = map.GameType.ToGameType(),
            VotesCount = votesCount,
            PlaysCount = playerSamples.Count,
            AveragePosition = Math.Round(averagePosition, 2),
            AvgPlayers = playerSamples.Count == 0 ? 0 : Math.Round(playerSamples.Average(), 2),
            PeakPlayers = playerSamples.Count == 0 ? 0 : playerSamples.Max()
        };

        return new ApiResponse<MapDetailDto>(dto).ToApiResult();
    }

    private static AnalyticsTimeWindowDto CreateWindow(DateTime fromUtc, DateTime toUtc)
    {
        return new AnalyticsTimeWindowDto
        {
            FromUtc = fromUtc,
            ToUtc = toUtc
        };
    }
}
