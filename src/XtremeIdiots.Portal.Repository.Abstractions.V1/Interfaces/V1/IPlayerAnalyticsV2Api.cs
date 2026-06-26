using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IPlayerAnalyticsV2Api
{
    Task<ApiResult<PlayerOverviewDto>> GetOverview(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<ApiResult<PlayerTrendsDto>> GetTrends(Guid playerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default);
    Task<ApiResult<PlayerRelatedActivityDto>> GetRelatedActivity(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<ApiResult<PlayerModerationSummaryDto>> GetModerationSummary(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}