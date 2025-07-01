using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class GameServersEventsApi : BaseApi, IGameServersEventsApi
    {
        public GameServersEventsApi(ILogger<GameServersEventsApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto> CreateGameServerEvent(CreateGameServerEventDto createGameServerEventDto)
        {
            var request = await CreateRequestAsync($"v1/game-server-events", Method.Post);
            request.AddJsonBody(new List<CreateGameServerEventDto> { createGameServerEventDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> CreateGameServerEvents(List<CreateGameServerEventDto> createGameServerEventDtos)
        {
            var request = await CreateRequestAsync($"v1/game-server-events", Method.Post);
            request.AddJsonBody(createGameServerEventDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
