using System.Net;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeDashboardAnalyticsApi : IDashboardAnalyticsApi
{
    private DashboardSummaryDto _summary = new();
    private DashboardTrendsDto _trends = new();
    private DashboardCompositionDto _composition = new();

    public FakeDashboardAnalyticsApi SetSummary(DashboardSummaryDto summary) { _summary = summary; return this; }
    public FakeDashboardAnalyticsApi SetTrends(DashboardTrendsDto trends) { _trends = trends; return this; }
    public FakeDashboardAnalyticsApi SetComposition(DashboardCompositionDto composition) { _composition = composition; return this; }

    public FakeDashboardAnalyticsApi Reset()
    {
        _summary = new();
        _trends = new();
        _composition = new();
        return this;
    }

    public Task<ApiResult<DashboardSummaryDto>> GetSummary(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<DashboardSummaryDto>(HttpStatusCode.OK, new ApiResponse<DashboardSummaryDto>(_summary)));
    }

    public Task<ApiResult<DashboardTrendsDto>> GetTrends(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<DashboardTrendsDto>(HttpStatusCode.OK, new ApiResponse<DashboardTrendsDto>(_trends)));
    }

    public Task<ApiResult<DashboardCompositionDto>> GetComposition(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult<DashboardCompositionDto>(HttpStatusCode.OK, new ApiResponse<DashboardCompositionDto>(_composition)));
    }
}
