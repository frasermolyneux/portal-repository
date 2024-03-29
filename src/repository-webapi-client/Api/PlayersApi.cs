﻿
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

namespace XtremeIdiots.Portal.RepositoryApiClient.Api
{
    public class PlayersApi : BaseApi, IPlayersApi
    {
        public PlayersApi(ILogger<PlayersApi> logger, IApiTokenProvider apiTokenProvider, IMemoryCache memoryCache, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {

        }
        public async Task<ApiResponseDto<PlayerDto>> GetPlayer(Guid playerId, PlayerEntityOptions playerEntityOptions)
        {
            var request = await CreateRequest($"players/{playerId}", Method.Get);
            request.AddQueryParameter(nameof(playerEntityOptions), playerEntityOptions);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<PlayerDto>();
        }

        public async Task<ApiResponseDto> HeadPlayerByGameType(GameType gameType, string guid)
        {
            var request = await CreateRequest($"players/by-game-type/{gameType}/{guid}", Method.Head);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto<PlayerDto>> GetPlayerByGameType(GameType gameType, string guid, PlayerEntityOptions playerEntityOptions)
        {
            var request = await CreateRequest($"players/by-game-type/{gameType}/{guid}", Method.Get);
            request.AddQueryParameter(nameof(playerEntityOptions), playerEntityOptions);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<PlayerDto>();
        }

        public async Task<ApiResponseDto<PlayersCollectionDto>> GetPlayers(GameType? gameType, PlayersFilter? filter, string? filterString, int skipEntries, int takeEntries, PlayersOrder? order, PlayerEntityOptions playerEntityOptions)
        {
            var request = await CreateRequest("players", Method.Get);
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
            var request = await CreateRequest("players", Method.Post);
            request.AddJsonBody(new List<CreatePlayerDto> { createPlayerDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> CreatePlayers(List<CreatePlayerDto> createPlayerDtos)
        {
            var request = await CreateRequest("players", Method.Post);
            request.AddJsonBody(createPlayerDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdatePlayer(EditPlayerDto editPlayerDto)
        {
            var request = await CreateRequest($"players/{editPlayerDto.PlayerId}", Method.Patch);
            request.AddJsonBody(editPlayerDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}