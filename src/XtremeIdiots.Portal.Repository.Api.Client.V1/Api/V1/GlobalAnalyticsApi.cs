using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Global;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1;

public class GlobalAnalyticsApi : BaseApi<RepositoryApiClientOptions>, IGlobalAnalyticsApi
{
    public GlobalAnalyticsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options)
        : base(logger, apiTokenProvider, restClientService, options)
    {
    }

    public async Task<ApiResult<GlobalOverviewDto>> GetOverview(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/global/overview", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GlobalOverviewDto>();
    }

    public async Task<ApiResult<GlobalTimeseriesDto>> GetTimeseries(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return await GetTimeseries(
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

    public async Task<ApiResult<GlobalTimeseriesDto>> GetTimeseries(
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
        var request = await CreateRequestAsync("v1/analytics/global/timeseries", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("bucket", bucket.ToString());
        request.AddQueryParameter("compareMode", ToCompareModeQueryValue(compareMode));
        request.AddQueryParameter("comparePeriods", comparePeriods.ToString());
        request.AddQueryParameter("alignMode", ToAlignModeQueryValue(alignMode));
        request.AddQueryParameter("timezone", timezone);
        request.AddQueryParameter("normalize", normalize ? "true" : "false");

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GlobalTimeseriesDto>();
    }

    public async Task<ApiResult<GlobalGameBreakdownDto>> GetGameBreakdown(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/global/games", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GlobalGameBreakdownDto>();
    }

    public async Task<ApiResult<GlobalServerBreakdownDto>> GetServerBreakdown(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/global/servers", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GlobalServerBreakdownDto>();
    }

    public async Task<ApiResult<GlobalPlayerActivityDto>> GetPlayerActivity(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/global/players", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GlobalPlayerActivityDto>();
    }

    public async Task<ApiResult<GlobalGeoDistributionDto>> GetGeoDistribution(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/global/geo", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GlobalGeoDistributionDto>();
    }

    public async Task<ApiResult<GlobalModerationDto>> GetModeration(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/global/moderation", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GlobalModerationDto>();
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