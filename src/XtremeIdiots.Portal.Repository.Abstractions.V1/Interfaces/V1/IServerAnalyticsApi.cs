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
        CancellationToken cancellationToken = default);

    Task<ApiResult<ServerPlayersCurrentDto>> GetPlayersCurrent(Guid gameServerId, CancellationToken cancellationToken = default);

    Task<ApiResult<ServerEventsSummaryDto>> GetEventsSummary(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<ApiResult<ServerChatSummaryDto>> GetChatSummary(Guid gameServerId, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);

    Task<ApiResult<ServerMapRotationPerformanceDto>> GetMapRotationPerformance(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}