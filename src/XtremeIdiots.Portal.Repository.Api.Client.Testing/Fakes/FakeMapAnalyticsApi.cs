using System.Net;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeMapAnalyticsApi : IMapAnalyticsApi
{
    private MapsOverviewDto _overview = new();
    private MapsHotspotsDto _hotspots = new();
    private MapsTopPlayedDto _topPlayed = new();
    private MapsTopVotedDto _topVoted = new();
    private MapsByGameDto _byGame = new();
    private MapsByServerDto _byServer = new();
    private MapDetailDto _mapDetail = new();

    public FakeMapAnalyticsApi SetOverview(MapsOverviewDto overview) { _overview = overview; return this; }
    public FakeMapAnalyticsApi SetHotspots(MapsHotspotsDto hotspots) { _hotspots = hotspots; return this; }
    public FakeMapAnalyticsApi SetTopPlayed(MapsTopPlayedDto topPlayed) { _topPlayed = topPlayed; return this; }
    public FakeMapAnalyticsApi SetTopVoted(MapsTopVotedDto topVoted) { _topVoted = topVoted; return this; }
    public FakeMapAnalyticsApi SetByGame(MapsByGameDto byGame) { _byGame = byGame; return this; }
    public FakeMapAnalyticsApi SetByServer(MapsByServerDto byServer) { _byServer = byServer; return this; }
    public FakeMapAnalyticsApi SetMapDetail(MapDetailDto mapDetail) { _mapDetail = mapDetail; return this; }

    public FakeMapAnalyticsApi Reset()
    {
        _overview = new();
        _hotspots = new();
        _topPlayed = new();
        _topVoted = new();
        _byGame = new();
        _byServer = new();
        _mapDetail = new();
        return this;
    }

    public Task<ApiResult<MapsOverviewDto>> GetOverview(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<MapsOverviewDto>(HttpStatusCode.OK, new ApiResponse<MapsOverviewDto>(_overview)));
    }

    public Task<ApiResult<MapsHotspotsDto>> GetHotspots(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<MapsHotspotsDto>(HttpStatusCode.OK, new ApiResponse<MapsHotspotsDto>(_hotspots)));
    }

    public Task<ApiResult<MapsTopPlayedDto>> GetTopPlayed(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<MapsTopPlayedDto>(HttpStatusCode.OK, new ApiResponse<MapsTopPlayedDto>(_topPlayed)));
    }

    public Task<ApiResult<MapsTopVotedDto>> GetTopVoted(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<MapsTopVotedDto>(HttpStatusCode.OK, new ApiResponse<MapsTopVotedDto>(_topVoted)));
    }

    public Task<ApiResult<MapsByGameDto>> GetByGame(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<MapsByGameDto>(HttpStatusCode.OK, new ApiResponse<MapsByGameDto>(_byGame)));
    }

    public Task<ApiResult<MapsByServerDto>> GetByServer(Guid gameServerId, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<MapsByServerDto>(HttpStatusCode.OK, new ApiResponse<MapsByServerDto>(_byServer)));
    }

    public Task<ApiResult<MapDetailDto>> GetMapDetail(Guid mapId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<MapDetailDto>(HttpStatusCode.OK, new ApiResponse<MapDetailDto>(_mapDetail)));
    }
}
