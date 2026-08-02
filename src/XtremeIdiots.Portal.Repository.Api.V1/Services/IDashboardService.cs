using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// Server-side seam for dashboard aggregations, extracted from
    /// <c>DashboardController</c> so caching can be layered as a decorator.
    /// </summary>
    public interface IDashboardService
    {
        Task<ApiResult<DashboardSummaryDto>> GetDashboardSummaryAsync(CancellationToken cancellationToken);
        Task<ApiResult<CollectionModel<AdminLeaderboardEntryDto>>> GetAdminLeaderboardAsync(int days, CancellationToken cancellationToken);
        Task<ApiResult<CollectionModel<ModerationTrendDataPointDto>>> GetModerationTrendAsync(int days, CancellationToken cancellationToken);
        Task<ApiResult<ServerUtilizationCollectionDto>> GetServerUtilizationAsync(CancellationToken cancellationToken);
    }
}
