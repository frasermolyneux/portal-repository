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
    public class GameServersStatsApi : BaseApi, IGameServersStatsApi
    {
        public GameServersStatsApi(ILogger<GameServersStatsApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto> CreateGameServerStats(List<CreateGameServerStatDto> createGameServerStatDtos)
        {
            var request = await CreateRequestAsync($"v1/game-servers-stats", Method.Post);
            request.AddJsonBody(createGameServerStatDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto<GameServerStatCollectionDto>> GetGameServerStatusStats(Guid gameServerId, DateTime cutoff)
        {
            var request = await CreateRequestAsync($"v1/game-servers-stats/{gameServerId}", Method.Get);
            request.AddQueryParameter("cutoff", cutoff.ToString("MM/dd/yyyy HH:mm:ss"));

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<GameServerStatCollectionDto>();
        }
    }
}
