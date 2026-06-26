using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1;

public class MapAnalyticsApi : BaseApi<RepositoryApiClientOptions>, IMapAnalyticsApi
{
    public MapAnalyticsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options)
        : base(logger, apiTokenProvider, restClientService, options)
    {
    }

    public async Task<ApiResult<MapOverviewDto>> GetOverview(Guid mapId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/maps/{mapId}/overview", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<MapOverviewDto>();
    }

    public async Task<ApiResult<MapTrendsDto>> GetTrends(Guid mapId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/maps/{mapId}/trends", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("bucket", bucket.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<MapTrendsDto>();
    }

    public async Task<ApiResult<MapRankingsDto>> GetRankings(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/maps/rankings", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<MapRankingsDto>();
    }
}