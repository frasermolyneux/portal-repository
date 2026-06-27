using System.Net;
using System.Text.Json;
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
using XtremeIdiots.Portal.Repository.Api.V1.Analytics;
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
    private const string ChatCommandExecutionEventType = "ChatCommandExecution";
    private const string ChatCommandDeniedEventType = "ChatCommandDenied";

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

        var recentPlayersCount = await context.RecentPlayers
            .AsNoTracking()
            .CountAsync(rp => rp.GameServerId == gameServerId && rp.Timestamp >= fromUtc && rp.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        var reportsCount = await context.Reports
            .AsNoTracking()
            .CountAsync(r => r.GameServerId == gameServerId && r.Timestamp >= fromUtc && r.Timestamp < toUtc, cancellationToken).ConfigureAwait(false);

        const double correlationWindowHours = 6;
        var adminActionsCount = await context.AdminActions
            .AsNoTracking()
            .CountAsync(a =>
                a.Created >= fromUtc
                && a.Created < toUtc
                && context.RecentPlayers.Any(rp =>
                    rp.PlayerId == a.PlayerId
                    && rp.GameServerId == gameServerId
                    && rp.Timestamp >= a.Created.AddHours(-correlationWindowHours)
                    && rp.Timestamp <= a.Created.AddHours(correlationWindowHours)),
                cancellationToken).ConfigureAwait(false);

        var dto = new ServerOverviewDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            GameServerId = gameServerId,
            Title = !string.IsNullOrWhiteSpace(live?.Title) ? live.Title : server.Title,
            GameType = server.GameType.ToGameType(),
            Online = live?.IsOnline ?? false,
            CurrentPlayers = live?.CurrentPlayers ?? 0,
            AvgPlayers = stats.Count == 0 ? 0 : Math.Round(stats.Average(), 2),
            PeakPlayers = stats.Count == 0 ? 0 : stats.Max(),
            EventsCount = eventsCount,
            ChatCount = chatCount,
            RecentPlayersCount = recentPlayersCount,
            AdminActionsCount = adminActionsCount,
            ReportsCount = reportsCount
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
            || !AnalyticsQueryValidator.TryValidateComparisonLookback(fromUtc, toUtc, compareMode, comparePeriods, alignMode, out _)
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

        var currentTotal = points.Where(p => p.Timestamp < toUtc).Sum(p => (double)p.UniquePlayers);

        AnalyticsCompareSummaryDto? summary = null;
        var comparisonWindows = AnalyticsComparison.GetWindows(alignedFromUtc, alignedToUtc, compareMode, comparePeriods, alignMode);
        if (comparisonWindows.Count > 0)
        {
            var earliestCmpFrom = comparisonWindows.Min(w => w.FromUtc);
            var cmpRecentPlayers = await context.RecentPlayers
                .AsNoTracking()
                .Where(rp => rp.GameServerId == gameServerId && rp.Timestamp >= earliestCmpFrom && rp.Timestamp < alignedFromUtc)
                .Select(rp => new { rp.PlayerId, rp.Timestamp })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var cmpUniqueByBucket = cmpRecentPlayers
                .GroupBy(x => AnalyticsTimeBucketing.Truncate(x.Timestamp, bucket))
                .ToDictionary(g => g.Key, g => (double)g.Select(x => x.PlayerId).Distinct().Count());

            var comparisonTotals = new List<double>(comparisonWindows.Count);
            foreach (var cmpWindow in comparisonWindows)
            {
                var cmpSeries = AnalyticsComparison.BuildComparisonSeries(
                    "uniquePlayers", "Unique Players", cmpWindow, bucketStarts, bucket, cmpUniqueByBucket, toUtc);
                comparisonTotals.Add(cmpSeries.Values.Sum(v => v.Value));
                series.Add(cmpSeries);
            }

            summary = AnalyticsComparison.BuildSummary(currentTotal, comparisonTotals);
        }

        if (normalize)
        {
            AnalyticsComparison.ApplyIndex100(series);
        }

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

    [HttpGet("{gameServerId:guid}/players-current")]
    [ProducesResponseType<ServerPlayersCurrentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayersCurrent(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var response = await ((IServerAnalyticsApi)this).GetPlayersCurrent(gameServerId, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ServerPlayersCurrentDto>> IServerAnalyticsApi.GetPlayersCurrent(Guid gameServerId, CancellationToken cancellationToken)
    {
        var server = await context.GameServers
            .AsNoTracking()
            .FirstOrDefaultAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken).ConfigureAwait(false);

        if (server == null)
        {
            return new ApiResult<ServerPlayersCurrentDto>(HttpStatusCode.NotFound);
        }

        var live = await liveStatusStore.GetServerLiveStatusAsync(gameServerId, cancellationToken).ConfigureAwait(false);
        var players = await liveStatusStore.GetLivePlayersAsync(gameServerId, cancellationToken).ConfigureAwait(false);

        var dto = new ServerPlayersCurrentDto
        {
            GameServerId = gameServerId,
            Online = live?.IsOnline ?? false,
            CurrentPlayers = live?.CurrentPlayers ?? players.Count,
            MaxPlayers = live?.MaxPlayers ?? 0,
            MapName = live?.Map,
            GameType = server.GameType.ToGameType(),
            LastUpdatedUtc = live?.LastUpdated,
            Players = players
        };

        return new ApiResponse<ServerPlayersCurrentDto>(dto).ToApiResult();
    }

    [HttpGet("{gameServerId:guid}/events-summary")]
    [ProducesResponseType<ServerEventsSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventsSummary(
        Guid gameServerId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IServerAnalyticsApi)this).GetEventsSummary(gameServerId, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ServerEventsSummaryDto>> IServerAnalyticsApi.GetEventsSummary(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<ServerEventsSummaryDto>(HttpStatusCode.BadRequest);
        }

        var serverExists = await context.GameServers
            .AsNoTracking()
            .AnyAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken).ConfigureAwait(false);

        if (!serverExists)
        {
            return new ApiResult<ServerEventsSummaryDto>(HttpStatusCode.NotFound);
        }

        var byType = await context.GameServerEvents
            .AsNoTracking()
            .Where(e => e.GameServerId == gameServerId && e.Timestamp >= fromUtc && e.Timestamp < toUtc)
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var items = byType
            .OrderByDescending(x => x.Count)
            .Select(x => new ServerEventTypeCountDto { EventType = x.EventType, Count = x.Count })
            .ToList();

        var dto = new ServerEventsSummaryDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            TotalEvents = items.Sum(i => i.Count),
            ByType = items
        };

        return new ApiResponse<ServerEventsSummaryDto>(dto).ToApiResult();
    }

    [HttpGet("{gameServerId:guid}/chat-summary")]
    [ProducesResponseType<ServerChatSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChatSummary(
        Guid gameServerId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IServerAnalyticsApi)this).GetChatSummary(gameServerId, fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ServerChatSummaryDto>> IServerAnalyticsApi.GetChatSummary(Guid gameServerId, DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<ServerChatSummaryDto>(HttpStatusCode.BadRequest);
        }

        var serverExists = await context.GameServers
            .AsNoTracking()
            .AnyAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken).ConfigureAwait(false);

        if (!serverExists)
        {
            return new ApiResult<ServerChatSummaryDto>(HttpStatusCode.NotFound);
        }

        var messages = await context.ChatMessages
            .AsNoTracking()
            .Where(c => c.GameServerId == gameServerId && c.Timestamp >= fromUtc && c.Timestamp < toUtc)
            .Select(c => new { c.PlayerId, c.Username })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var topChatters = messages
            .GroupBy(m => new { m.PlayerId, m.Username })
            .Select(g => new ServerChatterDto { PlayerId = g.Key.PlayerId, Username = g.Key.Username, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToList();

        var dto = new ServerChatSummaryDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            TotalMessages = messages.Count,
            UniqueChatters = messages.Select(m => m.PlayerId).Distinct().Count(),
            TopChatters = topChatters
        };

        return new ApiResponse<ServerChatSummaryDto>(dto).ToApiResult();
    }

    [HttpGet("{gameServerId:guid}/chat-commands-summary")]
    [ProducesResponseType<ServerChatCommandsSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChatCommandsSummary(
        Guid gameServerId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IServerAnalyticsApi)this).GetChatCommandsSummary(gameServerId, fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ServerChatCommandsSummaryDto>> IServerAnalyticsApi.GetChatCommandsSummary(Guid gameServerId, DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<ServerChatCommandsSummaryDto>(HttpStatusCode.BadRequest);
        }

        var serverExists = await context.GameServers
            .AsNoTracking()
            .AnyAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken).ConfigureAwait(false);

        if (!serverExists)
        {
            return new ApiResult<ServerChatCommandsSummaryDto>(HttpStatusCode.NotFound);
        }

        // The executed command name is stored only inside the event's JSON payload, so we materialise
        // the payloads for chat-command events in the window and aggregate by command in memory.
        // ChatCommandExecution = the command ran; ChatCommandDenied = the caller was blocked (both
        // carry the same commandPrefix payload).
        var payloads = await context.GameServerEvents
            .AsNoTracking()
            .Where(e => e.GameServerId == gameServerId
                && (e.EventType == ChatCommandExecutionEventType || e.EventType == ChatCommandDeniedEventType)
                && e.Timestamp >= fromUtc && e.Timestamp < toUtc
                && e.EventData != null)
            .Select(e => new { e.EventType, EventData = e.EventData! })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var executed = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var denied = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var payload in payloads)
        {
            var command = ExtractCommandPrefix(payload.EventData);
            if (string.IsNullOrWhiteSpace(command))
            {
                continue;
            }

            var target = payload.EventType == ChatCommandDeniedEventType ? denied : executed;
            target[command] = target.TryGetValue(command, out var existing) ? existing + 1 : 1;
        }

        var commands = executed.Keys
            .Union(denied.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(command =>
            {
                executed.TryGetValue(command, out var executedCount);
                denied.TryGetValue(command, out var deniedCount);
                return new ServerChatCommandCountDto
                {
                    Command = command,
                    Count = executedCount,
                    DeniedCount = deniedCount
                };
            })
            .OrderByDescending(c => c.Count + c.DeniedCount)
            .ThenBy(c => c.Command, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToList();

        var dto = new ServerChatCommandsSummaryDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            TotalExecutions = executed.Values.Sum(),
            TotalDenied = denied.Values.Sum(),
            Commands = commands
        };

        return new ApiResponse<ServerChatCommandsSummaryDto>(dto).ToApiResult();
    }

    [HttpGet("{gameServerId:guid}/map-rotation-performance")]
    [ProducesResponseType<ServerMapRotationPerformanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMapRotationPerformance(
        Guid gameServerId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IServerAnalyticsApi)this).GetMapRotationPerformance(gameServerId, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ServerMapRotationPerformanceDto>> IServerAnalyticsApi.GetMapRotationPerformance(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<ServerMapRotationPerformanceDto>(HttpStatusCode.BadRequest);
        }

        var serverExists = await context.GameServers
            .AsNoTracking()
            .AnyAsync(gs => gs.GameServerId == gameServerId && !gs.Deleted, cancellationToken).ConfigureAwait(false);

        if (!serverExists)
        {
            return new ApiResult<ServerMapRotationPerformanceDto>(HttpStatusCode.NotFound);
        }

        var samples = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.GameServerId == gameServerId && s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.MapName != null && s.MapName != "")
            .Select(s => new { s.MapName, s.PlayerCount })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var totalSamples = samples.Count;
        var maps = samples
            .GroupBy(s => s.MapName)
            .Select(g => new ServerMapPerformanceItemDto
            {
                MapName = g.Key,
                SampleCount = g.Count(),
                AvgPlayers = Math.Round(g.Average(x => (double)x.PlayerCount), 2),
                PeakPlayers = g.Max(x => x.PlayerCount),
                SharePercent = totalSamples == 0 ? 0 : Math.Round((double)g.Count() / totalSamples * 100d, 2)
            })
            .OrderByDescending(x => x.SampleCount)
            .ToList();

        var dto = new ServerMapRotationPerformanceDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Maps = maps
        };

        return new ApiResponse<ServerMapRotationPerformanceDto>(dto).ToApiResult();
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

    /// <summary>
    /// Extracts the chat command prefix (e.g. "!like") from a ChatCommandExecution event payload.
    /// The payload is serialised by the events processor with a camelCase "commandPrefix" property;
    /// matching is case-insensitive to tolerate naming-policy changes. Returns null for malformed
    /// payloads or payloads without a string command prefix.
    /// </summary>
    private static string? ExtractCommandPrefix(string eventData)
    {
        try
        {
            using var document = JsonDocument.Parse(eventData);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, "commandPrefix", StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // Malformed payload — exclude from the breakdown rather than failing the whole request.
        }

        return null;
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