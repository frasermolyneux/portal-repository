using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Games;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1;

public class GameAnalyticsApi : BaseApi<RepositoryApiClientOptions>, IGameAnalyticsApi
{
    public GameAnalyticsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options)
        : base(logger, apiTokenProvider, restClientService, options)
    {
    }

    public async Task<ApiResult<GameOverviewDto>> GetOverview(GameType gameType, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/games/overview", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("gameType", gameType.ToString());
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GameOverviewDto>();
    }

    public async Task<ApiResult<GameTimeseriesDto>> GetTimeseries(GameType gameType, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return await GetTimeseries(
            gameType,
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

    public async Task<ApiResult<GameTimeseriesDto>> GetTimeseries(
        GameType gameType,
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
        var request = await CreateRequestAsync("v1/analytics/games/timeseries", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("gameType", gameType.ToString());
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("bucket", bucket.ToString());
        request.AddQueryParameter("compareMode", ToCompareModeQueryValue(compareMode));
        request.AddQueryParameter("comparePeriods", comparePeriods.ToString());
        request.AddQueryParameter("alignMode", ToAlignModeQueryValue(alignMode));
        request.AddQueryParameter("timezone", timezone);
        request.AddQueryParameter("normalize", normalize ? "true" : "false");

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GameTimeseriesDto>();
    }

    public async Task<ApiResult<GameServerBreakdownDto>> GetServerBreakdown(GameType gameType, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/games/servers", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("gameType", gameType.ToString());
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GameServerBreakdownDto>();
    }

    public async Task<ApiResult<GamePlayerBreakdownDto>> GetPlayerBreakdown(GameType gameType, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/games/players", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("gameType", gameType.ToString());
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GamePlayerBreakdownDto>();
    }

    public async Task<ApiResult<GameMapBreakdownDto>> GetMapBreakdown(GameType gameType, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/analytics/games/maps", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("gameType", gameType.ToString());
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GameMapBreakdownDto>();
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