using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IMapAnalyticsApi
{
    Task<ApiResult<MapOverviewDto>> GetOverview(Guid mapId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<ApiResult<MapTrendsDto>> GetTrends(Guid mapId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default);
    Task<ApiResult<MapRankingsDto>> GetRankings(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default);
}