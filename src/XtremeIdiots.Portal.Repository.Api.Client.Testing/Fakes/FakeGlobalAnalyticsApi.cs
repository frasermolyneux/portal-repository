using System.Net;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Global;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeGlobalAnalyticsApi : IGlobalAnalyticsApi
{
    private GlobalOverviewDto _overview = new();
    private GlobalTimeseriesDto _timeseries = new();
    private GlobalGameBreakdownDto _gameBreakdown = new();
    private GlobalServerBreakdownDto _serverBreakdown = new();
    private GlobalPlayerActivityDto _playerActivity = new();
    private GlobalGeoDistributionDto _geoDistribution = new();
    private GlobalModerationDto _moderation = new();

    public FakeGlobalAnalyticsApi SetOverview(GlobalOverviewDto overview) { _overview = overview; return this; }
    public FakeGlobalAnalyticsApi SetTimeseries(GlobalTimeseriesDto timeseries) { _timeseries = timeseries; return this; }
    public FakeGlobalAnalyticsApi SetGameBreakdown(GlobalGameBreakdownDto gameBreakdown) { _gameBreakdown = gameBreakdown; return this; }
    public FakeGlobalAnalyticsApi SetServerBreakdown(GlobalServerBreakdownDto serverBreakdown) { _serverBreakdown = serverBreakdown; return this; }
    public FakeGlobalAnalyticsApi SetPlayerActivity(GlobalPlayerActivityDto playerActivity) { _playerActivity = playerActivity; return this; }
    public FakeGlobalAnalyticsApi SetGeoDistribution(GlobalGeoDistributionDto geoDistribution) { _geoDistribution = geoDistribution; return this; }
    public FakeGlobalAnalyticsApi SetModeration(GlobalModerationDto moderation) { _moderation = moderation; return this; }

    public FakeGlobalAnalyticsApi Reset()
    {
        _overview = new();
        _timeseries = new();
        _gameBreakdown = new();
        _serverBreakdown = new();
        _playerActivity = new();
        _geoDistribution = new();
        _moderation = new();
        return this;
    }

    public Task<ApiResult<GlobalOverviewDto>> GetOverview(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<GlobalOverviewDto>(HttpStatusCode.OK, new ApiResponse<GlobalOverviewDto>(_overview)));
    }

    public Task<ApiResult<GlobalTimeseriesDto>> GetTimeseries(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<GlobalTimeseriesDto>(HttpStatusCode.OK, new ApiResponse<GlobalTimeseriesDto>(_timeseries)));
    }

    public Task<ApiResult<GlobalGameBreakdownDto>> GetGameBreakdown(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<GlobalGameBreakdownDto>(HttpStatusCode.OK, new ApiResponse<GlobalGameBreakdownDto>(_gameBreakdown)));
    }

    public Task<ApiResult<GlobalServerBreakdownDto>> GetServerBreakdown(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<GlobalServerBreakdownDto>(HttpStatusCode.OK, new ApiResponse<GlobalServerBreakdownDto>(_serverBreakdown)));
    }

    public Task<ApiResult<GlobalPlayerActivityDto>> GetPlayerActivity(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<GlobalPlayerActivityDto>(HttpStatusCode.OK, new ApiResponse<GlobalPlayerActivityDto>(_playerActivity)));
    }

    public Task<ApiResult<GlobalGeoDistributionDto>> GetGeoDistribution(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<GlobalGeoDistributionDto>(HttpStatusCode.OK, new ApiResponse<GlobalGeoDistributionDto>(_geoDistribution)));
    }

    public Task<ApiResult<GlobalModerationDto>> GetModeration(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<GlobalModerationDto>(HttpStatusCode.OK, new ApiResponse<GlobalModerationDto>(_moderation)));
    }
}
