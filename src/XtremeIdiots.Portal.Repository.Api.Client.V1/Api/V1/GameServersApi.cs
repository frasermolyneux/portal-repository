using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class GameServersApi : BaseApi, IGameServersApi
    {
        public GameServersApi(ILogger<GameServersApi> logger, IApiTokenProvider apiTokenProvider, IMemoryCache memoryCache, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {

        }

        public async Task<ApiResponseDto<GameServerDto>> GetGameServer(Guid gameServerId)
        {
            var request = await CreateRequestAsync($"v1/game-servers/{gameServerId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<GameServerDto>();
        }

        public async Task<ApiResponseDto<GameServersCollectionDto>> GetGameServers(GameType[]? gameTypes, Guid[]? gameServerIds, GameServerFilter? filter, int skipEntries, int takeEntries, GameServerOrder? order)
        {
            var request = await CreateRequestAsync("v1/game-servers", Method.Get);

            if (gameTypes != null)
                request.AddQueryParameter("gameTypes", string.Join(",", gameTypes));

            if (gameServerIds != null)
                request.AddQueryParameter("gameServerIds", string.Join(",", gameServerIds));

            if (filter.HasValue)
                request.AddQueryParameter("filter", filter.ToString());

            request.AddQueryParameter("takeEntries", takeEntries.ToString());
            request.AddQueryParameter("skipEntries", skipEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<GameServersCollectionDto>();
        }

        public async Task<ApiResponseDto> CreateGameServer(CreateGameServerDto createGameServerDto)
        {
            var request = await CreateRequestAsync("v1/game-servers", Method.Post);
            request.AddJsonBody(new List<CreateGameServerDto> { createGameServerDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> CreateGameServers(List<CreateGameServerDto> createGameServerDtos)
        {
            var request = await CreateRequestAsync("v1/game-servers", Method.Post);
            request.AddJsonBody(createGameServerDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdateGameServer(EditGameServerDto editGameServerDto)
        {
            var request = await CreateRequestAsync($"v1/game-servers/{editGameServerDto.GameServerId}", Method.Patch);
            request.AddJsonBody(editGameServerDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> DeleteGameServer(Guid gameServerId)
        {
            var request = await CreateRequestAsync($"v1/game-servers/{gameServerId}", Method.Delete);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
