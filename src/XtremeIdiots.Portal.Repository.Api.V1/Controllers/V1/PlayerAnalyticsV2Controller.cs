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
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}/analytics/players")]
public class PlayerAnalyticsV2Controller : ControllerBase, IPlayerAnalyticsV2Api
{
    private readonly PortalDbContext context;

    public PlayerAnalyticsV2Controller(PortalDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.context = context;
    }

    [HttpGet("{playerId:guid}/overview")]
    [ProducesResponseType<PlayerOverviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOverview(
        Guid playerId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayerAnalyticsV2Api)this).GetOverview(playerId, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlayerOverviewDto>> IPlayerAnalyticsV2Api.GetOverview(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<PlayerOverviewDto>(HttpStatusCode.BadRequest);
        }

        var player = await context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerId == playerId, cancellationToken).ConfigureAwait(false);

        if (player == null)
        {
            return new ApiResult<PlayerOverviewDto>(HttpStatusCode.NotFound);
        }

        var sessionsCount = await context.RecentPlayers
            .AsNoTracking()
            .CountAsync(rp => rp.PlayerId == playerId && rp.Timestamp >= fromUtc && rp.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var actionCount = await context.AdminActions
            .AsNoTracking()
            .CountAsync(a => a.PlayerId == playerId && a.Created >= fromUtc && a.Created < toUtc, cancellationToken).ConfigureAwait(false);

        // Approximate playtime from observed recent-player session snapshots.
        var playTimeMinutes = sessionsCount;

        var dto = new PlayerOverviewDto
        {
            PlayerId = playerId,
            Username = string.IsNullOrWhiteSpace(player.Username) ? "Unknown" : player.Username,
            SessionsCount = sessionsCount,
            TotalPlayTimeMinutes = playTimeMinutes,
            AdminActionsCount = actionCount
        };

        return new ApiResponse<PlayerOverviewDto>(dto).ToApiResult();
    }

    [HttpGet("{playerId:guid}/trends")]
    [ProducesResponseType<PlayerTrendsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrends(
        Guid playerId,
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
        var response = await ((IPlayerAnalyticsV2Api)this).GetTrends(
            playerId,
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

    async Task<ApiResult<PlayerTrendsDto>> IPlayerAnalyticsV2Api.GetTrends(Guid playerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken)
    {
        return await ((IPlayerAnalyticsV2Api)this).GetTrends(
            playerId,
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

    async Task<ApiResult<PlayerTrendsDto>> IPlayerAnalyticsV2Api.GetTrends(
        Guid playerId,
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
            return new ApiResult<PlayerTrendsDto>(HttpStatusCode.BadRequest);
        }

        var playerExists = await context.Players
            .AsNoTracking()
            .AnyAsync(p => p.PlayerId == playerId, cancellationToken).ConfigureAwait(false);

        if (!playerExists)
        {
            return new ApiResult<PlayerTrendsDto>(HttpStatusCode.NotFound);
        }

        var sessions = await context.RecentPlayers
            .AsNoTracking()
            .Where(rp => rp.PlayerId == playerId && rp.Timestamp >= alignedFromUtc && rp.Timestamp < alignedToUtc)
            .Select(rp => rp.Timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var actions = await context.AdminActions
            .AsNoTracking()
            .Where(a => a.PlayerId == playerId && a.Created >= alignedFromUtc && a.Created < alignedToUtc)
            .Select(a => a.Created)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var sessionsByBucket = sessions.GroupBy(t => TruncateToBucket(t, bucket)).ToDictionary(g => g.Key, g => g.Count());
        var actionsByBucket = actions.GroupBy(t => TruncateToBucket(t, bucket)).ToDictionary(g => g.Key, g => g.Count());

        var bucketStarts = BuildBuckets(alignedFromUtc, alignedToUtc, bucket);
        var points = bucketStarts.Select(start => new AnalyticsTimeseriesPointDto
        {
            BucketStartUtc = start,
            BucketEndUtc = AddBucket(start, bucket),
            Value = sessionsByBucket.GetValueOrDefault(start, 0)
        }).ToList();

        var labels = points.Select(p => p.BucketStartUtc).ToList();
        var series = new List<AnalyticsSeriesDto>
        {
            BuildSeries("sessions", "Sessions", labels, points.Select(p => (double)p.Value)),
            BuildSeries("moderationActions", "Moderation Actions", labels, labels.Select(l => (double)actionsByBucket.GetValueOrDefault(l, 0)))
        };

        var currentTotal = points.Sum(p => (double)p.Value);
        var summary = compareMode == AnalyticsCompareMode.None
            ? null
            : BuildComparisonSummary(currentTotal, comparePeriods);

        var dto = new PlayerTrendsDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Bucket = bucket,
            Points = points,
            Labels = labels,
            Series = series,
            Summary = summary,
            Meta = BuildCompareMeta(compareMode, comparePeriods, alignMode, timezone, normalize)
        };

        return new ApiResponse<PlayerTrendsDto>(dto).ToApiResult();
    }

    [HttpGet("{playerId:guid}/related")]
    [ProducesResponseType<PlayerRelatedActivityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRelatedActivity(
        Guid playerId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayerAnalyticsV2Api)this).GetRelatedActivity(playerId, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlayerRelatedActivityDto>> IPlayerAnalyticsV2Api.GetRelatedActivity(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<PlayerRelatedActivityDto>(HttpStatusCode.BadRequest);
        }

        var playerExists = await context.Players
            .AsNoTracking()
            .AnyAsync(p => p.PlayerId == playerId, cancellationToken).ConfigureAwait(false);

        if (!playerExists)
        {
            return new ApiResult<PlayerRelatedActivityDto>(HttpStatusCode.NotFound);
        }

        var relatedPlayerIds = await context.RecentPlayers
            .AsNoTracking()
            .Where(rp => rp.PlayerId == playerId && rp.Timestamp >= fromUtc && rp.Timestamp < toUtc && rp.IpAddress != null)
            .Select(rp => rp.IpAddress!)
            .Distinct()
            .Join(
                context.RecentPlayers.AsNoTracking(),
                ip => ip,
                rp => rp.IpAddress!,
                (ip, rp) => rp.PlayerId)
            .Where(pid => pid != null && pid != playerId)
            .Select(pid => pid!.Value)
            .Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var sharedIpCount = await context.PlayerIpAddresses
            .AsNoTracking()
            .Where(pia => pia.PlayerId == playerId && pia.Address != null)
            .Select(pia => pia.Address!)
            .Distinct()
            .Join(
                context.PlayerIpAddresses.AsNoTracking().Where(pia => pia.PlayerId != playerId),
                ip => ip,
                other => other.Address!,
                (ip, other) => ip)
            .Distinct()
            .CountAsync(cancellationToken).ConfigureAwait(false);

        var sharedAliasCount = await context.PlayerAliases
            .AsNoTracking()
            .Where(pa => pa.PlayerId == playerId && pa.Name != null)
            .Select(pa => pa.Name!)
            .Distinct()
            .Join(
                context.PlayerAliases.AsNoTracking().Where(pa => pa.PlayerId != playerId),
                alias => alias,
                other => other.Name!,
                (alias, other) => alias)
            .Distinct()
            .CountAsync(cancellationToken).ConfigureAwait(false);

        var dto = new PlayerRelatedActivityDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            RelatedPlayersCount = relatedPlayerIds.Count,
            SharedIpAddressesCount = sharedIpCount,
            SharedAliasesCount = sharedAliasCount
        };

        return new ApiResponse<PlayerRelatedActivityDto>(dto).ToApiResult();
    }

    [HttpGet("{playerId:guid}/moderation")]
    [ProducesResponseType<PlayerModerationSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModerationSummary(
        Guid playerId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayerAnalyticsV2Api)this).GetModerationSummary(playerId, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlayerModerationSummaryDto>> IPlayerAnalyticsV2Api.GetModerationSummary(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<PlayerModerationSummaryDto>(HttpStatusCode.BadRequest);
        }

        var playerExists = await context.Players
            .AsNoTracking()
            .AnyAsync(p => p.PlayerId == playerId, cancellationToken).ConfigureAwait(false);

        if (!playerExists)
        {
            return new ApiResult<PlayerModerationSummaryDto>(HttpStatusCode.NotFound);
        }

        var actionCounts = await context.AdminActions
            .AsNoTracking()
            .Where(a => a.PlayerId == playerId && a.Created >= fromUtc && a.Created < toUtc)
            .GroupBy(a => a.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken).ConfigureAwait(false);

        var dto = new PlayerModerationSummaryDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            WarningsCount = actionCounts.GetValueOrDefault((int)AdminActionType.Warning, 0),
            // Mutes are not represented in AdminActionType in this repository contract.
            MutesCount = 0,
            KicksCount = actionCounts.GetValueOrDefault((int)AdminActionType.Kick, 0),
            BansCount = actionCounts.GetValueOrDefault((int)AdminActionType.Ban, 0)
        };

        return new ApiResponse<PlayerModerationSummaryDto>(dto).ToApiResult();
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