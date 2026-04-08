using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

/// <summary>
/// Controller for dashboard aggregate data, providing pre-computed summaries
/// for the admin dashboard without requiring multiple round-trips.
/// </summary>
[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public class DashboardController : ControllerBase, IDashboardApi
{
    private readonly PortalDbContext context;

    public DashboardController(PortalDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.context = context;
    }

    /// <summary>
    /// Returns an aggregated summary of server health, player counts, unclaimed bans,
    /// open reports, and recent admin action counts.
    /// </summary>
    [HttpGet("dashboard/summary")]
    [ProducesResponseType<DashboardSummaryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken = default)
    {
        var response = await ((IDashboardApi)this).GetDashboardSummary(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<DashboardSummaryDto>> IDashboardApi.GetDashboardSummary(CancellationToken cancellationToken)
    {
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
        var oneDayAgo = DateTime.UtcNow.AddDays(-1);
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // Server health
        var servers = await context.GameServers
            .AsNoTracking()
            .Where(gs => !gs.Deleted && gs.AgentEnabled)
            .Select(gs => new { gs.LiveCurrentPlayers, gs.LiveLastUpdated })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var totalServers = servers.Count;
        var onlineCount = servers.Count(s => s.LiveLastUpdated >= fiveMinutesAgo);
        var totalPlayers = servers.Sum(s => s.LiveCurrentPlayers ?? 0);

        // Unclaimed ban count: permanent bans (Type == Ban) with no admin assigned
        var unclaimedBanCount = await context.AdminActions
            .AsNoTracking()
            .CountAsync(a => a.Type == (int)AdminActionType.Ban
                          && a.UserProfileId == null, cancellationToken).ConfigureAwait(false);

        // Open reports
        var openReportCount = await context.Reports
            .AsNoTracking()
            .CountAsync(r => !r.Closed, cancellationToken).ConfigureAwait(false);

        // Recent admin actions (24h)
        var actions24h = await GetActionCountsSince(oneDayAgo, cancellationToken).ConfigureAwait(false);

        // Recent admin actions (7d)
        var actions7d = await GetActionCountsSince(sevenDaysAgo, cancellationToken).ConfigureAwait(false);

        var dto = new DashboardSummaryDto
        {
            TotalServers = totalServers,
            OnlineServerCount = onlineCount,
            OfflineServerCount = totalServers - onlineCount,
            TotalPlayersOnline = totalPlayers,
            UnclaimedBanCount = unclaimedBanCount,
            OpenReportCount = openReportCount,
            RecentActions24h = actions24h,
            RecentActions7d = actions7d
        };

        return new ApiResponse<DashboardSummaryDto>(dto).ToApiResult();
    }

    /// <summary>
    /// Returns admin activity leaderboard showing moderation action counts per admin
    /// over the specified number of days.
    /// </summary>
    [HttpGet("dashboard/admin-leaderboard")]
    [ProducesResponseType<CollectionModel<AdminLeaderboardEntryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminLeaderboard([FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        var response = await ((IDashboardApi)this).GetAdminLeaderboard(days, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<CollectionModel<AdminLeaderboardEntryDto>>> IDashboardApi.GetAdminLeaderboard(int days, CancellationToken cancellationToken)
    {
        if (days <= 0) days = 30;

        var cutoff = DateTime.UtcNow.AddDays(-days);

        var leaderboard = await context.AdminActions
            .AsNoTracking()
            .Where(a => a.Created >= cutoff && a.UserProfileId != null)
            .GroupBy(a => a.UserProfileId!.Value)
            .Select(g => new
            {
                AdminId = g.Key,
                Bans = g.Count(a => a.Type == (int)AdminActionType.Ban),
                TempBans = g.Count(a => a.Type == (int)AdminActionType.TempBan),
                Kicks = g.Count(a => a.Type == (int)AdminActionType.Kick),
                Warnings = g.Count(a => a.Type == (int)AdminActionType.Warning),
                Observations = g.Count(a => a.Type == (int)AdminActionType.Observation),
                Total = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        // Fetch display names for the admins
        var adminIds = leaderboard.Select(x => x.AdminId).ToList();
        var adminProfiles = await context.UserProfiles
            .AsNoTracking()
            .Where(up => adminIds.Contains(up.UserProfileId))
            .Select(up => new { up.UserProfileId, up.DisplayName })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var profileLookup = adminProfiles.ToDictionary(x => x.UserProfileId, x => x.DisplayName ?? "Unknown");

        var entries = leaderboard.Select(x => new AdminLeaderboardEntryDto
        {
            AdminId = x.AdminId,
            DisplayName = profileLookup.GetValueOrDefault(x.AdminId, "Unknown"),
            Bans = x.Bans,
            TempBans = x.TempBans,
            Kicks = x.Kicks,
            Warnings = x.Warnings,
            Observations = x.Observations,
            Total = x.Total
        }).ToList();

        var result = new CollectionModel<AdminLeaderboardEntryDto> { Items = entries };

        return new ApiResponse<CollectionModel<AdminLeaderboardEntryDto>>(result)
        {
            Pagination = new ApiPagination(entries.Count, entries.Count, 0, entries.Count)
        }.ToApiResult();
    }

    /// <summary>
    /// Returns daily moderation action counts over the specified number of days,
    /// suitable for sparklines or trend charts.
    /// </summary>
    [HttpGet("dashboard/moderation-trend")]
    [ProducesResponseType<CollectionModel<ModerationTrendDataPointDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModerationTrend([FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        var response = await ((IDashboardApi)this).GetModerationTrend(days, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<CollectionModel<ModerationTrendDataPointDto>>> IDashboardApi.GetModerationTrend(int days, CancellationToken cancellationToken)
    {
        if (days <= 0) days = 30;

        var cutoff = DateTime.UtcNow.AddDays(-days);

        var dailyCounts = await context.AdminActions
            .AsNoTracking()
            .Where(a => a.Created >= cutoff)
            .GroupBy(a => a.Created.Date)
            .Select(g => new
            {
                Date = g.Key,
                Bans = g.Count(a => a.Type == (int)AdminActionType.Ban),
                TempBans = g.Count(a => a.Type == (int)AdminActionType.TempBan),
                Kicks = g.Count(a => a.Type == (int)AdminActionType.Kick),
                Warnings = g.Count(a => a.Type == (int)AdminActionType.Warning),
                Observations = g.Count(a => a.Type == (int)AdminActionType.Observation)
            })
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var entries = dailyCounts.Select(x => new ModerationTrendDataPointDto
        {
            Date = x.Date,
            Bans = x.Bans,
            TempBans = x.TempBans,
            Kicks = x.Kicks,
            Warnings = x.Warnings,
            Observations = x.Observations
        }).ToList();

        var result = new CollectionModel<ModerationTrendDataPointDto> { Items = entries };

        return new ApiResponse<CollectionModel<ModerationTrendDataPointDto>>(result)
        {
            Pagination = new ApiPagination(entries.Count, entries.Count, 0, entries.Count)
        }.ToApiResult();
    }

    /// <summary>
    /// Returns per-server utilization data (average and peak player counts)
    /// computed from the last 24 hours of server stats.
    /// </summary>
    [HttpGet("dashboard/server-utilization")]
    [ProducesResponseType<ServerUtilizationCollectionDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServerUtilization(CancellationToken cancellationToken = default)
    {
        var response = await ((IDashboardApi)this).GetServerUtilization(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<ServerUtilizationCollectionDto>> IDashboardApi.GetServerUtilization(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        // Get all agent-enabled servers
        var servers = await context.GameServers
            .AsNoTracking()
            .Where(gs => !gs.Deleted && gs.AgentEnabled)
            .Select(gs => new
            {
                gs.GameServerId,
                gs.Title,
                gs.LiveTitle,
                gs.GameType,
                gs.LiveMaxPlayers
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        // Get aggregated stats per server for the last 24 hours
        var statsAggregates = await context.GameServerStats
            .AsNoTracking()
            .Where(s => s.Timestamp >= cutoff && s.GameServerId != null)
            .GroupBy(s => s.GameServerId!.Value)
            .Select(g => new
            {
                ServerId = g.Key,
                AvgPlayers = g.Average(s => (double)s.PlayerCount),
                PeakPlayers = g.Max(s => s.PlayerCount)
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var statsLookup = statsAggregates.ToDictionary(x => x.ServerId);

        var serverDtos = servers.Select(gs =>
        {
            var hasStats = statsLookup.TryGetValue(gs.GameServerId, out var stats);
            var maxPlayers = gs.LiveMaxPlayers ?? 0;
            var avg = hasStats ? stats!.AvgPlayers : 0;

            return new ServerUtilizationDto
            {
                ServerId = gs.GameServerId,
                Title = string.IsNullOrWhiteSpace(gs.LiveTitle) ? gs.Title : gs.LiveTitle,
                GameType = gs.GameType.ToGameType().ToString(),
                AvgPlayers = Math.Round(avg, 1),
                PeakPlayers = hasStats ? stats!.PeakPlayers : 0,
                MaxPlayers = maxPlayers,
                Utilization = maxPlayers > 0 ? Math.Round(avg / maxPlayers, 3) : 0
            };
        }).ToList();

        var dto = new ServerUtilizationCollectionDto
        {
            Servers = serverDtos,
            TotalAvgPlayers = Math.Round(serverDtos.Sum(s => s.AvgPlayers), 1),
            TotalPeakPlayers = serverDtos.Sum(s => s.PeakPlayers)
        };

        return new ApiResponse<ServerUtilizationCollectionDto>(dto).ToApiResult();
    }

    private async Task<AdminActionCountsDto> GetActionCountsSince(DateTime since, CancellationToken cancellationToken)
    {
        var counts = await context.AdminActions
            .AsNoTracking()
            .Where(a => a.Created >= since)
            .GroupBy(a => a.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new AdminActionCountsDto
        {
            Bans = counts.FirstOrDefault(c => c.Type == (int)AdminActionType.Ban)?.Count ?? 0,
            TempBans = counts.FirstOrDefault(c => c.Type == (int)AdminActionType.TempBan)?.Count ?? 0,
            Kicks = counts.FirstOrDefault(c => c.Type == (int)AdminActionType.Kick)?.Count ?? 0,
            Warnings = counts.FirstOrDefault(c => c.Type == (int)AdminActionType.Warning)?.Count ?? 0,
            Observations = counts.FirstOrDefault(c => c.Type == (int)AdminActionType.Observation)?.Count ?? 0
        };
    }
}
