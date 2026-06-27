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
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Games;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}/analytics/games")]
public class GameAnalyticsController : ControllerBase, IGameAnalyticsApi
{
    private readonly PortalDbContext context;

    public GameAnalyticsController(PortalDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.context = context;
    }

    [HttpGet("overview")]
    [ProducesResponseType<GameOverviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOverview(
        [FromQuery] GameType gameType,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IGameAnalyticsApi)this).GetOverview(gameType, fromUtc, toUtc, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<GameOverviewDto>> IGameAnalyticsApi.GetOverview(GameType gameType, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _))
        {
            return new ApiResult<GameOverviewDto>(HttpStatusCode.BadRequest);
        }

        var gameTypeInt = gameType.ToGameTypeInt();

        var serverCount = await context.GameServers
            .AsNoTracking()
            .CountAsync(gs => !gs.Deleted && gs.GameType == gameTypeInt, cancellationToken).ConfigureAwait(false);

        var stats = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc && s.Timestamp < toUtc && s.GameServer != null && !s.GameServer.Deleted && s.GameServer.GameType == gameTypeInt)
            .Select(s => s.PlayerCount)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var uniquePlayers = await context.Players
            .AsNoTracking()
            .CountAsync(p => p.GameType == gameTypeInt && p.LastSeen >= fromUtc && p.LastSeen < toUtc, cancellationToken).ConfigureAwait(false);

        var moderationActions = await context.AdminActions
            .AsNoTracking()
            .CountAsync(a => a.Created >= fromUtc && a.Created < toUtc && a.Player.GameType == gameTypeInt, cancellationToken).ConfigureAwait(false);

        var dto = new GameOverviewDto
        {
            GameType = gameType,
            Window = CreateWindow(fromUtc, toUtc),
            ServerCount = serverCount,
            AvgPlayers = stats.Count == 0 ? 0 : Math.Round(stats.Average(), 2),
            PeakPlayers = stats.Count == 0 ? 0 : stats.Max(),
            UniquePlayers = uniquePlayers,
            ModerationActions = moderationActions
        };

        return new ApiResponse<GameOverviewDto>(dto).ToApiResult();
    }

    [HttpGet("timeseries")]
    [ProducesResponseType<GameTimeseriesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTimeseries(
        [FromQuery] GameType gameType,
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
        var response = await ((IGameAnalyticsApi)this).GetTimeseries(
            gameType,
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

    async Task<ApiResult<GameTimeseriesDto>> IGameAnalyticsApi.GetTimeseries(GameType gameType, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken)
    {
        return await ((IGameAnalyticsApi)this).GetTimeseries(
            gameType,
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

    async Task<ApiResult<GameTimeseriesDto>> IGameAnalyticsApi.GetTimeseries(
        GameType gameType,
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
            return new ApiResult<GameTimeseriesDto>(HttpStatusCode.BadRequest);
        }

        var gameTypeInt = gameType.ToGameTypeInt();

        var stats = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= alignedFromUtc
                && s.Timestamp < alignedToUtc
                && s.GameServer != null
                && !s.GameServer.Deleted
                && s.GameServer.GameType == gameTypeInt)
            .Select(s => new { s.Timestamp, s.PlayerCount })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var events = await context.GameServerEvents
            .AsNoTracking()
            .Where(e => e.Timestamp >= alignedFromUtc
                && e.Timestamp < alignedToUtc
                && !e.GameServer.Deleted
                && e.GameServer.GameType == gameTypeInt)
            .Select(e => e.Timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var chat = await context.ChatMessages
            .AsNoTracking()
            .Where(c => c.Timestamp >= alignedFromUtc
                && c.Timestamp < alignedToUtc
                && c.GameServer.GameType == gameTypeInt)
            .Select(c => c.Timestamp)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var statsByBucket = stats
            .GroupBy(s => TruncateToBucket(s.Timestamp, bucket))
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    AvgPlayers = g.Average(x => (double)x.PlayerCount),
                    EventsCount = 0,
                    ChatCount = 0
                });

        var eventsByBucket = events.GroupBy(t => TruncateToBucket(t, bucket)).ToDictionary(g => g.Key, g => g.Count());
        var chatByBucket = chat.GroupBy(t => TruncateToBucket(t, bucket)).ToDictionary(g => g.Key, g => g.Count());

        var bucketStarts = BuildBuckets(alignedFromUtc, alignedToUtc, bucket);
        var points = bucketStarts.Select(start => new GameTimeseriesPointDto
        {
            BucketStartUtc = start,
            AvgPlayers = statsByBucket.TryGetValue(start, out var s) ? Math.Round(s.AvgPlayers, 2) : 0,
            EventsCount = eventsByBucket.GetValueOrDefault(start, 0),
            ChatCount = chatByBucket.GetValueOrDefault(start, 0)
        }).ToList();

        var labels = points.Select(p => p.BucketStartUtc).ToList();
        var series = new List<AnalyticsSeriesDto>
        {
            BuildSeries("avgPlayers", "Average Players", labels, points.Select(p => p.AvgPlayers)),
            BuildSeries("events", "Events", labels, points.Select(p => (double)p.EventsCount)),
            BuildSeries("chat", "Chat", labels, points.Select(p => (double)p.ChatCount))
        };

        var currentTotal = points.Sum(p => p.AvgPlayers);
        var summary = compareMode == AnalyticsCompareMode.None
            ? null
            : BuildComparisonSummary(currentTotal, comparePeriods);

        var dto = new GameTimeseriesDto
        {
            GameType = gameType,
            Bucket = bucket,
            Points = points,
            Labels = labels,
            Series = series,
            Summary = summary,
            Meta = BuildCompareMeta(compareMode, comparePeriods, alignMode, timezone, normalize)
        };

        return new ApiResponse<GameTimeseriesDto>(dto).ToApiResult();
    }

    [HttpGet("servers")]
    [ProducesResponseType<GameServerBreakdownDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetServerBreakdown(
        [FromQuery] GameType gameType,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IGameAnalyticsApi)this).GetServerBreakdown(gameType, fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<GameServerBreakdownDto>> IGameAnalyticsApi.GetServerBreakdown(GameType gameType, DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<GameServerBreakdownDto>(HttpStatusCode.BadRequest);
        }

        var gameTypeInt = gameType.ToGameTypeInt();

        var grouped = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc
                && s.Timestamp < toUtc
                && s.GameServerId != null
                && s.GameServer != null
                && !s.GameServer.Deleted
                && s.GameServer.GameType == gameTypeInt)
            .GroupBy(s => new { GameServerId = s.GameServerId!.Value, s.GameServer!.Title })
            .Select(g => new
            {
                g.Key.GameServerId,
                g.Key.Title,
                AvgPlayers = g.Average(x => (double)x.PlayerCount),
                PeakPlayers = g.Max(x => x.PlayerCount)
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var eventCounts = await context.GameServerEvents
            .AsNoTracking()
            .Where(e => e.Timestamp >= fromUtc && e.Timestamp < toUtc && !e.GameServer.Deleted && e.GameServer.GameType == gameTypeInt)
            .GroupBy(e => e.GameServerId)
            .Select(g => new { GameServerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GameServerId, x => x.Count, cancellationToken).ConfigureAwait(false);

        var items = grouped
            .OrderByDescending(x => x.AvgPlayers)
            .Take(top)
            .Select(x => new GameServerBreakdownItemDto
            {
                GameServerId = x.GameServerId,
                Title = x.Title,
                AvgPlayers = Math.Round(x.AvgPlayers, 2),
                PeakPlayers = x.PeakPlayers,
                EventsCount = eventCounts.GetValueOrDefault(x.GameServerId, 0)
            })
            .ToList();

        var dto = new GameServerBreakdownDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<GameServerBreakdownDto>(dto).ToApiResult();
    }

    [HttpGet("players")]
    [ProducesResponseType<GamePlayerBreakdownDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPlayerBreakdown(
        [FromQuery] GameType gameType,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IGameAnalyticsApi)this).GetPlayerBreakdown(gameType, fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<GamePlayerBreakdownDto>> IGameAnalyticsApi.GetPlayerBreakdown(GameType gameType, DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<GamePlayerBreakdownDto>(HttpStatusCode.BadRequest);
        }

        var gameTypeInt = gameType.ToGameTypeInt();

        var grouped = await context.ChatMessages
            .AsNoTracking()
            .Where(c => c.Timestamp >= fromUtc
                && c.Timestamp < toUtc
                && c.Player.GameType == gameTypeInt)
            .GroupBy(c => new { c.PlayerId, c.Username })
            .Select(g => new
            {
                g.Key.PlayerId,
                DisplayName = g.Key.Username,
                ActivityCount = g.Count(),
                LastSeenUtc = g.Max(x => x.Timestamp)
            })
            .OrderByDescending(x => x.ActivityCount)
            .Take(top)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var items = grouped.Select(x => new GamePlayerBreakdownItemDto
        {
            PlayerId = x.PlayerId,
            DisplayName = string.IsNullOrWhiteSpace(x.DisplayName) ? "Unknown" : x.DisplayName,
            ActivityCount = x.ActivityCount,
            LastSeenUtc = x.LastSeenUtc
        }).ToList();

        var dto = new GamePlayerBreakdownDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<GamePlayerBreakdownDto>(dto).ToApiResult();
    }

    [HttpGet("maps")]
    [ProducesResponseType<GameMapBreakdownDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMapBreakdown(
        [FromQuery] GameType gameType,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int top = AnalyticsQueryDefaults.DefaultTop,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IGameAnalyticsApi)this).GetMapBreakdown(gameType, fromUtc, toUtc, top, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<GameMapBreakdownDto>> IGameAnalyticsApi.GetMapBreakdown(GameType gameType, DateTime fromUtc, DateTime toUtc, int top, CancellationToken cancellationToken)
    {
        if (!AnalyticsQueryValidator.TryValidateWindow(fromUtc, toUtc, out _)
            || !AnalyticsQueryValidator.TryValidateTop(top, out _))
        {
            return new ApiResult<GameMapBreakdownDto>(HttpStatusCode.BadRequest);
        }

        var gameTypeInt = gameType.ToGameTypeInt();

        var plays = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= fromUtc
                && s.Timestamp < toUtc
                && s.GameServer != null
                && !s.GameServer.Deleted
                && s.GameServer.GameType == gameTypeInt)
            .GroupBy(s => s.MapName)
            .Select(g => new
            {
                MapName = g.Key,
                Plays = g.Count()
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var mapsByName = await context.Maps
            .AsNoTracking()
            .Where(m => m.GameType == gameTypeInt && m.MapName != null)
            .ToDictionaryAsync(m => m.MapName!, m => m, cancellationToken).ConfigureAwait(false);

        var mapIds = mapsByName.Values.Select(m => m.MapId).ToList();
        var voteGroups = await context.MapVotes
            .AsNoTracking()
            .Where(v => mapIds.Contains(v.MapId) && v.Timestamp >= fromUtc && v.Timestamp < toUtc)
            .GroupBy(v => new { v.MapId, v.Like })
            .Select(g => new { g.Key.MapId, g.Key.Like, Count = g.Count() })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var votesByMap = voteGroups
            .GroupBy(x => x.MapId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Up = g.Where(x => x.Like).Sum(x => x.Count),
                    Down = g.Where(x => !x.Like).Sum(x => x.Count)
                });

        var items = plays
            .OrderByDescending(x => x.Plays)
            .Take(top)
            .Select(x =>
            {
                mapsByName.TryGetValue(x.MapName ?? string.Empty, out var map);
                var voteData = map != null && votesByMap.TryGetValue(map.MapId, out var v) ? v : null;
                var up = voteData?.Up ?? 0;
                var down = voteData?.Down ?? 0;
                var score = up + down == 0 ? 0 : Math.Round((double)(up - down) / (up + down), 2);

                return new GameMapBreakdownItemDto
                {
                    MapId = map?.MapId,
                    MapName = x.MapName ?? "Unknown",
                    Plays = x.Plays,
                    UpVotes = up,
                    DownVotes = down,
                    Score = score
                };
            })
            .ToList();

        var dto = new GameMapBreakdownDto
        {
            Window = CreateWindow(fromUtc, toUtc),
            Items = items
        };

        return new ApiResponse<GameMapBreakdownDto>(dto).ToApiResult();
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