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
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}/analytics/dashboard")]
public class DashboardAnalyticsController : ControllerBase, IDashboardAnalyticsApi
{
    private readonly PortalDbContext context;

    public DashboardAnalyticsController(PortalDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.context = context;
    }

    [HttpGet("summary")]
    [ProducesResponseType<DashboardSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IDashboardAnalyticsApi)this).GetSummary(fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<DashboardSummaryDto>> IDashboardAnalyticsApi.GetSummary(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<DashboardSummaryDto>(HttpStatusCode.BadRequest);
        }

        var activeGamesCount = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.GameServer != null && !s.GameServer.Deleted)
            .Select(s => s.GameServer!.GameType)
            .Distinct()
            .CountAsync(cancellationToken).ConfigureAwait(false);

        var activeServersCount = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.GameServerId != null && s.GameServer != null && !s.GameServer.Deleted)
            .Select(s => s.GameServerId!.Value)
            .Distinct()
            .CountAsync(cancellationToken).ConfigureAwait(false);

        var uniquePlayersCount = await context.RecentPlayers
            .AsNoTracking()
            .Where(rp => rp.Timestamp >= fromUtc && rp.Timestamp < toUtc && rp.PlayerId != null)
            .Select(rp => rp.PlayerId!.Value)
            .Distinct()
            .CountAsync(cancellationToken).ConfigureAwait(false);

        var reportsCount = await context.Reports
            .AsNoTracking()
            .CountAsync(r => r.Timestamp >= fromUtc && r.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var dto = new DashboardSummaryDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            ActiveGamesCount = activeGamesCount,
            ActiveServersCount = activeServersCount,
            UniquePlayersCount = uniquePlayersCount,
            ReportsCount = reportsCount
        };

        return new ApiResponse<DashboardSummaryDto>(dto).ToApiResult();
    }

    [HttpGet("trends")]
    [ProducesResponseType<DashboardTrendsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTrends(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] AnalyticsBucket bucket,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IDashboardAnalyticsApi)this).GetTrends(fromUtc, toUtc, bucket, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<DashboardTrendsDto>> IDashboardAnalyticsApi.GetTrends(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateBucketWindow(fromUtc, toUtc, bucket, out _))
        {
            return new ApiResult<DashboardTrendsDto>(HttpStatusCode.BadRequest);
        }

        var recentPlayers = await context.RecentPlayers
            .AsNoTracking()
            .Where(rp => rp.Timestamp >= fromUtc && rp.Timestamp < toUtc)
            .Select(rp => rp.Timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var playersByBucket = recentPlayers
            .GroupBy(t => TruncateToBucket(t, bucket))
            .ToDictionary(g => g.Key, g => g.Count());

        var bucketStarts = BuildBuckets(fromUtc, toUtc, bucket);
        var points = bucketStarts.Select(start => new AnalyticsTimeseriesPointDto
        {
            BucketStartUtc = start,
            BucketEndUtc = AddBucket(start, bucket),
            Value = playersByBucket.GetValueOrDefault(start, 0)
        }).ToList();

        var dto = new DashboardTrendsDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Bucket = bucket,
            Points = points
        };

        return new ApiResponse<DashboardTrendsDto>(dto).ToApiResult();
    }

    [HttpGet("composition")]
    [ProducesResponseType<DashboardCompositionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetComposition(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IDashboardAnalyticsApi)this).GetComposition(fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<DashboardCompositionDto>> IDashboardAnalyticsApi.GetComposition(DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<DashboardCompositionDto>(HttpStatusCode.BadRequest);
        }

        var topGamesRaw = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.GameServer != null && !s.GameServer.Deleted)
            .GroupBy(s => s.GameServer!.GameType)
            .Select(g => new { GameType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var topServersRaw = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.GameServerId != null && s.GameServer != null && !s.GameServer.Deleted)
            .GroupBy(s => new { Id = s.GameServerId!.Value, s.GameServer!.Title })
            .Select(g => new { g.Key.Id, g.Key.Title, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var topMapsRaw = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.MapName != null)
            .GroupBy(s => s.MapName)
            .Select(g => new { MapName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var totalGames = topGamesRaw.Sum(x => x.Count);
        var totalServers = topServersRaw.Sum(x => x.Count);
        var totalMaps = topMapsRaw.Sum(x => x.Count);

        var dto = new DashboardCompositionDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            TopGames = topGamesRaw.Select(x => new AnalyticsTopItemDto
            {
                Key = x.GameType.ToString(),
                Label = x.GameType.ToString(),
                Count = x.Count,
                Percentage = totalGames == 0 ? null : Math.Round((double)x.Count * 100d / totalGames, 2)
            }).ToList(),
            TopServers = topServersRaw.Select(x => new AnalyticsTopItemDto
            {
                Key = x.Id.ToString(),
                Label = x.Title,
                Count = x.Count,
                Percentage = totalServers == 0 ? null : Math.Round((double)x.Count * 100d / totalServers, 2)
            }).ToList(),
            TopMaps = topMapsRaw.Select(x => new AnalyticsTopItemDto
            {
                Key = x.MapName,
                Label = x.MapName,
                Count = x.Count,
                Percentage = totalMaps == 0 ? null : Math.Round((double)x.Count * 100d / totalMaps, 2)
            }).ToList()
        };

        return new ApiResponse<DashboardCompositionDto>(dto).ToApiResult();
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
