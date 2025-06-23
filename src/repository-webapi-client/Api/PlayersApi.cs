using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Tags;

namespace XtremeIdiots.Portal.RepositoryApiClient.Api
{
    public class PlayersApi : BaseApi, IPlayersApi
    {
        public PlayersApi(ILogger<PlayersApi> logger, IApiTokenProvider apiTokenProvider, IMemoryCache memoryCache, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {

        }
        public async Task<ApiResponseDto<PlayerDto>> GetPlayer(Guid playerId, PlayerEntityOptions playerEntityOptions)
        {
            var request = await CreateRequestAsync($"players/{playerId}", Method.Get);
            request.AddQueryParameter(nameof(playerEntityOptions), playerEntityOptions);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<PlayerDto>();
        }

        public async Task<ApiResponseDto> HeadPlayerByGameType(GameType gameType, string guid)
        {
            var request = await CreateRequestAsync($"players/by-game-type/{gameType}/{guid}", Method.Head);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto<PlayerDto>> GetPlayerByGameType(GameType gameType, string guid, PlayerEntityOptions playerEntityOptions)
        {
            var request = await CreateRequestAsync($"players/by-game-type/{gameType}/{guid}", Method.Get);
            request.AddQueryParameter(nameof(playerEntityOptions), playerEntityOptions);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<PlayerDto>();
        }

        public async Task<ApiResponseDto<PlayersCollectionDto>> GetPlayersWithIpAddress(string ipAddress, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions)
        {
            var request = await CreateRequestAsync($"players/with-ip-address/{ipAddress}", Method.Get);

            request.AddQueryParameter(nameof(skipEntries), skipEntries);
            request.AddQueryParameter(nameof(takeEntries), takeEntries);

            if (order.HasValue)
                request.AddQueryParameter(nameof(order), order.Value);

            request.AddQueryParameter(nameof(playerEntityOptions), playerEntityOptions);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<PlayersCollectionDto>();
        }

        public async Task<ApiResponseDto<PlayersCollectionDto>> GetPlayers(GameType? gameType, PlayersFilter? filter, string? filterString, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions)
        {
            var request = await CreateRequestAsync("players", Method.Get);
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

            return response.ToApiResponse<PlayersCollectionDto>();
        }

        public async Task<ApiResponseDto> CreatePlayer(CreatePlayerDto createPlayerDto)
        {
            var request = await CreateRequestAsync("players", Method.Post);
            request.AddJsonBody(new List<CreatePlayerDto> { createPlayerDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> CreatePlayers(List<CreatePlayerDto> createPlayerDtos)
        {
            var request = await CreateRequestAsync("players", Method.Post);
            request.AddJsonBody(createPlayerDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdatePlayer(EditPlayerDto editPlayerDto)
        {
            var request = await CreateRequestAsync($"players/{editPlayerDto.PlayerId}", Method.Patch);
            request.AddJsonBody(editPlayerDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        #region Protected Names

        public async Task<ApiResponseDto<ProtectedNamesCollectionDto>> GetProtectedNames(int skipEntries, int takeEntries)
        {
            var request = await CreateRequestAsync("players/protected-names", Method.Get);

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<ProtectedNamesCollectionDto>();
        }

        public async Task<ApiResponseDto<ProtectedNameDto>> GetProtectedName(Guid protectedNameId)
        {
            var request = await CreateRequestAsync($"players/protected-names/{protectedNameId}", Method.Get);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<ProtectedNameDto>();
        }

        public async Task<ApiResponseDto<ProtectedNamesCollectionDto>> GetProtectedNamesForPlayer(Guid playerId)
        {
            var request = await CreateRequestAsync($"players/{playerId}/protected-names", Method.Get);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<ProtectedNamesCollectionDto>();
        }

        public async Task<ApiResponseDto> CreateProtectedName(CreateProtectedNameDto createProtectedNameDto)
        {
            var request = await CreateRequestAsync("players/protected-names", Method.Post);
            request.AddJsonBody(createProtectedNameDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> DeleteProtectedName(DeleteProtectedNameDto deleteProtectedNameDto)
        {
            var request = await CreateRequestAsync($"players/protected-names/{deleteProtectedNameDto.ProtectedNameId}", Method.Delete);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto<ProtectedNameUsageReportDto>> GetProtectedNameUsageReport(Guid protectedNameId)
        {
            var request = await CreateRequestAsync($"players/protected-names/{protectedNameId}/usage-report", Method.Get);

            var response = await ExecuteAsync(request); return response.ToApiResponse<ProtectedNameUsageReportDto>();
        }

        #endregion

        #region Player Tags        
        public async Task<ApiResponseDto<PlayerTagsCollectionDto>> GetPlayerTags(Guid playerId)
        {
            var request = await CreateRequestAsync($"players/{playerId}/tags", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<PlayerTagsCollectionDto>();
        }

        public async Task<ApiResponseDto<IpAddressesCollectionDto>> GetPlayerIpAddresses(Guid playerId, int skipEntries, int takeEntries, IpAddressesOrder? order)
        {
            var request = await CreateRequestAsync($"players/{playerId}/ip-addresses", Method.Get);

            request.AddQueryParameter(nameof(skipEntries), skipEntries);
            request.AddQueryParameter(nameof(takeEntries), takeEntries);

            if (order.HasValue)
                request.AddQueryParameter(nameof(order), order.Value);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<IpAddressesCollectionDto>();
        }

        public async Task<ApiResponseDto> AddPlayerTag(Guid playerId, PlayerTagDto playerTagDto)
        {
            var request = await CreateRequestAsync($"players/{playerId}/tags", Method.Post);
            request.AddJsonBody(playerTagDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
        public async Task<ApiResponseDto> RemovePlayerTag(Guid playerId, Guid playerTagId)
        {
            var request = await CreateRequestAsync($"players/{playerId}/tags/{playerTagId}", Method.Delete);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto<PlayerTagDto>> GetPlayerTagById(Guid playerTagId)
        {
            var request = await CreateRequestAsync($"players/tags/{playerTagId}", Method.Get);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<PlayerTagDto>();
        }

        public async Task<ApiResponseDto> RemovePlayerTagById(Guid playerTagId)
        {
            var request = await CreateRequestAsync($"players/tags/{playerTagId}", Method.Delete);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        #endregion

        #region Player Aliases methods
        public async Task<ApiResponseDto<PlayerAliasesCollectionDto>> GetPlayerAliases(Guid playerId, int skipEntries, int takeEntries)
        {
            var request = await CreateRequestAsync($"players/{playerId}/aliases", Method.Get);
            request.AddQueryParameter(nameof(skipEntries), skipEntries);
            request.AddQueryParameter(nameof(takeEntries), takeEntries);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<PlayerAliasesCollectionDto>();
        }

        public async Task<ApiResponseDto> AddPlayerAlias(Guid playerId, CreatePlayerAliasDto createPlayerAliasDto)
        {
            var request = await CreateRequestAsync($"players/{playerId}/aliases", Method.Post);
            request.AddJsonBody(createPlayerAliasDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdatePlayerAlias(Guid playerId, Guid aliasId, CreatePlayerAliasDto updatePlayerAliasDto)
        {
            var request = await CreateRequestAsync($"players/{playerId}/aliases/{aliasId}", Method.Put);
            request.AddJsonBody(updatePlayerAliasDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> DeletePlayerAlias(Guid playerId, Guid aliasId)
        {
            var request = await CreateRequestAsync($"players/{playerId}/aliases/{aliasId}", Method.Delete);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto<PlayerAliasesCollectionDto>> SearchPlayersByAlias(string aliasSearch, int skipEntries, int takeEntries)
        {
            var request = await CreateRequestAsync($"aliases/search", Method.Get);
            request.AddQueryParameter(nameof(aliasSearch), aliasSearch);
            request.AddQueryParameter(nameof(skipEntries), skipEntries);
            request.AddQueryParameter(nameof(takeEntries), takeEntries);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<PlayerAliasesCollectionDto>();
        }
        #endregion
    }
}