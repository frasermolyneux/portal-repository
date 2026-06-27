using System.Net;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeDashboardAnalyticsApi : IDashboardAnalyticsApi
{
    private DashboardHomeDto _home = new();
    private DashboardServerDto _server = new();

    public FakeDashboardAnalyticsApi SetHome(DashboardHomeDto home) { _home = home; return this; }
    public FakeDashboardAnalyticsApi SetServer(DashboardServerDto server) { _server = server; return this; }

    public FakeDashboardAnalyticsApi Reset()
    {
        _home = new();
        _server = new();
        return this;
    }

    public Task<ApiResult<DashboardHomeDto>> GetHome(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<DashboardHomeDto>(HttpStatusCode.OK, new ApiResponse<DashboardHomeDto>(_home)));
    }

    public Task<ApiResult<DashboardServerDto>> GetServer(Guid gameServerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<DashboardServerDto>(HttpStatusCode.OK, new ApiResponse<DashboardServerDto>(_server)));
    }
}
