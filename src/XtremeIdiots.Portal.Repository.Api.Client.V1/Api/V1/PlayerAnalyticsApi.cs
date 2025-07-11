using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class PlayerAnalyticsApi : BaseApi, IPlayerAnalyticsApi
    {
        public PlayerAnalyticsApi(ILogger<PlayerAnalyticsApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<PlayerAnalyticEntryCollectionDto>> GetCumulativeDailyPlayers(DateTime cutoff)
        {
            var request = await CreateRequestAsync($"v1/player-analytics/cumulative-daily-players", Method.Get);
            request.AddQueryParameter("cutoff", cutoff.ToString("MM/dd/yyyy HH:mm:ss"));

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<PlayerAnalyticEntryCollectionDto>();
        }

        public async Task<ApiResponseDto<PlayerAnalyticPerGameEntryCollectionDto>> GetNewDailyPlayersPerGame(DateTime cutoff)
        {
            var request = await CreateRequestAsync($"v1/player-analytics/new-daily-players-per-game", Method.Get);
            request.AddQueryParameter("cutoff", cutoff.ToString("MM/dd/yyyy HH:mm:ss"));

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<PlayerAnalyticPerGameEntryCollectionDto>();
        }

        public async Task<ApiResponseDto<PlayerAnalyticPerGameEntryCollectionDto>> GetPlayersDropOffPerGameJson(DateTime cutoff)
        {
            var request = await CreateRequestAsync($"v1/player-analytics/players-drop-off-per-game", Method.Get);
            request.AddQueryParameter("cutoff", cutoff.ToString("MM/dd/yyyy HH:mm:ss"));

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<PlayerAnalyticPerGameEntryCollectionDto>();
        }
    }
}
