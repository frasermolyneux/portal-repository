using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Servers;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1;

public class ServerAnalyticsApi : BaseApi<RepositoryApiClientOptions>, IServerAnalyticsApi
{
    public ServerAnalyticsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options)
        : base(logger, apiTokenProvider, restClientService, options)
    {
    }

    public async Task<ApiResult<ServerOverviewDto>> GetOverview(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/servers/{gameServerId}/overview", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<ServerOverviewDto>();
    }

    public async Task<ApiResult<ServerTimeseriesDto>> GetTimeseries(Guid gameServerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return await GetTimeseries(
            gameServerId,
            fromUtc,
            toUtc,
            bucket,
            AnalyticsCompareMode.None,
            AnalyticsQueryDefaults.DefaultComparePeriods,
            AnalyticsAlignMode.None,
            "UTC",
            false,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApiResult<ServerTimeseriesDto>> GetTimeseries(
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
        var request = await CreateRequestAsync($"v1/analytics/servers/{gameServerId}/timeseries", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("bucket", bucket.ToString());
        request.AddQueryParameter("compareMode", ToCompareModeQueryValue(compareMode));
        request.AddQueryParameter("comparePeriods", comparePeriods.ToString());
        request.AddQueryParameter("alignMode", ToAlignModeQueryValue(alignMode));
        request.AddQueryParameter("timezone", timezone);
        request.AddQueryParameter("normalize", normalize ? "true" : "false");

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<ServerTimeseriesDto>();
    }

    public async Task<ApiResult<ServerPlayersCurrentDto>> GetPlayersCurrent(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/servers/{gameServerId}/players-current", Method.Get).ConfigureAwait(false);

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<ServerPlayersCurrentDto>();
    }

    public async Task<ApiResult<ServerEventsSummaryDto>> GetEventsSummary(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/servers/{gameServerId}/events-summary", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<ServerEventsSummaryDto>();
    }

    public async Task<ApiResult<ServerChatSummaryDto>> GetChatSummary(Guid gameServerId, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/servers/{gameServerId}/chat-summary", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<ServerChatSummaryDto>();
    }

    public async Task<ApiResult<ServerMapRotationPerformanceDto>> GetMapRotationPerformance(Guid gameServerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/servers/{gameServerId}/map-rotation-performance", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<ServerMapRotationPerformanceDto>();
    }

    private static string ToCompareModeQueryValue(AnalyticsCompareMode compareMode)
    {
        return compareMode switch
        {
            AnalyticsCompareMode.PreviousPeriod => "previous_period",
            AnalyticsCompareMode.RollingPeriods => "rolling_periods",
            _ => "none"
        };
    }

    private static string ToAlignModeQueryValue(AnalyticsAlignMode alignMode)
    {
        return alignMode switch
        {
            AnalyticsAlignMode.Week => "week",
            AnalyticsAlignMode.Month => "month",
            _ => "none"
        };
    }
}