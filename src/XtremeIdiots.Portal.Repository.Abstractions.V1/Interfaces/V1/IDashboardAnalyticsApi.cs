using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IDashboardAnalyticsApi
{
    Task<ApiResult<DashboardHomeDto>> GetHome(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);

    Task<ApiResult<DashboardServerDto>> GetServer(Guid gameServerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default);
}