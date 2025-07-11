using Microsoft.Extensions.Logging;



using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class GameServersEventsApi : BaseApi<RepositoryApiClientOptions>, IGameServersEventsApi
    {
        public GameServersEventsApi(ILogger<GameServersEventsApi> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult> CreateGameServerEvent(CreateGameServerEventDto createGameServerEventDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/game-server-events", Method.Post);
            request.AddJsonBody(new List<CreateGameServerEventDto> { createGameServerEventDto });

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> CreateGameServerEvents(List<CreateGameServerEventDto> createGameServerEventDtos, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/game-server-events", Method.Post);
            request.AddJsonBody(createGameServerEventDtos);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }
    }
}


