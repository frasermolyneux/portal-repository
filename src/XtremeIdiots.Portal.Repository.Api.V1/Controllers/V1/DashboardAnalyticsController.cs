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
using XtremeIdiots.Portal.Repository.Api.V1.Analytics;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.TableStorage;
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
    private readonly ILiveStatusStore liveStatusStore;

    public DashboardAnalyticsController(PortalDbContext context, ILiveStatusStore liveStatusStore)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(liveStatusStore);
        this.context = context;
        this.liveStatusStore = liveStatusStore;
    }

    [HttpGet("home")]
    [ProducesResponseType<DashboardHomeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHome(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] AnalyticsBucket bucket,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IDashboardAnalyticsApi)this).GetHome(fromUtc, toUtc, bucket, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<DashboardHomeDto>> IDashboardAnalyticsApi.GetHome(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateBucketWindow(fromUtc, toUtc, bucket, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<DashboardHomeDto>(HttpStatusCode.BadRequest);
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

        var recentPlayers = await context.RecentPlayers
            .AsNoTracking()
            .Where(rp => rp.Timestamp >= fromUtc && rp.Timestamp < toUtc)
            .Select(rp => rp.Timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var playersByBucket = recentPlayers
            .GroupBy(t => AnalyticsTimeBucketing.Truncate(t, bucket))
            .ToDictionary(g => g.Key, g => g.Count());

        var trendPoints = AnalyticsTimeBucketing.BuildBuckets(fromUtc, toUtc, bucket)
            .Select(start => new AnalyticsTimeseriesPointDto
            {
                BucketStartUtc = start,
                BucketEndUtc = AnalyticsTimeBucketing.Add(start, bucket),
                Value = playersByBucket.GetValueOrDefault(start, 0)
            })
            .ToList();

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
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.MapName != null && s.MapName != "")
            .GroupBy(s => s.MapName)
            .Select(g => new { MapName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var totalGames = topGamesRaw.Sum(x => x.Count);
        var totalServers = topServersRaw.Sum(x => x.Count);
        var totalMaps = topMapsRaw.Sum(x => x.Count);

        var composition = new DashboardCompositionDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            TopGames = topGamesRaw.Select(x => new AnalyticsTopItemDto
            {
                Key = x.GameType.ToString(),
                Label = x.GameType.ToGameType().ToString(),
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

        var dto = new DashboardHomeDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Summary = new DashboardSummaryDto
            {
                Window = CreateWindow(fromUtc, toUtc),
                ActiveGamesCount = activeGamesCount,
                ActiveServersCount = activeServersCount,
                UniquePlayersCount = uniquePlayersCount,
                ReportsCount = reportsCount
            },
            Bucket = bucket,
            TrendPoints = trendPoints,
            Composition = composition
        };

        return new ApiResponse<DashboardHomeDto>(dto).ToApiResult();
    }

    [HttpGet("server/{gameServerId:guid}")]
    [ProducesResponseType<DashboardServerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServer(
        Guid gameServerId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] AnalyticsBucket bucket,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IDashboardAnalyticsApi)this).GetServer(gameServerId, fromUtc, toUtc, bucket, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<DashboardServerDto>> IDashboardAnalyticsApi.GetServer(Guid gameServerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateBucketWindow(fromUtc, toUtc, bucket, out _))
        {
            return new ApiResult<DashboardServerDto>(HttpStatusCode.BadRequest);
        }

        var server = await context.GameServers
            .AsNoTracking()
            .FirstOrDefaultAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken).ConfigureAwait(false);

        if (server == null)
        {
            return new ApiResult<DashboardServerDto>(HttpStatusCode.NotFound);
        }

        var playerCounts = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.GameServerId == gameServerId && s.Timestamp >= fromUtc && s.Timestamp < toUtc)
            .Select(s => s.PlayerCount)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var eventsCount = await context.GameServerEvents
            .AsNoTracking()
            .CountAsync(e => e.GameServerId == gameServerId && e.Timestamp >= fromUtc && e.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var chatCount = await context.ChatMessages
            .AsNoTracking()
            .CountAsync(c => c.GameServerId == gameServerId && c.Timestamp >= fromUtc && c.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var recentPlayers = await context.RecentPlayers
            .AsNoTracking()
            .Where(rp => rp.GameServerId == gameServerId && rp.Timestamp >= fromUtc && rp.Timestamp < toUtc)
            .Select(rp => new { rp.PlayerId, rp.Timestamp })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var uniquePlayers = recentPlayers.Select(x => x.PlayerId).Where(id => id != null).Distinct().Count();

        var playersByBucket = recentPlayers
            .GroupBy(x => AnalyticsTimeBucketing.Truncate(x.Timestamp, bucket))
            .ToDictionary(g => g.Key, g => g.Select(x => x.PlayerId).Where(id => id != null).Distinct().Count());

        var trendPoints = AnalyticsTimeBucketing.BuildBuckets(fromUtc, toUtc, bucket)
            .Select(start => new AnalyticsTimeseriesPointDto
            {
                BucketStartUtc = start,
                BucketEndUtc = AnalyticsTimeBucketing.Add(start, bucket),
                Value = playersByBucket.GetValueOrDefault(start, 0)
            })
            .ToList();

        var live = await liveStatusStore.GetServerLiveStatusAsync(gameServerId, cancellationToken).ConfigureAwait(false);

        var dto = new DashboardServerDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            GameServerId = gameServerId,
            Title = !string.IsNullOrWhiteSpace(live?.Title) ? live.Title : server.Title,
            GameType = server.GameType.ToGameType(),
            Online = live?.IsOnline ?? false,
            CurrentPlayers = live?.CurrentPlayers ?? 0,
            MaxPlayers = live?.MaxPlayers ?? 0,
            MapName = live?.Map,
            AvgPlayers = playerCounts.Count == 0 ? 0 : Math.Round(playerCounts.Average(), 2),
            PeakPlayers = playerCounts.Count == 0 ? 0 : playerCounts.Max(),
            EventsCount = eventsCount,
            ChatCount = chatCount,
            UniquePlayers = uniquePlayers,
            Bucket = bucket,
            TrendPoints = trendPoints
        };

        return new ApiResponse<DashboardServerDto>(dto).ToApiResult();
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
