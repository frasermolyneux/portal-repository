using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Servers;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IServerAnalyticsApi
{
    Task<ApiResult<ServerOverviewDto>> GetOverview(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<ApiResult<ServerTimeseriesDto>> GetTimeseries(Guid gameServerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default);
    Task<ApiResult<ServerTimeseriesDto>> GetTimeseries(
        Guid gameServerId,
        DateTime fromUtc,
        DateTime toUtc,
        AnalyticsBucket bucket,
        AnalyticsCompareMode compareMode,
        int comparePeriods = AnalyticsQueryDefaults.DefaultComparePeriods,
        AnalyticsAlignMode alignMode = AnalyticsAlignMode.None,
        string timezone = "UTC",
        bool normalize = false,
        CancellationToken cancellationToken = default)
    {
        return GetTimeseries(gameServerId, fromUtc, toUtc, bucket, cancellationToken);
    }

    Task<ApiResult<ServerSummaryDto>> GetSummary(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}