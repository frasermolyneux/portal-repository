using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameServers;

namespace XtremeIdiots.Portal.RepositoryApiClient.Api
{
    public class GameServersStatsApi : BaseApi, IGameServersStatsApi
    {
        public GameServersStatsApi(ILogger<GameServersStatsApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options) : base(logger, apiTokenProvider, options)
        {
        }

        public async Task<ApiResponseDto> CreateGameServerStats(List<CreateGameServerStatDto> createGameServerStatDtos)
        {
            var request = await CreateRequest($"game-servers-stats", Method.Post);
            request.AddJsonBody(createGameServerStatDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto<GameServerStatCollectionDto>> GetGameServerStatusStats(Guid gameServerId, DateTime cutoff)
        {
            var request = await CreateRequest($"game-servers-stats/{gameServerId}", Method.Get);
            request.AddQueryParameter("cutoff", cutoff.ToString("MM/dd/yyyy HH:mm:ss"));

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<GameServerStatCollectionDto>();
        }
    }
}
