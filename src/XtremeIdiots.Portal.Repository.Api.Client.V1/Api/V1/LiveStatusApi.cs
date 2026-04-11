using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.LiveStatus;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1;

public class LiveStatusApi : BaseApi<RepositoryApiClientOptions>, ILiveStatusApi
{
    public LiveStatusApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options)
        : base(logger, apiTokenProvider, restClientService, options)
    {
    }

    public async Task<ApiResult> SetGameServerLiveStatus(Guid gameServerId, SetGameServerLiveStatusDto dto, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/game-servers/{gameServerId}/live-status", Method.Put).ConfigureAwait(false);
        request.AddJsonBody(dto);

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult();
    }

    public async Task<ApiResult<GameServerLiveStatusDto>> GetGameServerLiveStatus(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/game-servers/{gameServerId}/live-status", Method.Get).ConfigureAwait(false);

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<GameServerLiveStatusDto>();
    }

    public async Task<ApiResult<CollectionModel<GameServerLiveStatusDto>>> GetAllGameServerLiveStatuses(CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync("v1/game-servers/live-status", Method.Get).ConfigureAwait(false);

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<CollectionModel<GameServerLiveStatusDto>>();
    }

    public async Task<ApiResult<CollectionModel<LivePlayerDto>>> GetGameServerLivePlayers(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync($"v1/game-servers/{gameServerId}/live-players", Method.Get).ConfigureAwait(false);

        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return response.ToApiResult<CollectionModel<LivePlayerDto>>();
    }
}
