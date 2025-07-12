
using Microsoft.Extensions.Logging;



using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class GameServersApi : BaseApi<RepositoryApiClientOptions>, IGameServersApi
    {
        public GameServersApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {

        }

        public async Task<ApiResult<GameServerDto>> GetGameServer(Guid gameServerId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/game-servers/{gameServerId}", Method.Get);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<GameServerDto>();
        }

        public async Task<ApiResult<CollectionModel<GameServerDto>>> GetGameServers(GameType[]? gameTypes, Guid[]? gameServerIds, GameServerFilter? filter, int skipEntries, int takeEntries, GameServerOrder? order, CancellationToken cancellationToken = default)
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

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<CollectionModel<GameServerDto>>();
        }

        public async Task<ApiResult> CreateGameServer(CreateGameServerDto createGameServerDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/game-servers", Method.Post);
            request.AddJsonBody(new List<CreateGameServerDto> { createGameServerDto });

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> CreateGameServers(List<CreateGameServerDto> createGameServerDtos, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/game-servers", Method.Post);
            request.AddJsonBody(createGameServerDtos);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpdateGameServer(EditGameServerDto editGameServerDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/game-servers/{editGameServerDto.GameServerId}", Method.Patch);
            request.AddJsonBody(editGameServerDto);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeleteGameServer(Guid gameServerId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/game-servers/{gameServerId}", Method.Delete);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }
    }
}


