using System.Net;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeMapAnalyticsApi : IMapAnalyticsApi
{
    private MapOverviewDto _overview = new();
    private MapTrendsDto _trends = new();
    private MapRankingsDto _rankings = new();

    public FakeMapAnalyticsApi SetOverview(MapOverviewDto overview) { _overview = overview; return this; }
    public FakeMapAnalyticsApi SetTrends(MapTrendsDto trends) { _trends = trends; return this; }
    public FakeMapAnalyticsApi SetRankings(MapRankingsDto rankings) { _rankings = rankings; return this; }

    public FakeMapAnalyticsApi Reset()
    {
        _overview = new();
        _trends = new();
        _rankings = new();
        return this;
    }

    public Task<ApiResult<MapOverviewDto>> GetOverview(Guid mapId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<MapOverviewDto>(HttpStatusCode.OK, new ApiResponse<MapOverviewDto>(_overview)));
    }

    public Task<ApiResult<MapTrendsDto>> GetTrends(Guid mapId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<MapTrendsDto>(HttpStatusCode.OK, new ApiResponse<MapTrendsDto>(_trends)));
    }

    public Task<ApiResult<MapRankingsDto>> GetRankings(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<MapRankingsDto>(HttpStatusCode.OK, new ApiResponse<MapRankingsDto>(_rankings)));
    }
}
