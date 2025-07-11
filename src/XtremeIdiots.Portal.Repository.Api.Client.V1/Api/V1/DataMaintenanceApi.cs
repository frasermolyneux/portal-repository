using Microsoft.Extensions.Logging;



using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class DataMaintenanceApi : BaseApi<RepositoryApiClientOptions>, IDataMaintenanceApi
    {
        public DataMaintenanceApi(ILogger<DataMaintenanceApi> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {

        }

        public async Task<ApiResult> PruneChatMessages()
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/prune-chat-messages", Method.Delete));

            return response.ToApiResult();
        }

        public async Task<ApiResult> PruneGameServerEvents()
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/prune-game-server-events", Method.Delete));

            return response.ToApiResult();
        }

        public async Task<ApiResult> PruneGameServerStats()
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/prune-game-server-stats", Method.Delete));

            return response.ToApiResult();
        }

        public async Task<ApiResult> PruneRecentPlayers()
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/prune-recent-players", Method.Delete));

            return response.ToApiResult();
        }

        public async Task<ApiResult> ResetSystemAssignedPlayerTags()
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/reset-system-assigned-player-tags", Method.Delete));

            return response.ToApiResult();
        }
    }
}


