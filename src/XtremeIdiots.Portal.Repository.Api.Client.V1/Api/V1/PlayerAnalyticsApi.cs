using Microsoft.Extensions.Logging;



using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class PlayerAnalyticsApi : BaseApi<RepositoryApiClientOptions>, IPlayerAnalyticsApi
    {
        public PlayerAnalyticsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<CollectionModel<PlayerAnalyticEntryDto>>> GetCumulativeDailyPlayers(DateTime cutoff, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/player-analytics/cumulative-daily-players", Method.Get);
            request.AddQueryParameter("cutoff", cutoff.ToString("MM/dd/yyyy HH:mm:ss"));

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<CollectionModel<PlayerAnalyticEntryDto>>();
        }

        public async Task<ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>> GetNewDailyPlayersPerGame(DateTime cutoff, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/player-analytics/new-daily-players-per-game", Method.Get);
            request.AddQueryParameter("cutoff", cutoff.ToString("MM/dd/yyyy HH:mm:ss"));

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>();
        }

        public async Task<ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>> GetPlayersDropOffPerGameJson(DateTime cutoff, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/player-analytics/players-drop-off-per-game", Method.Get);
            request.AddQueryParameter("cutoff", cutoff.ToString("MM/dd/yyyy HH:mm:ss"));

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>();
        }
    }
}


