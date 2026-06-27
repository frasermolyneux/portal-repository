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
using XtremeIdiots.Portal.Repository.Api.V1.Analytics;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
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

    [HttpGet("overview")]
    [ProducesResponseType<PlayersOverviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOverview([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayerAnalyticsV2Api)this).GetOverview(fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlayersOverviewDto>> IPlayerAnalyticsV2Api.GetOverview(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<PlayersOverviewDto>(HttpStatusCode.BadRequest);
        }

        var activePlayers = await context.Players
            .AsNoTracking()
            .CountAsync(p => p.LastSeen >= fromUtc && p.LastSeen < toUtc, cancellationToken).ConfigureAwait(false);

        var newPlayers = await context.Players
            .AsNoTracking()
            .CountAsync(p => p.FirstSeen >= fromUtc && p.FirstSeen < toUtc, cancellationToken).ConfigureAwait(false);

        var returningPlayers = await context.Players
            .AsNoTracking()
            .CountAsync(p => p.LastSeen >= fromUtc && p.LastSeen < toUtc && p.FirstSeen < fromUtc, cancellationToken).ConfigureAwait(false);

        var dto = new PlayersOverviewDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            TotalPlayers = activePlayers,
            NewPlayers = newPlayers,
            ActivePlayers = activePlayers,
            ReturningPlayers = returningPlayers
        };

        return new ApiResponse<PlayersOverviewDto>(dto).ToApiResult();
    }

    [HttpGet("timeseries")]
    [ProducesResponseType<PlayersTimeseriesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTimeseries(
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
        var response = await ((IPlayerAnalyticsV2Api)this).GetTimeseries(
            fromUtc, toUtc, bucket, compareMode, comparePeriods, alignMode, timezone, normalize, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlayersTimeseriesDto>> IPlayerAnalyticsV2Api.GetTimeseries(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken)
    {
        return await ((IPlayerAnalyticsV2Api)this).GetTimeseries(
            fromUtc, toUtc, bucket, AnalyticsCompareMode.None, AnalyticsQueryDefaults.DefaultComparePeriods, AnalyticsAlignMode.None, "UTC", false, cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<PlayersTimeseriesDto>> IPlayerAnalyticsV2Api.GetTimeseries(
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
            || !AnalyticsQueryValidator.TryValidateComparisonLookback(fromUtc, toUtc, compareMode, comparePeriods, alignMode, out _)
            || !AnalyticsQueryValidator.TryGetAlignedWindow(fromUtc, toUtc, alignMode, timezone, out var alignedFromUtc, out var alignedToUtc, out _))
        {
            return new ApiResult<PlayersTimeseriesDto>(HttpStatusCode.BadRequest);
        }

        var newPlayerDates = await context.Players
            .AsNoTracking()
            .Where(p => p.FirstSeen >= alignedFromUtc && p.FirstSeen < alignedToUtc)
            .Select(p => p.FirstSeen)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var activeSamples = await context.RecentPlayers
            .AsNoTracking()
            .Where(rp => rp.PlayerId != null && rp.Timestamp >= alignedFromUtc && rp.Timestamp < alignedToUtc)
            .Select(rp => new { rp.PlayerId, rp.Timestamp })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var newByBucket = newPlayerDates.GroupBy(t => AnalyticsTimeBucketing.Truncate(t, bucket)).ToDictionary(g => g.Key, g => g.Count());
        var activeByBucket = activeSamples
            .GroupBy(x => AnalyticsTimeBucketing.Truncate(x.Timestamp, bucket))
            .ToDictionary(g => g.Key, g => g.Select(x => x.PlayerId).Distinct().Count());

        var bucketStarts = AnalyticsTimeBucketing.BuildBuckets(alignedFromUtc, alignedToUtc, bucket);
        var points = bucketStarts.Select(start => new PlayersTimeseriesPointDto
        {
            BucketStartUtc = start,
            NewPlayers = newByBucket.GetValueOrDefault(start, 0),
            ActivePlayers = activeByBucket.GetValueOrDefault(start, 0)
        }).ToList();

        var labels = points.Select(p => p.BucketStartUtc).ToList();
        var series = new List<AnalyticsSeriesDto>
        {
            BuildSeries("newPlayers", "New Players", labels, points.Select(p => (double)p.NewPlayers)),
            BuildSeries("activePlayers", "Active Players", labels, points.Select(p => (double)p.ActivePlayers))
        };

        var currentTotal = points.Where(p => p.BucketStartUtc < toUtc).Sum(p => (double)p.ActivePlayers);

        AnalyticsCompareSummaryDto? summary = null;
        var comparisonWindows = AnalyticsComparison.GetWindows(alignedFromUtc, alignedToUtc, compareMode, comparePeriods, alignMode);
        if (comparisonWindows.Count > 0)
        {
            var earliestCmpFrom = comparisonWindows.Min(w => w.FromUtc);
            var cmpSamples = await context.RecentPlayers
                .AsNoTracking()
                .Where(rp => rp.PlayerId != null && rp.Timestamp >= earliestCmpFrom && rp.Timestamp < alignedFromUtc)
                .Select(rp => new { rp.PlayerId, rp.Timestamp })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var cmpActiveByBucket = cmpSamples
                .GroupBy(x => AnalyticsTimeBucketing.Truncate(x.Timestamp, bucket))
                .ToDictionary(g => g.Key, g => (double)g.Select(x => x.PlayerId).Distinct().Count());

            var comparisonTotals = new List<double>(comparisonWindows.Count);
            foreach (var cmpWindow in comparisonWindows)
            {
                var cmpSeries = AnalyticsComparison.BuildComparisonSeries(
                    "activePlayers", "Active Players", cmpWindow, bucketStarts, bucket, cmpActiveByBucket, toUtc);
                comparisonTotals.Add(cmpSeries.Values.Sum(v => v.Value));
                series.Add(cmpSeries);
            }

            summary = AnalyticsComparison.BuildSummary(currentTotal, comparisonTotals);
        }

        if (normalize)
        {
            AnalyticsComparison.ApplyIndex100(series);
        }

        var dto = new PlayersTimeseriesDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Bucket = bucket,
            Points = points,
            Labels = labels,
            Series = series,
            Summary = summary,
            Meta = BuildCompareMeta(compareMode, comparePeriods, alignMode, timezone, normalize)
        };

        return new ApiResponse<PlayersTimeseriesDto>(dto).ToApiResult();
    }

    [HttpGet("top")]
    [ProducesResponseType<PlayersTopDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTop(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayerAnalyticsV2Api)this).GetTop(fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlayersTopDto>> IPlayerAnalyticsV2Api.GetTop(DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<PlayersTopDto>(HttpStatusCode.BadRequest);
        }

        var topRaw = await context.RecentPlayers
            .AsNoTracking()
            .Where(rp => rp.PlayerId != null && rp.Timestamp >= fromUtc && rp.Timestamp < toUtc)
            .GroupBy(rp => rp.PlayerId!.Value)
            .Select(g => new { PlayerId = g.Key, Sessions = g.Count() })
            .OrderByDescending(x => x.Sessions)
            .Take(top)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var ids = topRaw.Select(x => x.PlayerId).ToList();
        var players = await context.Players
            .AsNoTracking()
            .Where(p => ids.Contains(p.PlayerId))
            .Select(p => new { p.PlayerId, p.Username, p.GameType, p.LastSeen })
            .ToDictionaryAsync(p => p.PlayerId, cancellationToken).ConfigureAwait(false);

        var items = topRaw.Select(x =>
        {
            players.TryGetValue(x.PlayerId, out var p);
            return new PlayersTopItemDto
            {
                PlayerId = x.PlayerId,
                Username = string.IsNullOrWhiteSpace(p?.Username) ? "Unknown" : p!.Username!,
                GameType = (p?.GameType ?? 0).ToGameType(),
                SessionsCount = x.Sessions,
                LastSeenUtc = p?.LastSeen ?? default
            };
        }).ToList();

        var dto = new PlayersTopDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<PlayersTopDto>(dto).ToApiResult();
    }

    [HttpGet("by-game")]
    [ProducesResponseType<PlayersByGameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByGame(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayerAnalyticsV2Api)this).GetByGame(fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlayersByGameDto>> IPlayerAnalyticsV2Api.GetByGame(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<PlayersByGameDto>(HttpStatusCode.BadRequest);
        }

        var grouped = await context.Players
            .AsNoTracking()
            .Where(p => p.LastSeen >= fromUtc && p.LastSeen < toUtc)
            .GroupBy(p => p.GameType)
            .Select(g => new
            {
                GameType = g.Key,
                Active = g.Count(),
                New = g.Count(p => p.FirstSeen >= fromUtc && p.FirstSeen < toUtc)
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var items = grouped
            .OrderByDescending(x => x.Active)
            .Select(x => new PlayersByGameItemDto
            {
                GameType = x.GameType.ToGameType(),
                TotalPlayers = x.Active,
                NewPlayers = x.New,
                ActivePlayers = x.Active
            })
            .ToList();

        var dto = new PlayersByGameDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<PlayersByGameDto>(dto).ToApiResult();
    }

    [HttpGet("by-server")]
    [ProducesResponseType<PlayersByServerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByServer(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayerAnalyticsV2Api)this).GetByServer(fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlayersByServerDto>> IPlayerAnalyticsV2Api.GetByServer(DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<PlayersByServerDto>(HttpStatusCode.BadRequest);
        }

        var recent = await context.RecentPlayers
            .AsNoTracking()
            .Where(rp => rp.PlayerId != null && rp.GameServerId != null && rp.Timestamp >= fromUtc && rp.Timestamp < toUtc)
            .Select(rp => new { GameServerId = rp.GameServerId!.Value, rp.PlayerId })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var byServer = recent
            .GroupBy(r => r.GameServerId)
            .Select(g => new { GameServerId = g.Key, Active = g.Select(x => x.PlayerId).Distinct().Count() })
            .OrderByDescending(x => x.Active)
            .Take(top)
            .ToList();

        var serverIds = byServer.Select(x => x.GameServerId).ToList();
        var servers = await context.GameServers
            .AsNoTracking()
            .Where(gs => serverIds.Contains(gs.GameServerId))
            .Select(gs => new { gs.GameServerId, gs.Title, gs.GameType })
            .ToDictionaryAsync(gs => gs.GameServerId, cancellationToken).ConfigureAwait(false);

        var items = byServer.Select(x =>
        {
            servers.TryGetValue(x.GameServerId, out var s);
            return new PlayersByServerItemDto
            {
                GameServerId = x.GameServerId,
                Title = string.IsNullOrWhiteSpace(s?.Title) ? "Unknown" : s!.Title,
                GameType = (s?.GameType ?? 0).ToGameType(),
                ActivePlayers = x.Active
            };
        }).ToList();

        var dto = new PlayersByServerDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<PlayersByServerDto>(dto).ToApiResult();
    }

    [HttpGet("{playerId:guid}")]
    [ProducesResponseType<PlayerDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerDetail(
        Guid playerId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IPlayerAnalyticsV2Api)this).GetPlayerDetail(playerId, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlayerDetailDto>> IPlayerAnalyticsV2Api.GetPlayerDetail(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<PlayerDetailDto>(HttpStatusCode.BadRequest);
        }

        var player = await context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerId == playerId, cancellationToken).ConfigureAwait(false);

        if (player == null)
        {
            return new ApiResult<PlayerDetailDto>(HttpStatusCode.NotFound);
        }

        var sessionsCount = await context.RecentPlayers
            .AsNoTracking()
            .CountAsync(rp => rp.PlayerId == playerId && rp.Timestamp >= fromUtc && rp.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var actionCounts = await context.AdminActions
            .AsNoTracking()
            .Where(a => a.PlayerId == playerId && a.Created >= fromUtc && a.Created < toUtc)
            .GroupBy(a => a.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken).ConfigureAwait(false);

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

        var dto = new PlayerDetailDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            PlayerId = playerId,
            Username = string.IsNullOrWhiteSpace(player.Username) ? "Unknown" : player.Username,
            GameType = player.GameType.ToGameType(),
            FirstSeenUtc = player.FirstSeen,
            LastSeenUtc = player.LastSeen,
            SessionsCount = sessionsCount,
            // Approximate playtime from observed recent-player session snapshots.
            TotalPlayTimeMinutes = sessionsCount,
            Moderation = new PlayerModerationSummaryDto
            {
                Window = CreateWindow(fromUtc, toUtc),
                WarningsCount = actionCounts.GetValueOrDefault((int)AdminActionType.Warning, 0),
                // Mutes are not represented in AdminActionType in this repository contract.
                MutesCount = 0,
                KicksCount = actionCounts.GetValueOrDefault((int)AdminActionType.Kick, 0),
                BansCount = actionCounts.GetValueOrDefault((int)AdminActionType.Ban, 0)
            },
            Related = new PlayerRelatedActivityDto
            {
                Window = CreateWindow(fromUtc, toUtc),
                RelatedPlayersCount = relatedPlayerIds.Count,
                SharedIpAddressesCount = sharedIpCount,
                SharedAliasesCount = sharedAliasCount
            }
        };

        return new ApiResponse<PlayerDetailDto>(dto).ToApiResult();
    }

    [HttpGet("{playerId:guid}/timeseries")]
    [ProducesResponseType<PlayerTrendsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerTimeseries(
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
        var response = await ((IPlayerAnalyticsV2Api)this).GetPlayerTimeseries(
            playerId, fromUtc, toUtc, bucket, compareMode, comparePeriods, alignMode, timezone, normalize, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<PlayerTrendsDto>> IPlayerAnalyticsV2Api.GetPlayerTimeseries(Guid playerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken)
    {
        return await ((IPlayerAnalyticsV2Api)this).GetPlayerTimeseries(
            playerId, fromUtc, toUtc, bucket, AnalyticsCompareMode.None, AnalyticsQueryDefaults.DefaultComparePeriods, AnalyticsAlignMode.None, "UTC", false, cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<PlayerTrendsDto>> IPlayerAnalyticsV2Api.GetPlayerTimeseries(
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
            || !AnalyticsQueryValidator.TryValidateComparisonLookback(fromUtc, toUtc, compareMode, comparePeriods, alignMode, out _)
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

        var sessionsByBucket = sessions.GroupBy(t => AnalyticsTimeBucketing.Truncate(t, bucket)).ToDictionary(g => g.Key, g => g.Count());
        var actionsByBucket = actions.GroupBy(t => AnalyticsTimeBucketing.Truncate(t, bucket)).ToDictionary(g => g.Key, g => g.Count());

        var bucketStarts = AnalyticsTimeBucketing.BuildBuckets(alignedFromUtc, alignedToUtc, bucket);
        var points = bucketStarts.Select(start => new AnalyticsTimeseriesPointDto
        {
            BucketStartUtc = start,
            BucketEndUtc = AnalyticsTimeBucketing.Add(start, bucket),
            Value = sessionsByBucket.GetValueOrDefault(start, 0)
        }).ToList();

        var labels = points.Select(p => p.BucketStartUtc).ToList();
        var series = new List<AnalyticsSeriesDto>
        {
            BuildSeries("sessions", "Sessions", labels, points.Select(p => (double)p.Value)),
            BuildSeries("moderationActions", "Moderation Actions", labels, labels.Select(l => (double)actionsByBucket.GetValueOrDefault(l, 0)))
        };

        var currentTotal = points.Where(p => p.BucketStartUtc < toUtc).Sum(p => (double)p.Value);

        AnalyticsCompareSummaryDto? summary = null;
        var comparisonWindows = AnalyticsComparison.GetWindows(alignedFromUtc, alignedToUtc, compareMode, comparePeriods, alignMode);
        if (comparisonWindows.Count > 0)
        {
            var earliestCmpFrom = comparisonWindows.Min(w => w.FromUtc);
            var cmpSessions = await context.RecentPlayers
                .AsNoTracking()
                .Where(rp => rp.PlayerId == playerId && rp.Timestamp >= earliestCmpFrom && rp.Timestamp < alignedFromUtc)
                .Select(rp => rp.Timestamp)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var cmpSessionsByBucket = cmpSessions
                .GroupBy(t => AnalyticsTimeBucketing.Truncate(t, bucket))
                .ToDictionary(g => g.Key, g => (double)g.Count());

            var comparisonTotals = new List<double>(comparisonWindows.Count);
            foreach (var cmpWindow in comparisonWindows)
            {
                var cmpSeries = AnalyticsComparison.BuildComparisonSeries(
                    "sessions", "Sessions", cmpWindow, bucketStarts, bucket, cmpSessionsByBucket, toUtc);
                comparisonTotals.Add(cmpSeries.Values.Sum(v => v.Value));
                series.Add(cmpSeries);
            }

            summary = AnalyticsComparison.BuildSummary(currentTotal, comparisonTotals);
        }

        if (normalize)
        {
            AnalyticsComparison.ApplyIndex100(series);
        }

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
}
