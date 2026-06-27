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

    [HttpGet("{mapId:guid}/overview")]
    [ProducesResponseType<MapOverviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOverview(
        Guid mapId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IMapAnalyticsApi)this).GetOverview(mapId, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<MapOverviewDto>> IMapAnalyticsApi.GetOverview(Guid mapId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<MapOverviewDto>(HttpStatusCode.BadRequest);
        }

        var map = await context.Maps
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MapId == mapId, cancellationToken).ConfigureAwait(false);

        if (map == null)
        {
            return new ApiResult<MapOverviewDto>(HttpStatusCode.NotFound);
        }

        var votesCount = await context.MapVotes
            .AsNoTracking()
            .CountAsync(v => v.MapId == mapId && v.Timestamp >= fromUtc && v.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var playsCount = await context.GameServerStats
            .AsNoTracking()
            .CountAsync(s =>
                s.MapName == map.MapName
                && s.Timestamp >= fromUtc
                && s.Timestamp < toUtc
                && s.GameServer != null
                && !s.GameServer.Deleted
                && s.GameServer.GameType == map.GameType,
                cancellationToken).ConfigureAwait(false);

        var votesForTargetMap = await context.MapVotes
            .AsNoTracking()
            .Where(v => v.MapId == mapId && v.Timestamp >= fromUtc && v.Timestamp < toUtc)
            .Select(v => new { v.GameServerId })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var voteCountsByServerAndMap = await context.MapVotes
            .AsNoTracking()
            .Where(v => v.Timestamp >= fromUtc && v.Timestamp < toUtc && v.GameServerId != null)
            .GroupBy(v => new { v.GameServerId, v.MapId })
            .Select(g => new { g.Key.GameServerId, g.Key.MapId, Count = g.Count() })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var targetVotesByServer = votesForTargetMap
            .Where(v => v.GameServerId != null)
            .GroupBy(v => v.GameServerId)
            .ToDictionary(g => g.Key!.Value, g => g.Count());

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

        var dto = new MapOverviewDto
        {
            MapId = mapId,
            MapName = map.MapName ?? "Unknown",
            VotesCount = votesCount,
            PlaysCount = playsCount,
            AveragePosition = Math.Round(averagePosition, 2)
        };

        return new ApiResponse<MapOverviewDto>(dto).ToApiResult();
    }

    [HttpGet("{mapId:guid}/trends")]
    [ProducesResponseType<MapTrendsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrends(
        Guid mapId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] AnalyticsBucket bucket,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IMapAnalyticsApi)this).GetTrends(mapId, fromUtc, toUtc, bucket, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<MapTrendsDto>> IMapAnalyticsApi.GetTrends(Guid mapId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateBucketWindow(fromUtc, toUtc, bucket, out _))
        {
            return new ApiResult<MapTrendsDto>(HttpStatusCode.BadRequest);
        }

        var map = await context.Maps
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MapId == mapId, cancellationToken).ConfigureAwait(false);

        if (map == null)
        {
            return new ApiResult<MapTrendsDto>(HttpStatusCode.NotFound);
        }

        var plays = await context.GameServerStats
            .AsNoTracking()
            .Where(s =>
                s.MapName == map.MapName
                && s.Timestamp >= fromUtc
                && s.Timestamp < toUtc
                && s.GameServer != null
                && !s.GameServer.Deleted
                && s.GameServer.GameType == map.GameType)
            .Select(s => s.Timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var playsByBucket = plays
            .GroupBy(t => TruncateToBucket(t, bucket))
            .ToDictionary(g => g.Key, g => g.Count());

        var bucketStarts = BuildBuckets(fromUtc, toUtc, bucket);
        var points = bucketStarts.Select(start => new AnalyticsTimeseriesPointDto
        {
            BucketStartUtc = start,
            BucketEndUtc = AddBucket(start, bucket),
            Value = playsByBucket.GetValueOrDefault(start, 0)
        }).ToList();

        var dto = new MapTrendsDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Bucket = bucket,
            Points = points
        };

        return new ApiResponse<MapTrendsDto>(dto).ToApiResult();
    }

    [HttpGet("rankings")]
    [ProducesResponseType<MapRankingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRankings(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IMapAnalyticsApi)this).GetRankings(fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<MapRankingsDto>> IMapAnalyticsApi.GetRankings(DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<MapRankingsDto>(HttpStatusCode.BadRequest);
        }

        var topMapsRaw = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.MapName != null)
            .GroupBy(s => s.MapName)
            .Select(g => new { MapName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var total = topMapsRaw.Sum(x => x.Count);

        var dto = new MapRankingsDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            TopMaps = topMapsRaw.Select(x => new AnalyticsTopItemDto
            {
                Key = x.MapName,
                Label = x.MapName,
                Count = x.Count,
                Percentage = total == 0 ? null : Math.Round((double)x.Count * 100d / total, 2)
            }).ToList()
        };

        return new ApiResponse<MapRankingsDto>(dto).ToApiResult();
    }

    private static AnalyticsTimeWindowDto CreateWindow(DateTime fromUtc, DateTime toUtc)
    {
        return new AnalyticsTimeWindowDto
        {
            FromUtc = fromUtc,
            ToUtc = toUtc
        };
    }

    private static List<DateTime> BuildBuckets(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket)
    {
        var result = new List<DateTime>();
        for (var cursor = TruncateToBucket(fromUtc, bucket); cursor < toUtc; cursor = AddBucket(cursor, bucket))
        {
            result.Add(cursor);
        }

        return result;
    }

    private static DateTime TruncateToBucket(DateTime value, AnalyticsBucket bucket)
    {
        return bucket switch
        {
            AnalyticsBucket.FifteenMinutes => new DateTime(value.Year, value.Month, value.Day, value.Hour, (value.Minute / 15) * 15, 0, DateTimeKind.Utc),
            AnalyticsBucket.OneHour => new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0, DateTimeKind.Utc),
            _ => value.Date
        };
    }

    private static DateTime AddBucket(DateTime value, AnalyticsBucket bucket)
    {
        return bucket switch
        {
            AnalyticsBucket.FifteenMinutes => value.AddMinutes(15),
            AnalyticsBucket.OneHour => value.AddHours(1),
            _ => value.AddDays(1)
        };
    }
}
