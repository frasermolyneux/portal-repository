using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Global;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IGlobalAnalyticsApi
{
    Task<ApiResult<GlobalOverviewDto>> GetOverview(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<ApiResult<GlobalTimeseriesDto>> GetTimeseries(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default);
    Task<ApiResult<GlobalGameBreakdownDto>> GetGameBreakdown(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);
    Task<ApiResult<GlobalServerBreakdownDto>> GetServerBreakdown(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);
    Task<ApiResult<GlobalPlayerActivityDto>> GetPlayerActivity(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);
    Task<ApiResult<GlobalGeoDistributionDto>> GetGeoDistribution(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);
    Task<ApiResult<GlobalModerationDto>> GetModeration(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}