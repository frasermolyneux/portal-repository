using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class LivePlayersApi : BaseApi, ILivePlayersApi
    {
        public LivePlayersApi(ILogger<LivePlayersApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<LivePlayersCollectionDto>> GetLivePlayers(GameType? gameType, Guid? gameServerId, LivePlayerFilter? filter, int skipEntries, int takeEntries, LivePlayersOrder? order)
        {
            var request = await CreateRequestAsync($"v1/live-players", Method.Get);

            if (gameType.HasValue)
                request.AddQueryParameter("gameType", gameType.ToString());

            if (gameServerId.HasValue)
                request.AddQueryParameter("gameServerId", gameServerId.ToString());

            if (filter.HasValue)
                request.AddQueryParameter("filter", filter.ToString());

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<LivePlayersCollectionDto>();
        }

        public async Task<ApiResponseDto> SetLivePlayersForGameServer(Guid gameServerId, List<CreateLivePlayerDto> createLivePlayerDtos)
        {
            var request = await CreateRequestAsync($"v1/live-players/{gameServerId}", Method.Post);
            request.AddJsonBody(createLivePlayerDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
