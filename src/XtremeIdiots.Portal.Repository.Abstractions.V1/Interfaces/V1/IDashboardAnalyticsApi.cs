using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IDashboardAnalyticsApi
{
    Task<ApiResult<DashboardSummaryDto>> GetSummary(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<ApiResult<DashboardTrendsDto>> GetTrends(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default);
    Task<ApiResult<DashboardCompositionDto>> GetComposition(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);
}