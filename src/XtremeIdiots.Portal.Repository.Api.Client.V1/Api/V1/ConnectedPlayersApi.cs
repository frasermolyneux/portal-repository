using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class ConnectedPlayersApi : BaseApi<RepositoryApiClientOptions>, IConnectedPlayersApi
    {
        public ConnectedPlayersApi(
            ILogger<BaseApi<RepositoryApiClientOptions>> logger,
            IApiTokenProvider apiTokenProvider,
            IRestClientService restClientService,
            RepositoryApiClientOptions options)
            : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult> CreateConnectedPlayerLink(CreateConnectedPlayerLinkDto dto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/connected-players/link", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(dto);

            var response = await ExecuteAsync(request).ConfigureAwait(false);
            return response.ToApiResult();
        }

        public async Task<ApiResult<ConnectedPlayerActivationCodeDto>> ActivateConnectedPlayerActivationCode(
            ActivateConnectedPlayerActivationCodeDto dto,
            CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/connected-players/activation-code", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(dto);

            var response = await ExecuteAsync(request).ConfigureAwait(false);
            return response.ToApiResult<ConnectedPlayerActivationCodeDto>();
        }

        public async Task<ApiResult<ConnectedPlayerActivationCodeDto>> GetActiveConnectedPlayerActivationCode(
            Guid userProfileId,
            CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/user-profiles/{userProfileId}/connected-player-activation-code", Method.Get).ConfigureAwait(false);

            var response = await ExecuteAsync(request).ConfigureAwait(false);
            return response.ToApiResult<ConnectedPlayerActivationCodeDto>();
        }

        public async Task<ApiResult<ConnectedPlayerDto>> ConsumeConnectedPlayerActivationCode(
            ConsumeConnectedPlayerActivationCodeDto dto,
            CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/connected-players/activation-code/consume", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(dto);

            var response = await ExecuteAsync(request).ConfigureAwait(false);
            return response.ToApiResult<ConnectedPlayerDto>();
        }

        public async Task<ApiResult<CollectionModel<ConnectedPlayerDto>>> GetConnectedPlayersByUserProfile(
            Guid userProfileId,
            int skipEntries,
            int takeEntries,
            CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/user-profiles/{userProfileId}/connected-players", Method.Get).ConfigureAwait(false);
            request.AddQueryParameter(nameof(skipEntries), skipEntries);
            request.AddQueryParameter(nameof(takeEntries), takeEntries);

            var response = await ExecuteAsync(request).ConfigureAwait(false);
            return response.ToApiResult<CollectionModel<ConnectedPlayerDto>>();
        }

        public async Task<ApiResult<CollectionModel<ConnectedPlayerDto>>> GetConnectedPlayers(
            Guid? playerId,
            Guid? userProfileId,
            GameType? gameType,
            bool? isActive,
            int skipEntries,
            int takeEntries,
            CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/connected-players", Method.Get).ConfigureAwait(false);

            if (playerId.HasValue)
            {
                request.AddQueryParameter(nameof(playerId), playerId.Value);
            }

            if (userProfileId.HasValue)
            {
                request.AddQueryParameter(nameof(userProfileId), userProfileId.Value);
            }

            if (gameType.HasValue)
            {
                request.AddQueryParameter(nameof(gameType), gameType.Value.ToString());
            }

            if (isActive.HasValue)
            {
                request.AddQueryParameter(nameof(isActive), isActive.Value);
            }

            request.AddQueryParameter(nameof(skipEntries), skipEntries);
            request.AddQueryParameter(nameof(takeEntries), takeEntries);

            var response = await ExecuteAsync(request).ConfigureAwait(false);
            return response.ToApiResult<CollectionModel<ConnectedPlayerDto>>();
        }

        public async Task<ApiResult<Cod4xAdminRosterDto>> GetCod4xAdminRoster(
            Guid gameServerId,
            CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/game-servers/{gameServerId}/connected-players/admin-roster", Method.Get).ConfigureAwait(false);

            var response = await ExecuteAsync(request).ConfigureAwait(false);
            return response.ToApiResult<Cod4xAdminRosterDto>();
        }

        public async Task<ApiResult> ForceUnlinkConnectedPlayer(
            Guid connectedPlayerProfileId,
            ForceUnlinkConnectedPlayerDto dto,
            CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/connected-players/{connectedPlayerProfileId}", Method.Delete).ConfigureAwait(false);
            request.AddJsonBody(dto);

            var response = await ExecuteAsync(request).ConfigureAwait(false);
            return response.ToApiResult();
        }
    }
}
