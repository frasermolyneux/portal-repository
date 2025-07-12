using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class PlayersApi : BaseApi<RepositoryApiClientOptions>, IPlayersApi
    {
        public PlayersApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<PlayerDto>> GetPlayer(Guid playerId, PlayerEntityOptions playerEntityOptions)
        {
            var request = await CreateRequestAsync($"v1/players/{playerId}", Method.Get);
            request.AddQueryParameter(nameof(playerEntityOptions), playerEntityOptions);

            var response = await ExecuteAsync(request);

            return response.ToApiResult<PlayerDto>();
        }

        public async Task<ApiResult> HeadPlayerByGameType(GameType gameType, string guid)
        {
            var request = await CreateRequestAsync($"v1/players/by-game-type/{gameType}/{guid}", Method.Head);
            var response = await ExecuteAsync(request);

            return response.ToApiResult();
        }

        public async Task<ApiResult<PlayerDto>> GetPlayerByGameType(GameType gameType, string guid, PlayerEntityOptions playerEntityOptions)
        {
            var request = await CreateRequestAsync($"v1/players/by-game-type/{gameType}/{guid}", Method.Get);
            request.AddQueryParameter(nameof(playerEntityOptions), playerEntityOptions);

            var response = await ExecuteAsync(request);

            return response.ToApiResult<PlayerDto>();
        }

        public async Task<ApiResult<CollectionModel<PlayerDto>>> GetPlayers(GameType? gameType, PlayersFilter? filter, string? filterString, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions)
        {
            var request = await CreateRequestAsync("v1/players", Method.Get);
            request.AddQueryParameter(nameof(playerEntityOptions), playerEntityOptions);

            if (gameType.HasValue)
                request.AddQueryParameter("gameType", gameType.ToString());

            if (filter.HasValue)
                request.AddQueryParameter("filter", filter.ToString());

            if (!string.IsNullOrWhiteSpace(filterString))
                request.AddQueryParameter("filterString", filterString);

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResult<CollectionModel<PlayerDto>>();
        }

        public async Task<ApiResult<CollectionModel<PlayerDto>>> GetPlayersWithIpAddress(string ipAddress, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions)
        {
            var request = await CreateRequestAsync("v1/players/by-ip-address", Method.Get);
            request.AddQueryParameter(nameof(playerEntityOptions), playerEntityOptions);
            request.AddQueryParameter(nameof(ipAddress), ipAddress);

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResult<CollectionModel<PlayerDto>>();
        }

        public async Task<ApiResult> CreatePlayer(CreatePlayerDto createPlayerDto)
        {
            var request = await CreateRequestAsync("v1/players", Method.Post);
            request.AddJsonBody(new List<CreatePlayerDto> { createPlayerDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResult();
        }

        public async Task<ApiResult> CreatePlayers(List<CreatePlayerDto> createPlayerDtos)
        {
            var request = await CreateRequestAsync("v1/players", Method.Post);
            request.AddJsonBody(createPlayerDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpdatePlayer(EditPlayerDto editPlayerDto)
        {
            var request = await CreateRequestAsync($"v1/players/{editPlayerDto.PlayerId}", Method.Patch);
            request.AddJsonBody(editPlayerDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResult();
        }

        #region Protected Names

        public async Task<ApiResult<CollectionModel<ProtectedNameDto>>> GetProtectedNames(int skipEntries, int takeEntries)
        {
            var request = await CreateRequestAsync("v1/players/protected-names", Method.Get);

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResult<CollectionModel<ProtectedNameDto>>();
        }

        public async Task<ApiResult<ProtectedNameDto>> GetProtectedName(Guid protectedNameId)
        {
            var request = await CreateRequestAsync($"v1/players/protected-names/{protectedNameId}", Method.Get);

            var response = await ExecuteAsync(request);

            return response.ToApiResult<ProtectedNameDto>();
        }

        public async Task<ApiResult<CollectionModel<ProtectedNameDto>>> GetProtectedNamesForPlayer(Guid playerId)
        {
            var request = await CreateRequestAsync($"v1/players/{playerId}/protected-names", Method.Get);

            var response = await ExecuteAsync(request);

            return response.ToApiResult<CollectionModel<ProtectedNameDto>>();
        }

        public async Task<ApiResult> CreateProtectedName(CreateProtectedNameDto createProtectedNameDto)
        {
            var request = await CreateRequestAsync("v1/players/protected-names", Method.Post);
            request.AddJsonBody(createProtectedNameDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeleteProtectedName(DeleteProtectedNameDto deleteProtectedNameDto)
        {
            var request = await CreateRequestAsync($"v1/players/protected-names/{deleteProtectedNameDto.ProtectedNameId}", Method.Delete);

            var response = await ExecuteAsync(request);

            return response.ToApiResult();
        }

        public async Task<ApiResult<ProtectedNameUsageReportDto>> GetProtectedNameUsageReport(Guid protectedNameId)
        {
            var request = await CreateRequestAsync($"v1/players/protected-names/{protectedNameId}/usage-report", Method.Get);

            var response = await ExecuteAsync(request);
            return response.ToApiResult<ProtectedNameUsageReportDto>();
        }

        #endregion

        #region Player Tags        
        public async Task<ApiResult<CollectionModel<PlayerTagDto>>> GetPlayerTags(Guid playerId)
        {
            var request = await CreateRequestAsync($"v1/players/{playerId}/tags", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResult<CollectionModel<PlayerTagDto>>();
        }

        public async Task<ApiResult<CollectionModel<IpAddressDto>>> GetPlayerIpAddresses(Guid playerId, int skipEntries, int takeEntries, IpAddressesOrder? order)
        {
            var request = await CreateRequestAsync($"v1/players/{playerId}/ip-addresses", Method.Get);

            request.AddQueryParameter(nameof(skipEntries), skipEntries);
            request.AddQueryParameter(nameof(takeEntries), takeEntries);

            if (order.HasValue)
                request.AddQueryParameter(nameof(order), order.Value);

            var response = await ExecuteAsync(request);

            return response.ToApiResult<CollectionModel<IpAddressDto>>();
        }

        public async Task<ApiResult> AddPlayerTag(Guid playerId, PlayerTagDto playerTagDto)
        {
            var request = await CreateRequestAsync($"v1/players/{playerId}/tags", Method.Post);
            request.AddJsonBody(playerTagDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResult();
        }
        public async Task<ApiResult> RemovePlayerTag(Guid playerId, Guid playerTagId)
        {
            var request = await CreateRequestAsync($"v1/players/{playerId}/tags/{playerTagId}", Method.Delete);

            var response = await ExecuteAsync(request);

            return response.ToApiResult();
        }

        public async Task<ApiResult<PlayerTagDto>> GetPlayerTagById(Guid playerTagId)
        {
            var request = await CreateRequestAsync($"v1/players/tags/{playerTagId}", Method.Get);

            var response = await ExecuteAsync(request);

            return response.ToApiResult<PlayerTagDto>();
        }

        public async Task<ApiResult> RemovePlayerTagById(Guid playerTagId)
        {
            var request = await CreateRequestAsync($"v1/players/tags/{playerTagId}", Method.Delete);

            var response = await ExecuteAsync(request);

            return response.ToApiResult();
        }

        #endregion

        #region Player Aliases methods
        public async Task<ApiResult<CollectionModel<PlayerAliasDto>>> GetPlayerAliases(Guid playerId, int skipEntries, int takeEntries)
        {
            var request = await CreateRequestAsync($"v1/players/{playerId}/aliases", Method.Get);
            request.AddQueryParameter(nameof(skipEntries), skipEntries);
            request.AddQueryParameter(nameof(takeEntries), takeEntries);

            var response = await ExecuteAsync(request);

            return response.ToApiResult<CollectionModel<PlayerAliasDto>>();
        }

        public async Task<ApiResult> AddPlayerAlias(Guid playerId, CreatePlayerAliasDto createPlayerAliasDto)
        {
            var request = await CreateRequestAsync($"v1/players/{playerId}/aliases", Method.Post);
            request.AddJsonBody(createPlayerAliasDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpdatePlayerAlias(Guid playerId, Guid aliasId, CreatePlayerAliasDto updatePlayerAliasDto)
        {
            var request = await CreateRequestAsync($"v1/players/{playerId}/aliases/{aliasId}", Method.Put);
            request.AddJsonBody(updatePlayerAliasDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeletePlayerAlias(Guid playerId, Guid aliasId)
        {
            var request = await CreateRequestAsync($"v1/players/{playerId}/aliases/{aliasId}", Method.Delete);

            var response = await ExecuteAsync(request);

            return response.ToApiResult();
        }

        public async Task<ApiResult<CollectionModel<PlayerAliasDto>>> SearchPlayersByAlias(string aliasSearch, int skipEntries, int takeEntries)
        {
            var request = await CreateRequestAsync($"v1/aliases/search", Method.Get);
            request.AddQueryParameter(nameof(aliasSearch), aliasSearch);
            request.AddQueryParameter(nameof(skipEntries), skipEntries);
            request.AddQueryParameter(nameof(takeEntries), takeEntries);

            var response = await ExecuteAsync(request);

            return response.ToApiResult<CollectionModel<PlayerAliasDto>>();
        }
        #endregion
    }
}
