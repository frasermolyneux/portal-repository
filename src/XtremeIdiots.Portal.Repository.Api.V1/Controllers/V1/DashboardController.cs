using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;
using XtremeIdiots.Portal.Repository.Api.V1.Services;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

/// <summary>
/// Controller for dashboard aggregate data, providing pre-computed summaries
/// for the admin dashboard without requiring multiple round-trips. Delegates
/// to <see cref="IDashboardService"/> so cache-aside behaviour can be injected
/// via a decorator without touching controller logic.
/// </summary>
[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public class DashboardController : ControllerBase, IDashboardApi
{
    private readonly IDashboardService dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        ArgumentNullException.ThrowIfNull(dashboardService);
        this.dashboardService = dashboardService;
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

    Task<ApiResult<DashboardSummaryDto>> IDashboardApi.GetDashboardSummary(CancellationToken cancellationToken)
        => dashboardService.GetDashboardSummaryAsync(cancellationToken);

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

    Task<ApiResult<CollectionModel<AdminLeaderboardEntryDto>>> IDashboardApi.GetAdminLeaderboard(int days, CancellationToken cancellationToken)
        => dashboardService.GetAdminLeaderboardAsync(days, cancellationToken);

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

    Task<ApiResult<CollectionModel<ModerationTrendDataPointDto>>> IDashboardApi.GetModerationTrend(int days, CancellationToken cancellationToken)
        => dashboardService.GetModerationTrendAsync(days, cancellationToken);

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

    Task<ApiResult<ServerUtilizationCollectionDto>> IDashboardApi.GetServerUtilization(CancellationToken cancellationToken)
        => dashboardService.GetServerUtilizationAsync(cancellationToken);
}
