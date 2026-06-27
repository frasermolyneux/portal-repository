using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1;

public class PlayerAnalyticsV2Api : BaseApi<RepositoryApiClientOptions>, IPlayerAnalyticsV2Api
{
    public PlayerAnalyticsV2Api(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options)
        : base(logger, apiTokenProvider, restClientService, options)
    {
    }

    public async Task<ApiResult<PlayersOverviewDto>> GetOverview(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/players/overview", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<PlayersOverviewDto>();
    }

    public async Task<ApiResult<PlayersTimeseriesDto>> GetTimeseries(DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return await GetTimeseries(
            fromUtc, toUtc, bucket, AnalyticsCompareMode.None, AnalyticsQueryDefaults.DefaultComparePeriods, AnalyticsAlignMode.None, "UTC", false, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApiResult<PlayersTimeseriesDto>> GetTimeseries(
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
        var request = await CreateRequestAsync($"v1/analytics/players/timeseries", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("bucket", bucket.ToString());
        request.AddQueryParameter("compareMode", ToCompareModeQueryValue(compareMode));
        request.AddQueryParameter("comparePeriods", comparePeriods.ToString());
        request.AddQueryParameter("alignMode", ToAlignModeQueryValue(alignMode));
        request.AddQueryParameter("timezone", timezone);
        request.AddQueryParameter("normalize", normalize ? "true" : "false");

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<PlayersTimeseriesDto>();
    }

    public async Task<ApiResult<PlayersTopDto>> GetTop(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/players/top", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<PlayersTopDto>();
    }

    public async Task<ApiResult<PlayersByGameDto>> GetByGame(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/players/by-game", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<PlayersByGameDto>();
    }

    public async Task<ApiResult<PlayersByServerDto>> GetByServer(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/players/by-server", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<PlayersByServerDto>();
    }

    public async Task<ApiResult<PlayerDetailDto>> GetPlayerDetail(Guid playerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/players/{playerId}", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<PlayerDetailDto>();
    }

    public async Task<ApiResult<PlayerTrendsDto>> GetPlayerTimeseries(Guid playerId, DateTime fromUtc, DateTime toUtc, AnalyticsBucket bucket, CancellationToken cancellationToken = default)
    {
        return await GetPlayerTimeseries(
            playerId, fromUtc, toUtc, bucket, AnalyticsCompareMode.None, AnalyticsQueryDefaults.DefaultComparePeriods, AnalyticsAlignMode.None, "UTC", false, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApiResult<PlayerTrendsDto>> GetPlayerTimeseries(
        Guid playerId,
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
        var request = await CreateRequestAsync($"v1/analytics/players/{playerId}/timeseries", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("bucket", bucket.ToString());
        request.AddQueryParameter("compareMode", ToCompareModeQueryValue(compareMode));
        request.AddQueryParameter("comparePeriods", comparePeriods.ToString());
        request.AddQueryParameter("alignMode", ToAlignModeQueryValue(alignMode));
        request.AddQueryParameter("timezone", timezone);
        request.AddQueryParameter("normalize", normalize ? "true" : "false");

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<PlayerTrendsDto>();
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