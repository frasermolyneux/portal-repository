using System.Net;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Games;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeGameAnalyticsApi : IGameAnalyticsApi
{
    private GameOverviewDto _overview = new();
    private GameTimeseriesDto _timeseries = new();
    private GameServerBreakdownDto _serverBreakdown = new();
    private GamePlayerBreakdownDto _playerBreakdown = new();
    private GameMapBreakdownDto _mapBreakdown = new();

    public FakeGameAnalyticsApi SetOverview(GameOverviewDto overview) { _overview = overview; return this; }
    public FakeGameAnalyticsApi SetTimeseries(GameTimeseriesDto timeseries) { _timeseries = timeseries; return this; }
    public FakeGameAnalyticsApi SetServerBreakdown(GameServerBreakdownDto serverBreakdown) { _serverBreakdown = serverBreakdown; return this; }
    public FakeGameAnalyticsApi SetPlayerBreakdown(GamePlayerBreakdownDto playerBreakdown) { _playerBreakdown = playerBreakdown; return this; }
    public FakeGameAnalyticsApi SetMapBreakdown(GameMapBreakdownDto mapBreakdown) { _mapBreakdown = mapBreakdown; return this; }

    public FakeGameAnalyticsApi Reset()
    {
        _overview = new();
        _timeseries = new();
        _serverBreakdown = new();
        _playerBreakdown = new();
        _mapBreakdown = new();
        return this;
    }

    public Task<ApiResult<GameOverviewDto>> GetOverview(GameType gameType, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<GameOverviewDto>(HttpStatusCode.OK, new ApiResponse<GameOverviewDto>(_overview)));
    }

    public Task<ApiResult<GameTimeseriesDto>> GetTimeseries(GameType gameType, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<GameTimeseriesDto>(HttpStatusCode.OK, new ApiResponse<GameTimeseriesDto>(_timeseries)));
    }

    public Task<ApiResult<GameServerBreakdownDto>> GetServerBreakdown(GameType gameType, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<GameServerBreakdownDto>(HttpStatusCode.OK, new ApiResponse<GameServerBreakdownDto>(_serverBreakdown)));
    }

    public Task<ApiResult<GamePlayerBreakdownDto>> GetPlayerBreakdown(GameType gameType, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<GamePlayerBreakdownDto>(HttpStatusCode.OK, new ApiResponse<GamePlayerBreakdownDto>(_playerBreakdown)));
    }

    public Task<ApiResult<GameMapBreakdownDto>> GetMapBreakdown(GameType gameType, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<GameMapBreakdownDto>(HttpStatusCode.OK, new ApiResponse<GameMapBreakdownDto>(_mapBreakdown)));
    }
}
