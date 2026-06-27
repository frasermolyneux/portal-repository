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
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Global;
using XtremeIdiots.Portal.Repository.Api.V1.Analytics;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.TableStorage;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}/analytics/global")]
public class GlobalAnalyticsController : ControllerBase, IGlobalAnalyticsApi
{
    private readonly PortalDbContext context;
    private readonly ILiveStatusStore liveStatusStore;

    public GlobalAnalyticsController(PortalDbContext context, ILiveStatusStore liveStatusStore)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(liveStatusStore);
        this.context = context;
        this.liveStatusStore = liveStatusStore;
    }

    [HttpGet("overview")]
    [ProducesResponseType<GlobalOverviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOverview([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var response = await ((IGlobalAnalyticsApi)this).GetOverview(fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<GlobalOverviewDto>> IGlobalAnalyticsApi.GetOverview(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<GlobalOverviewDto>(HttpStatusCode.BadRequest);
        }

        var window = CreateWindow(fromUtc, toUtc);

        var totalServers = await context.GameServers
            .AsNoTracking()
            .CountAsync(gs => !gs.Deleted, cancellationToken).ConfigureAwait(false);

        var liveStatuses = await liveStatusStore.GetAllServerLiveStatusesAsync(cancellationToken).ConfigureAwait(false);
        var onlineServers = liveStatuses.Count(x => x.IsOnline);
        var totalPlayersOnline = liveStatuses.Where(x => x.IsOnline).Sum(x => x.CurrentPlayers);

        var uniquePlayersWindow = await context.Players
            .AsNoTracking()
            .CountAsync(p => p.LastSeen >= fromUtc && p.LastSeen < toUtc, cancellationToken).ConfigureAwait(false);

        var totalEvents = await context.GameServerEvents
            .AsNoTracking()
            .CountAsync(e => e.Timestamp >= fromUtc && e.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var totalChatMessages = await context.ChatMessages
            .AsNoTracking()
            .CountAsync(c => c.Timestamp >= fromUtc && c.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var openReports = await context.Reports
            .AsNoTracking()
            .CountAsync(r => !r.Closed && r.Timestamp >= fromUtc && r.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var activeBans = await context.AdminActions
            .AsNoTracking()
            .CountAsync(a =>
                ((a.Type == (int)AdminActionType.Ban && (a.Expires == null || a.Expires > toUtc))
                || (a.Type == (int)AdminActionType.TempBan && a.Expires > toUtc))
                && a.Created <= toUtc,
                cancellationToken).ConfigureAwait(false);

        var dto = new GlobalOverviewDto
        {
            Window = window,
            TotalServers = totalServers,
            OnlineServers = onlineServers,
            TotalPlayersOnline = totalPlayersOnline,
            UniquePlayersWindow = uniquePlayersWindow,
            TotalEvents = totalEvents,
            TotalChatMessages = totalChatMessages,
            OpenReports = openReports,
            ActiveBans = activeBans
        };

        return new ApiResponse<GlobalOverviewDto>(dto).ToApiResult();
    }

    [HttpGet("timeseries")]
    [ProducesResponseType<GlobalTimeseriesDto>(StatusCodes.Status200OK)]
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
        var response = await ((IGlobalAnalyticsApi)this).GetTimeseries(
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

    async Task<ApiResult<GlobalTimeseriesDto>> IGlobalAnalyticsApi.GetTimeseries(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken)
    {
        return await ((IGlobalAnalyticsApi)this).GetTimeseries(
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

    async Task<ApiResult<GlobalTimeseriesDto>> IGlobalAnalyticsApi.GetTimeseries(
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
            return new ApiResult<GlobalTimeseriesDto>(HttpStatusCode.BadRequest);
        }

        var stats = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= alignedFromUtc && s.Timestamp < alignedToUtc)
            .OrderBy(s => s.Timestamp)
            .Select(s => new { s.Timestamp, s.PlayerCount })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var events = await context.GameServerEvents
            .AsNoTracking()
            .Where(e => e.Timestamp >= alignedFromUtc && e.Timestamp < alignedToUtc)
            .Select(e => e.Timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var chat = await context.ChatMessages
            .AsNoTracking()
            .Where(c => c.Timestamp >= alignedFromUtc && c.Timestamp < alignedToUtc)
            .Select(c => c.Timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var statByBucket = stats.GroupBy(s => TruncateToBucket(s.Timestamp, bucket))
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    AvgPlayers = g.Average(x => (double)x.PlayerCount),
                    PeakPlayers = g.Max(x => x.PlayerCount)
                });

        var eventsByBucket = events.GroupBy(t => TruncateToBucket(t, bucket)).ToDictionary(g => g.Key, g => g.Count());
        var chatByBucket = chat.GroupBy(t => TruncateToBucket(t, bucket)).ToDictionary(g => g.Key, g => g.Count());

        var bucketStarts = BuildBuckets(alignedFromUtc, alignedToUtc, bucket);
        var points = bucketStarts.Select(start => new GlobalTimeseriesPointDto
        {
            BucketStartUtc = start,
            AvgPlayers = statByBucket.TryGetValue(start, out var s) ? Math.Round(s.AvgPlayers, 2) : 0,
            PeakPlayers = statByBucket.TryGetValue(start, out var p) ? p.PeakPlayers : 0,
            EventsCount = eventsByBucket.GetValueOrDefault(start, 0),
            ChatCount = chatByBucket.GetValueOrDefault(start, 0)
        }).ToList();

        var labels = points.Select(p => p.BucketStartUtc).ToList();
        var series = new List<AnalyticsSeriesDto>
        {
            BuildSeries("avgPlayers", "Average Players", labels, points.Select(p => p.AvgPlayers)),
            BuildSeries("peakPlayers", "Peak Players", labels, points.Select(p => (double)p.PeakPlayers)),
            BuildSeries("events", "Events", labels, points.Select(p => (double)p.EventsCount)),
            BuildSeries("chat", "Chat", labels, points.Select(p => (double)p.ChatCount))
        };

        var currentTotal = points.Where(p => p.BucketStartUtc < toUtc).Sum(p => p.AvgPlayers);

        AnalyticsCompareSummaryDto? summary = null;
        var comparisonWindows = AnalyticsComparison.GetWindows(alignedFromUtc, alignedToUtc, compareMode, comparePeriods, alignMode);
        if (comparisonWindows.Count > 0)
        {
            var earliestCmpFrom = comparisonWindows.Min(w => w.FromUtc);
            var cmpStats = await context.GameServerStats
                .AsNoTracking()
                .Where(s => s.Timestamp >= earliestCmpFrom && s.Timestamp < alignedFromUtc)
                .Select(s => new { s.Timestamp, s.PlayerCount })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var cmpAvgByBucket = cmpStats
                .GroupBy(s => AnalyticsTimeBucketing.Truncate(s.Timestamp, bucket))
                .ToDictionary(g => g.Key, g => g.Average(x => (double)x.PlayerCount));

            var comparisonTotals = new List<double>(comparisonWindows.Count);
            foreach (var cmpWindow in comparisonWindows)
            {
                var cmpSeries = AnalyticsComparison.BuildComparisonSeries(
                    "avgPlayers", "Average Players", cmpWindow, bucketStarts, bucket, cmpAvgByBucket, toUtc);
                comparisonTotals.Add(cmpSeries.Values.Sum(v => v.Value));
                series.Add(cmpSeries);
            }

            summary = AnalyticsComparison.BuildSummary(currentTotal, comparisonTotals);
        }

        if (normalize)
        {
            AnalyticsComparison.ApplyIndex100(series);
        }

        var dto = new GlobalTimeseriesDto
        {
            Bucket = bucket,
            Points = points,
            Labels = labels,
            Series = series,
            Summary = summary,
            Meta = BuildCompareMeta(compareMode, comparePeriods, alignMode, timezone, normalize)
        };

        return new ApiResponse<GlobalTimeseriesDto>(dto).ToApiResult();
    }

    [HttpGet("games")]
    [ProducesResponseType<GlobalGameBreakdownDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGameBreakdown(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IGlobalAnalyticsApi)this).GetGameBreakdown(fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<GlobalGameBreakdownDto>> IGlobalAnalyticsApi.GetGameBreakdown(DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<GlobalGameBreakdownDto>(HttpStatusCode.BadRequest);
        }

        var statsByGame = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.GameServer != null && !s.GameServer.Deleted)
            .GroupBy(s => s.GameServer!.GameType)
            .Select(g => new
            {
                GameType = g.Key,
                AvgPlayers = g.Average(x => (double)x.PlayerCount),
                ServerIds = g.Select(x => x.GameServerId).Distinct().Count()
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var eventsByGame = await context.GameServerEvents
            .AsNoTracking()
            .Where(e => e.Timestamp >= fromUtc && e.Timestamp < toUtc && !e.GameServer.Deleted)
            .GroupBy(e => e.GameServer.GameType)
            .Select(g => new { GameType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GameType, x => x.Count, cancellationToken).ConfigureAwait(false);

        var uniquePlayersByGame = await context.Players
            .AsNoTracking()
            .Where(p => p.LastSeen >= fromUtc && p.LastSeen < toUtc)
            .GroupBy(p => p.GameType)
            .Select(g => new { GameType = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GameType, x => x.Count, cancellationToken).ConfigureAwait(false);

        var items = statsByGame
            .OrderByDescending(x => x.AvgPlayers)
            .Take(top)
            .Select(x => new GlobalGameBreakdownItemDto
            {
                GameType = x.GameType.ToGameType(),
                ServerCount = x.ServerIds,
                AvgPlayers = Math.Round(x.AvgPlayers, 2),
                EventsCount = eventsByGame.GetValueOrDefault(x.GameType, 0),
                UniquePlayers = uniquePlayersByGame.GetValueOrDefault(x.GameType, 0)
            })
            .ToList();

        var dto = new GlobalGameBreakdownDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<GlobalGameBreakdownDto>(dto).ToApiResult();
    }

    [HttpGet("servers")]
    [ProducesResponseType<GlobalServerBreakdownDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetServerBreakdown(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IGlobalAnalyticsApi)this).GetServerBreakdown(fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<GlobalServerBreakdownDto>> IGlobalAnalyticsApi.GetServerBreakdown(DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<GlobalServerBreakdownDto>(HttpStatusCode.BadRequest);
        }

        var liveStatusLookup = (await liveStatusStore.GetAllServerLiveStatusesAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(x => x.ServerId, x => x);

        var statsByServer = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.GameServerId != null)
            .GroupBy(s => s.GameServerId!.Value)
            .Select(g => new
            {
                GameServerId = g.Key,
                AvgPlayers = g.Average(x => (double)x.PlayerCount),
                PeakPlayers = g.Max(x => x.PlayerCount)
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var eventsByServer = await context.GameServerEvents
            .AsNoTracking()
            .Where(e => e.Timestamp >= fromUtc && e.Timestamp < toUtc)
            .GroupBy(e => e.GameServerId)
            .Select(g => new { GameServerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GameServerId, x => x.Count, cancellationToken).ConfigureAwait(false);

        var servers = await context.GameServers
            .AsNoTracking()
            .Where(gs => !gs.Deleted)
            .ToDictionaryAsync(gs => gs.GameServerId, cancellationToken).ConfigureAwait(false);

        var items = statsByServer
            .Where(s => servers.ContainsKey(s.GameServerId))
            .OrderByDescending(s => s.AvgPlayers)
            .Take(top)
            .Select(s =>
            {
                var server = servers[s.GameServerId];
                var hasLive = liveStatusLookup.TryGetValue(s.GameServerId, out var live);
                return new GlobalServerBreakdownItemDto
                {
                    GameServerId = s.GameServerId,
                    Title = hasLive && !string.IsNullOrWhiteSpace(live!.Title) ? live.Title : server.Title,
                    GameType = server.GameType.ToGameType(),
                    AvgPlayers = Math.Round(s.AvgPlayers, 2),
                    PeakPlayers = s.PeakPlayers,
                    EventsCount = eventsByServer.GetValueOrDefault(s.GameServerId, 0),
                    Online = hasLive && live!.IsOnline
                };
            })
            .ToList();

        var dto = new GlobalServerBreakdownDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<GlobalServerBreakdownDto>(dto).ToApiResult();
    }

    [HttpGet("players")]
    [ProducesResponseType<GlobalPlayerActivityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPlayerActivity(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IGlobalAnalyticsApi)this).GetPlayerActivity(fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<GlobalPlayerActivityDto>> IGlobalAnalyticsApi.GetPlayerActivity(DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<GlobalPlayerActivityDto>(HttpStatusCode.BadRequest);
        }

        var query = await context.ChatMessages
            .AsNoTracking()
            .Where(c => c.Timestamp >= fromUtc && c.Timestamp < toUtc)
            .GroupBy(c => c.PlayerId)
            .Select(g => new
            {
                PlayerId = g.Key,
                ActivityCount = g.Count(),
                LastSeenUtc = g.Max(x => x.Timestamp)
            })
            .OrderByDescending(x => x.ActivityCount)
            .Take(top)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var playerIds = query.Select(x => x.PlayerId).ToList();
        var players = await context.Players
            .AsNoTracking()
            .Where(p => playerIds.Contains(p.PlayerId))
            .ToDictionaryAsync(p => p.PlayerId, cancellationToken).ConfigureAwait(false);

        var items = query
            .Where(x => players.ContainsKey(x.PlayerId))
            .Select(x =>
            {
                var player = players[x.PlayerId];
                return new GlobalPlayerActivityItemDto
                {
                    PlayerId = x.PlayerId,
                    DisplayName = string.IsNullOrWhiteSpace(player.Username) ? "Unknown" : player.Username,
                    GameType = player.GameType.ToGameType(),
                    ActivityCount = x.ActivityCount,
                    LastSeenUtc = x.LastSeenUtc
                };
            })
            .ToList();

        var dto = new GlobalPlayerActivityDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<GlobalPlayerActivityDto>(dto).ToApiResult();
    }

    [HttpGet("geo")]
    [ProducesResponseType<GlobalGeoDistributionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGeoDistribution(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IGlobalAnalyticsApi)this).GetGeoDistribution(fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<GlobalGeoDistributionDto>> IGlobalAnalyticsApi.GetGeoDistribution(DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<GlobalGeoDistributionDto>(HttpStatusCode.BadRequest);
        }

        var groups = await context.RecentPlayers
            .AsNoTracking()
            .Where(rp => rp.Timestamp >= fromUtc && rp.Timestamp < toUtc && rp.CountryCode != null && rp.CountryCode != string.Empty)
            .GroupBy(rp => rp.CountryCode!)
            .Select(g => new
            {
                CountryCode = g.Key,
                PlayerCount = g.Select(x => x.PlayerId).Distinct().Count()
            })
            .OrderByDescending(x => x.PlayerCount)
            .Take(top)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var total = groups.Sum(x => x.PlayerCount);
        var items = groups.Select(x => new GlobalGeoDistributionItemDto
        {
            CountryCode = x.CountryCode,
            PlayerCount = x.PlayerCount,
            Percentage = total > 0 ? Math.Round((double)x.PlayerCount * 100d / total, 2) : 0
        }).ToList();

        var dto = new GlobalGeoDistributionDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<GlobalGeoDistributionDto>(dto).ToApiResult();
    }

    [HttpGet("moderation")]
    [ProducesResponseType<GlobalModerationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetModeration([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var response = await ((IGlobalAnalyticsApi)this).GetModeration(fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<GlobalModerationDto>> IGlobalAnalyticsApi.GetModeration(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<GlobalModerationDto>(HttpStatusCode.BadRequest);
        }

        var actionCounts = await context.AdminActions
            .AsNoTracking()
            .Where(a => a.Created >= fromUtc && a.Created < toUtc)
            .GroupBy(a => a.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken).ConfigureAwait(false);

        var totalActions = actionCounts.Values.Sum();

        var openReports = await context.Reports
            .AsNoTracking()
            .CountAsync(r => !r.Closed && r.Timestamp >= fromUtc && r.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var closedReports = await context.Reports
            .AsNoTracking()
            .CountAsync(r => r.Closed && r.Timestamp >= fromUtc && r.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var moderators = await context.AdminActions
            .AsNoTracking()
            .Where(a => a.Created >= fromUtc && a.Created < toUtc && a.UserProfileId != null)
            .GroupBy(a => a.UserProfileId!.Value)
            .Select(g => new
            {
                UserProfileId = g.Key,
                Total = g.Count(),
                Bans = g.Count(x => x.Type == (int)AdminActionType.Ban),
                TempBans = g.Count(x => x.Type == (int)AdminActionType.TempBan),
                Kicks = g.Count(x => x.Type == (int)AdminActionType.Kick),
                Warnings = g.Count(x => x.Type == (int)AdminActionType.Warning),
                Observations = g.Count(x => x.Type == (int)AdminActionType.Observation)
            })
            .OrderByDescending(x => x.Total)
            .Take(AnalyticsQueryDefaults.DefaultTop)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var moderatorIds = moderators.Select(x => x.UserProfileId).ToList();
        var profileNames = await context.UserProfiles
            .AsNoTracking()
            .Where(up => moderatorIds.Contains(up.UserProfileId))
            .Select(up => new { up.UserProfileId, up.DisplayName })
            .ToDictionaryAsync(x => x.UserProfileId, x => x.DisplayName ?? "Unknown", cancellationToken).ConfigureAwait(false);

        var summary = new GlobalModerationSummaryDto
        {
            TotalActions = totalActions,
            OpenReports = openReports,
            ClosedReports = closedReports,
            ByType = new AdminActionCountsDto
            {
                Bans = actionCounts.GetValueOrDefault((int)AdminActionType.Ban, 0),
                TempBans = actionCounts.GetValueOrDefault((int)AdminActionType.TempBan, 0),
                Kicks = actionCounts.GetValueOrDefault((int)AdminActionType.Kick, 0),
                Warnings = actionCounts.GetValueOrDefault((int)AdminActionType.Warning, 0),
                Observations = actionCounts.GetValueOrDefault((int)AdminActionType.Observation, 0)
            },
            TopModerators = moderators.Select(x => new AdminLeaderboardEntryDto
            {
                AdminId = x.UserProfileId,
                DisplayName = profileNames.GetValueOrDefault(x.UserProfileId, "Unknown"),
                Bans = x.Bans,
                TempBans = x.TempBans,
                Kicks = x.Kicks,
                Warnings = x.Warnings,
                Observations = x.Observations,
                Total = x.Total
            }).ToList()
        };

        var dto = new GlobalModerationDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Summary = summary
        };

        return new ApiResponse<GlobalModerationDto>(dto).ToApiResult();
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