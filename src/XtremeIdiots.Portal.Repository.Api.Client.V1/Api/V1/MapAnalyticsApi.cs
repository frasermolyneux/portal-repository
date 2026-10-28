using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

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

    public async Task<ApiResult<MapsOverviewDto>> GetOverview(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/maps/overview", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<MapsOverviewDto>();
    }

    public async Task<ApiResult<MapInsightsDto>> GetInsights(DateTime fromUtc, DateTime toUtc, GameType? gameType = null, Guid? gameServerId = null, CancellationToken cancellationToken = default)
        => await ReadInsights("v1/analytics/maps/insights", fromUtc, toUtc, gameType, gameServerId, cancellationToken).ConfigureAwait(false);

    public Task<ApiResult<MapInsightsDto>> GetVoteInsights(DateTime fromUtc, DateTime toUtc, GameType? gameType = null, Guid? gameServerId = null, CancellationToken cancellationToken = default)
        => ReadInsights("v1/analytics/maps/insights/votes", fromUtc, toUtc, gameType, gameServerId, cancellationToken);

    private async Task<ApiResult<MapInsightsDto>> ReadInsights(string route, DateTime fromUtc, DateTime toUtc, GameType? gameType, Guid? gameServerId, CancellationToken cancellationToken)
    {
        var request = await CreateRequestAsync(route, Method.Get, cancellationToken).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        if (gameType.HasValue)
        {
            request.AddQueryParameter("gameType", gameType.Value.ToString());
        }

        if (gameServerId.HasValue)
        {
            request.AddQueryParameter("gameServerId", gameServerId.Value.ToString());
        }

        return (await ExecuteAsync(request, cancellationToken).ConfigureAwait(false)).ToApiResult<MapInsightsDto>();
    }

    public async Task<ApiResult<MapsHotspotsDto>> GetHotspots(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/maps/hotspots", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<MapsHotspotsDto>();
    }

    public async Task<ApiResult<MapsTopPlayedDto>> GetTopPlayed(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/maps/top-played", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<MapsTopPlayedDto>();
    }

    public async Task<ApiResult<MapsTopVotedDto>> GetTopVoted(DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/maps/top-voted", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<MapsTopVotedDto>();
    }

    public async Task<ApiResult<MapsByGameDto>> GetByGame(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/maps/by-game", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<MapsByGameDto>();
    }

    public async Task<ApiResult<MapsByServerDto>> GetByServer(Guid gameServerId, DateTime fromUtc, DateTime toUtc, int top = AnalyticsQueryDefaults.DefaultTop, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/maps/by-server/{gameServerId}", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));
        request.AddQueryParameter("top", top.ToString());

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<MapsByServerDto>();
    }

    public async Task<ApiResult<MapDetailDto>> GetMapDetail(Guid mapId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/analytics/maps/{mapId}", Method.Get).ConfigureAwait(false);
        request.AddQueryParameter("fromUtc", fromUtc.ToString("O"));
        request.AddQueryParameter("toUtc", toUtc.ToString("O"));

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<MapDetailDto>();
    }
}
