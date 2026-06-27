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
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Servers;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.TableStorage;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}/analytics/servers")]
public class ServerAnalyticsController : ControllerBase, IServerAnalyticsApi
{
    private readonly PortalDbContext context;
    private readonly ILiveStatusStore liveStatusStore;

    public ServerAnalyticsController(PortalDbContext context, ILiveStatusStore liveStatusStore)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(liveStatusStore);
        this.context = context;
        this.liveStatusStore = liveStatusStore;
    }

    [HttpGet("{gameServerId:guid}/overview")]
    [ProducesResponseType<ServerOverviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOverview(
        Guid gameServerId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IServerAnalyticsApi)this).GetOverview(gameServerId, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ServerOverviewDto>> IServerAnalyticsApi.GetOverview(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<ServerOverviewDto>(HttpStatusCode.BadRequest);
        }

        var server = await context.GameServers
            .AsNoTracking()
            .FirstOrDefaultAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken).ConfigureAwait(false);

        if (server == null)
        {
            return new ApiResult<ServerOverviewDto>(HttpStatusCode.NotFound);
        }

        var stats = await context.GameServerStats
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

        var live = await liveStatusStore.GetServerLiveStatusAsync(gameServerId, cancellationToken).ConfigureAwait(false);

        var dto = new ServerOverviewDto
        {
            GameServerId = gameServerId,
            Title = !string.IsNullOrWhiteSpace(live?.Title) ? live.Title : server.Title,
            GameType = server.GameType.ToGameType(),
            Online = live?.IsOnline ?? false,
            CurrentPlayers = live?.CurrentPlayers ?? 0,
            AvgPlayers = stats.Count == 0 ? 0 : Math.Round(stats.Average(), 2),
            PeakPlayers = stats.Count == 0 ? 0 : stats.Max(),
            EventsCount = eventsCount,
            ChatCount = chatCount
        };

        return new ApiResponse<ServerOverviewDto>(dto).ToApiResult();
    }

    [HttpGet("{gameServerId:guid}/timeseries")]
    [ProducesResponseType<ServerTimeseriesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimeseries(
        Guid gameServerId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] AnalyticsBucket bucket,
        [FromQuery] AnalyticsCompareMode compareMode = AnalyticsCompareMode.None,
        [FromQuery] int comparePeriods = AnalyticsQueryDefaults.DefaultComparePeriods,
        [FromQuery] AnalyticsAlignMode alignMode = AnalyticsAlignMode.None,
        [FromQuery] string timezone = "UTC",
        [FromQuery] bool normalize = false,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IServerAnalyticsApi)this).GetTimeseries(
            gameServerId,
            fromUtc,
            toUtc,
            bucket,
            compareMode,
            comparePeriods,
            alignMode,
            timezone,
            normalize,
            cancellationToken).ConfigureAwait(false);

        return response.ToHttpResult();
    }

    async Task<ApiResult<ServerTimeseriesDto>> IServerAnalyticsApi.GetTimeseries(Guid gameServerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken)
    {
        return await ((IServerAnalyticsApi)this).GetTimeseries(
            gameServerId,
            fromUtc,
            toUtc,
            bucket,
            AnalyticsCompareMode.None,
            AnalyticsQueryDefaults.DefaultComparePeriods,
            AnalyticsAlignMode.None,
            "UTC",
            false,
            cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<ServerTimeseriesDto>> IServerAnalyticsApi.GetTimeseries(
        Guid gameServerId,
        DateTime fromUtc,
        DateTime toUtc,
        AnalyticsBucket bucket,
        AnalyticsCompareMode compareMode,
        int comparePeriods,
        AnalyticsAlignMode alignMode,
        string timezone,
        bool normalize,
        CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateBucketWindow(fromUtc, toUtc, bucket, out _)
            || !AnalyticsQueryValidator.TryValidateComparisonOptions(compareMode, comparePeriods, alignMode, timezone, out _)
            || !AnalyticsQueryValidator.TryGetAlignedWindow(fromUtc, toUtc, alignMode, timezone, out var alignedFromUtc, out var alignedToUtc, out _))
        {
            return new ApiResult<ServerTimeseriesDto>(HttpStatusCode.BadRequest);
        }

        var serverExists = await context.GameServers
            .AsNoTracking()
            .AnyAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken).ConfigureAwait(false);

        if (!serverExists)
        {
            return new ApiResult<ServerTimeseriesDto>(HttpStatusCode.NotFound);
        }

        var recentPlayers = await context.RecentPlayers
            .AsNoTracking()
            .Where(rp => rp.GameServerId == gameServerId && rp.Timestamp >= alignedFromUtc && rp.Timestamp < alignedToUtc)
            .Select(rp => new { rp.PlayerId, rp.Timestamp })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var events = await context.GameServerEvents
            .AsNoTracking()
            .Where(e => e.GameServerId == gameServerId && e.Timestamp >= alignedFromUtc && e.Timestamp < alignedToUtc)
            .Select(e => e.Timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var chat = await context.ChatMessages
            .AsNoTracking()
            .Where(c => c.GameServerId == gameServerId && c.Timestamp >= alignedFromUtc && c.Timestamp < alignedToUtc)
            .Select(c => c.Timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var uniquePlayersByBucket = recentPlayers
            .GroupBy(x => TruncateToBucket(x.Timestamp, bucket))
            .ToDictionary(g => g.Key, g => g.Select(x => x.PlayerId).Distinct().Count());

        var eventsByBucket = events.GroupBy(t => TruncateToBucket(t, bucket)).ToDictionary(g => g.Key, g => g.Count());
        var chatByBucket = chat.GroupBy(t => TruncateToBucket(t, bucket)).ToDictionary(g => g.Key, g => g.Count());

        var bucketStarts = BuildBuckets(alignedFromUtc, alignedToUtc, bucket);
        var points = bucketStarts.Select(start => new ServerTimeseriesPointDto
        {
            Timestamp = start,
            UniquePlayers = uniquePlayersByBucket.GetValueOrDefault(start, 0),
            EventsCount = eventsByBucket.GetValueOrDefault(start, 0),
            ChatMessagesCount = chatByBucket.GetValueOrDefault(start, 0)
        }).ToList();

        var labels = points.Select(p => p.Timestamp).ToList();
        var series = new List<AnalyticsSeriesDto>
        {
            BuildSeries("uniquePlayers", "Unique Players", labels, points.Select(p => (double)p.UniquePlayers)),
            BuildSeries("events", "Events", labels, points.Select(p => (double)p.EventsCount)),
            BuildSeries("chatMessages", "Chat Messages", labels, points.Select(p => (double)p.ChatMessagesCount))
        };

        var currentTotal = points.Sum(p => (double)p.UniquePlayers);
        var summary = compareMode == AnalyticsCompareMode.None
            ? null
            : BuildComparisonSummary(currentTotal, comparePeriods);

        var dto = new ServerTimeseriesDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Bucket = bucket,
            Points = points,
            Labels = labels,
            Series = series,
            Summary = summary,
            Meta = BuildCompareMeta(compareMode, comparePeriods, alignMode, timezone, normalize)
        };

        return new ApiResponse<ServerTimeseriesDto>(dto).ToApiResult();
    }

    [HttpGet("{gameServerId:guid}/summary")]
    [ProducesResponseType<ServerSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary(
        Guid gameServerId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IServerAnalyticsApi)this).GetSummary(gameServerId, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ServerSummaryDto>> IServerAnalyticsApi.GetSummary(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<ServerSummaryDto>(HttpStatusCode.BadRequest);
        }

        var serverExists = await context.GameServers
            .AsNoTracking()
            .AnyAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken).ConfigureAwait(false);

        if (!serverExists)
        {
            return new ApiResult<ServerSummaryDto>(HttpStatusCode.NotFound);
        }

        var recentPlayersCount = await context.RecentPlayers
            .AsNoTracking()
            .CountAsync(rp => rp.GameServerId == gameServerId && rp.Timestamp >= fromUtc && rp.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var reportsCount = await context.Reports
            .AsNoTracking()
            .CountAsync(r => r.GameServerId == gameServerId && r.Timestamp >= fromUtc && r.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var correlationWindow = TimeSpan.FromHours(6);

        var adminActionsCount = await context.AdminActions
            .AsNoTracking()
            .CountAsync(a =>
                a.Created >= fromUtc
                && a.Created < toUtc
                && context.RecentPlayers.Any(rp =>
                    rp.PlayerId == a.PlayerId
                    && rp.GameServerId == gameServerId
                    && rp.Timestamp >= a.Created - correlationWindow
                    && rp.Timestamp <= a.Created + correlationWindow),
                cancellationToken).ConfigureAwait(false);

        var dto = new ServerSummaryDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            RecentPlayersCount = recentPlayersCount,
            ReportsCount = reportsCount,
            AdminActionsCount = adminActionsCount
        };

        return new ApiResponse<ServerSummaryDto>(dto).ToApiResult();
    }

    private static AnalyticsCompareMetaDto BuildCompareMeta(
        AnalyticsCompareMode compareMode,
        int comparePeriods,
        AnalyticsAlignMode alignMode,
        string timezone,
        bool normalize)
    {
        return new AnalyticsCompareMetaDto
        {
            CompareMode = compareMode,
            ComparePeriods = comparePeriods,
            AlignMode = alignMode,
            Timezone = timezone,
            Normalize = normalize
        };
    }

    private static AnalyticsCompareSummaryDto BuildComparisonSummary(double currentTotal, int comparePeriods)
    {
        var baseline = comparePeriods > 0 ? currentTotal / Math.Max(comparePeriods, 1) : currentTotal;
        var delta = currentTotal - baseline;
        var deltaPercent = baseline == 0 ? 0 : (delta / baseline) * 100d;

        return new AnalyticsCompareSummaryDto
        {
            CurrentTotal = Math.Round(currentTotal, 2),
            BaselineTotal = Math.Round(baseline, 2),
            Delta = Math.Round(delta, 2),
            DeltaPercent = Math.Round(deltaPercent, 2)
        };
    }

    private static AnalyticsSeriesDto BuildSeries(string key, string label, IEnumerable<DateTime> bucketStarts, IEnumerable<double> values)
    {
        var starts = bucketStarts.ToList();
        var vals = values.ToList();

        return new AnalyticsSeriesDto
        {
            Key = key,
            Label = label,
            Values = starts.Zip(vals, (start, value) => new AnalyticsSeriesValueDto
            {
                BucketStartUtc = start,
                Value = Math.Round(value, 2)
            }).ToList()
        };
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