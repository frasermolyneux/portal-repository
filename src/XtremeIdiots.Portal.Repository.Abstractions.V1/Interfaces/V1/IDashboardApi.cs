using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IDashboardApi
{
    Task<ApiResult<DashboardSummaryDto>> GetDashboardSummary(CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<AdminLeaderboardEntryDto>>> GetAdminLeaderboard(int days, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<ModerationTrendDataPointDto>>> GetModerationTrend(int days, CancellationToken cancellationToken = default);
    Task<ApiResult<ServerUtilizationCollectionDto>> GetServerUtilization(CancellationToken cancellationToken = default);
}
