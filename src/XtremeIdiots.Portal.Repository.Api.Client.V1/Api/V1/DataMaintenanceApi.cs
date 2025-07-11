using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class DataMaintenanceApi : BaseApi, IDataMaintenanceApi
    {
        public DataMaintenanceApi(ILogger<DataMaintenanceApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {

        }

        public async Task<ApiResponseDto> PruneChatMessages()
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/prune-chat-messages", Method.Delete));

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> PruneGameServerEvents()
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/prune-game-server-events", Method.Delete));

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> PruneGameServerStats()
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/prune-game-server-stats", Method.Delete));

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> PruneRecentPlayers()
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/prune-recent-players", Method.Delete));

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> ResetSystemAssignedPlayerTags()
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/reset-system-assigned-player-tags", Method.Delete));

            return response.ToApiResponse();
        }
    }
}
