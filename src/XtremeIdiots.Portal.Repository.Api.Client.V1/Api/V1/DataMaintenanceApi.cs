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
        public DataMaintenanceApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {

        }

        public async Task<ApiResult> PruneChatMessages(CancellationToken cancellationToken = default)
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/prune-chat-messages", Method.Delete), cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> PruneGameServerEvents(CancellationToken cancellationToken = default)
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/prune-game-server-events", Method.Delete), cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> PruneGameServerStats(CancellationToken cancellationToken = default)
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/prune-game-server-stats", Method.Delete), cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> PruneRecentPlayers(CancellationToken cancellationToken = default)
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/prune-recent-players", Method.Delete), cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> ResetSystemAssignedPlayerTags(CancellationToken cancellationToken = default)
        {
            var response = await ExecuteAsync(await CreateRequestAsync("v1/data-maintenance/reset-system-assigned-player-tags", Method.Put), cancellationToken);

            return response.ToApiResult();
        }
    }
}


