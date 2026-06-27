using System.Net;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Servers;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeServerAnalyticsApi : IServerAnalyticsApi
{
    private ServerOverviewDto _overview = new();
    private ServerTimeseriesDto _timeseries = new();
    private ServerPlayersCurrentDto _playersCurrent = new();
    private ServerEventsSummaryDto _eventsSummary = new();
    private ServerChatSummaryDto _chatSummary = new();
    private ServerChatCommandsSummaryDto _chatCommandsSummary = new();
    private ServerMapRotationPerformanceDto _mapRotationPerformance = new();

    public FakeServerAnalyticsApi SetOverview(ServerOverviewDto overview) { _overview = overview; return this; }
    public FakeServerAnalyticsApi SetTimeseries(ServerTimeseriesDto timeseries) { _timeseries = timeseries; return this; }
    public FakeServerAnalyticsApi SetPlayersCurrent(ServerPlayersCurrentDto playersCurrent) { _playersCurrent = playersCurrent; return this; }
    public FakeServerAnalyticsApi SetEventsSummary(ServerEventsSummaryDto eventsSummary) { _eventsSummary = eventsSummary; return this; }
    public FakeServerAnalyticsApi SetChatSummary(ServerChatSummaryDto chatSummary) { _chatSummary = chatSummary; return this; }
    public FakeServerAnalyticsApi SetChatCommandsSummary(ServerChatCommandsSummaryDto chatCommandsSummary) { _chatCommandsSummary = chatCommandsSummary; return this; }
    public FakeServerAnalyticsApi SetMapRotationPerformance(ServerMapRotationPerformanceDto mapRotationPerformance) { _mapRotationPerformance = mapRotationPerformance; return this; }

    public FakeServerAnalyticsApi Reset()
    {
        _overview = new();
        _timeseries = new();
        _playersCurrent = new();
        _eventsSummary = new();
        _chatSummary = new();
        _chatCommandsSummary = new();
        _mapRotationPerformance = new();
        return this;
    }

    public Task<ApiResult<ServerOverviewDto>> GetOverview(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<ServerOverviewDto>(HttpStatusCode.OK, new ApiResponse<ServerOverviewDto>(_overview)));
    }

    public Task<ApiResult<ServerTimeseriesDto>> GetTimeseries(Guid gameServerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<ServerTimeseriesDto>(HttpStatusCode.OK, new ApiResponse<ServerTimeseriesDto>(_timeseries)));
    }

    public Task<ApiResult<ServerTimeseriesDto>> GetTimeseries(
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
        return Task.FromResult(new ApiResult<ServerTimeseriesDto>(HttpStatusCode.OK, new ApiResponse<ServerTimeseriesDto>(_timeseries)));
    }

    public Task<ApiResult<ServerPlayersCurrentDto>> GetPlayersCurrent(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<ServerPlayersCurrentDto>(HttpStatusCode.OK, new ApiResponse<ServerPlayersCurrentDto>(_playersCurrent)));
    }

    public Task<ApiResult<ServerEventsSummaryDto>> GetEventsSummary(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<ServerEventsSummaryDto>(HttpStatusCode.OK, new ApiResponse<ServerEventsSummaryDto>(_eventsSummary)));
    }

    public Task<ApiResult<ServerChatSummaryDto>> GetChatSummary(Guid gameServerId, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<ServerChatSummaryDto>(HttpStatusCode.OK, new ApiResponse<ServerChatSummaryDto>(_chatSummary)));
    }

    public Task<ApiResult<ServerChatCommandsSummaryDto>> GetChatCommandsSummary(Guid gameServerId, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<ServerChatCommandsSummaryDto>(HttpStatusCode.OK, new ApiResponse<ServerChatCommandsSummaryDto>(_chatCommandsSummary)));
    }

    public Task<ApiResult<ServerMapRotationPerformanceDto>> GetMapRotationPerformance(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<ServerMapRotationPerformanceDto>(HttpStatusCode.OK, new ApiResponse<ServerMapRotationPerformanceDto>(_mapRotationPerformance)));
    }
}
