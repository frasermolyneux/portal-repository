using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IDashboardAnalyticsApi
{
    Task<ApiResult<DashboardHomeDto>> GetHome(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);

    Task<ApiResult<DashboardHomeDto>> GetHome(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, int top, bool includeComposition, CancellationToken cancellationToken)
    {
        return includeComposition
            ? GetHome(fromUtc, toUtc, bucket, top, cancellationToken)
            : throw new NotSupportedException("This dashboard analytics implementation does not support excluding composition.");
    }

    Task<ApiResult<DashboardServerDto>> GetServer(Guid gameServerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default);
}
