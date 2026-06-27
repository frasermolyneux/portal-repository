using System.Net;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakePlayerAnalyticsV2Api : IPlayerAnalyticsV2Api
{
    private PlayersOverviewDto _overview = new();
    private PlayersTimeseriesDto _timeseries = new();
    private PlayersTopDto _top = new();
    private PlayersByGameDto _byGame = new();
    private PlayersByServerDto _byServer = new();
    private PlayerDetailDto _playerDetail = new();
    private PlayerTrendsDto _playerTimeseries = new();

    public FakePlayerAnalyticsV2Api SetOverview(PlayersOverviewDto overview) { _overview = overview; return this; }
    public FakePlayerAnalyticsV2Api SetTimeseries(PlayersTimeseriesDto timeseries) { _timeseries = timeseries; return this; }
    public FakePlayerAnalyticsV2Api SetTop(PlayersTopDto top) { _top = top; return this; }
    public FakePlayerAnalyticsV2Api SetByGame(PlayersByGameDto byGame) { _byGame = byGame; return this; }
    public FakePlayerAnalyticsV2Api SetByServer(PlayersByServerDto byServer) { _byServer = byServer; return this; }
    public FakePlayerAnalyticsV2Api SetPlayerDetail(PlayerDetailDto playerDetail) { _playerDetail = playerDetail; return this; }
    public FakePlayerAnalyticsV2Api SetPlayerTimeseries(PlayerTrendsDto playerTimeseries) { _playerTimeseries = playerTimeseries; return this; }

    public FakePlayerAnalyticsV2Api Reset()
    {
        _overview = new();
        _timeseries = new();
        _top = new();
        _byGame = new();
        _byServer = new();
        _playerDetail = new();
        _playerTimeseries = new();
        return this;
    }

    public Task<ApiResult<PlayersOverviewDto>> GetOverview(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<PlayersOverviewDto>(HttpStatusCode.OK, new ApiResponse<PlayersOverviewDto>(_overview)));
    }

    public Task<ApiResult<PlayersTimeseriesDto>> GetTimeseries(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<PlayersTimeseriesDto>(HttpStatusCode.OK, new ApiResponse<PlayersTimeseriesDto>(_timeseries)));
    }

    public Task<ApiResult<PlayersTimeseriesDto>> GetTimeseries(
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
        return Task.FromResult(new ApiResult<PlayersTimeseriesDto>(HttpStatusCode.OK, new ApiResponse<PlayersTimeseriesDto>(_timeseries)));
    }

    public Task<ApiResult<PlayersTopDto>> GetTop(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<PlayersTopDto>(HttpStatusCode.OK, new ApiResponse<PlayersTopDto>(_top)));
    }

    public Task<ApiResult<PlayersByGameDto>> GetByGame(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<PlayersByGameDto>(HttpStatusCode.OK, new ApiResponse<PlayersByGameDto>(_byGame)));
    }

    public Task<ApiResult<PlayersByServerDto>> GetByServer(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<PlayersByServerDto>(HttpStatusCode.OK, new ApiResponse<PlayersByServerDto>(_byServer)));
    }

    public Task<ApiResult<PlayerDetailDto>> GetPlayerDetail(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<PlayerDetailDto>(HttpStatusCode.OK, new ApiResponse<PlayerDetailDto>(_playerDetail)));
    }

    public Task<ApiResult<PlayerTrendsDto>> GetPlayerTimeseries(Guid playerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<PlayerTrendsDto>(HttpStatusCode.OK, new ApiResponse<PlayerTrendsDto>(_playerTimeseries)));
    }

    public Task<ApiResult<PlayerTrendsDto>> GetPlayerTimeseries(
        Guid playerId,
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
        return Task.FromResult(new ApiResult<PlayerTrendsDto>(HttpStatusCode.OK, new ApiResponse<PlayerTrendsDto>(_playerTimeseries)));
    }
}
