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
    public class GameServersStatsApi : BaseApi<RepositoryApiClientOptions>, IGameServersStatsApi
    {
        public GameServersStatsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult> CreateGameServerStats(List<CreateGameServerStatDto> createGameServerStatDtos, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/game-servers-stats", Method.Post);
            request.AddJsonBody(createGameServerStatDtos);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult<CollectionModel<GameServerStatDto>>> GetGameServerStatusStats(Guid gameServerId, DateTime cutoff, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/game-servers-stats/{gameServerId}", Method.Get);
            request.AddQueryParameter("cutoff", cutoff.ToString("MM/dd/yyyy HH:mm:ss"));

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<CollectionModel<GameServerStatDto>>();
        }
    }
}


