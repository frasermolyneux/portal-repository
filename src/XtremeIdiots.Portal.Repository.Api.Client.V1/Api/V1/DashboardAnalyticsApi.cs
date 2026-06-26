using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1;

public class DashboardAnalyticsApi : BaseApi<RepositoryApiClientOptions>, IDashboardAnalyticsApi
{
    public DashboardAnalyticsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options)
        : base(logger, apiTokenProvider, restClientService, options)
    {
    }

    public async Task<ApiResult<DashboardSummaryDto>> GetSummary(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/dashboard/summary", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<DashboardSummaryDto>();
    }

    public async Task<ApiResult<DashboardTrendsDto>> GetTrends(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/dashboard/trends", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("bucket", bucket.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<DashboardTrendsDto>();
    }

    public async Task<ApiResult<DashboardCompositionDto>> GetComposition(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/dashboard/composition", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<DashboardCompositionDto>();
    }
}