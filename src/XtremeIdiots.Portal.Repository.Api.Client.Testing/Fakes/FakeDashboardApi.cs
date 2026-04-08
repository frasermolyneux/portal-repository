using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeDashboardApi : IDashboardApi
{
    private DashboardSummaryDto _summary = new();
    private List<AdminLeaderboardEntryDto> _leaderboard = [];
    private List<ModerationTrendDataPointDto> _trend = [];
    private ServerUtilizationCollectionDto _utilization = new();

    public FakeDashboardApi SetSummary(DashboardSummaryDto summary) { _summary = summary; return this; }
    public FakeDashboardApi SetLeaderboard(List<AdminLeaderboardEntryDto> entries) { _leaderboard = entries; return this; }
    public FakeDashboardApi SetTrend(List<ModerationTrendDataPointDto> entries) { _trend = entries; return this; }
    public FakeDashboardApi SetUtilization(ServerUtilizationCollectionDto utilization) { _utilization = utilization; return this; }
    public FakeDashboardApi Reset() { _summary = new(); _leaderboard = []; _trend = []; _utilization = new(); return this; }

    public Task<ApiResult<DashboardSummaryDto>> GetDashboardSummary(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<DashboardSummaryDto>(HttpStatusCode.OK, new ApiResponse<DashboardSummaryDto>(_summary)));
    }

    public Task<ApiResult<CollectionModel<AdminLeaderboardEntryDto>>> GetAdminLeaderboard(int days, CancellationToken cancellationToken = default)
    {
        var collection = new CollectionModel<AdminLeaderboardEntryDto> { Items = _leaderboard };
        return Task.FromResult(new ApiResult<CollectionModel<AdminLeaderboardEntryDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<AdminLeaderboardEntryDto>>(collection)));
    }

    public Task<ApiResult<CollectionModel<ModerationTrendDataPointDto>>> GetModerationTrend(int days, CancellationToken cancellationToken = default)
    {
        var collection = new CollectionModel<ModerationTrendDataPointDto> { Items = _trend };
        return Task.FromResult(new ApiResult<CollectionModel<ModerationTrendDataPointDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<ModerationTrendDataPointDto>>(collection)));
    }

    public Task<ApiResult<ServerUtilizationCollectionDto>> GetServerUtilization(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<ServerUtilizationCollectionDto>(HttpStatusCode.OK, new ApiResponse<ServerUtilizationCollectionDto>(_utilization)));
    }
}
