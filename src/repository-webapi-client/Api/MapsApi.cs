﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Maps;

namespace XtremeIdiots.Portal.RepositoryApiClient.Api
{
    public class MapsApi : BaseApi, IMapsApi
    {
        public MapsApi(ILogger<MapsApi> logger, IApiTokenProvider apiTokenProvider, IMemoryCache memoryCache, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {

        }

        public async Task<ApiResponseDto<MapDto>> GetMap(Guid mapId)
        {
            var request = await CreateRequestAsync($"maps/{mapId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<MapDto>();
        }

        public async Task<ApiResponseDto<MapDto>> GetMap(GameType gameType, string mapName)
        {
            var request = await CreateRequestAsync($"maps/{gameType}/{mapName}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<MapDto>();
        }

        public async Task<ApiResponseDto<MapsCollectionDto>> GetMaps(GameType? gameType, string[]? mapNames, MapsFilter? filter, string? filterString, int skipEntries, int takeEntries, MapsOrder? order)
        {
            var request = await CreateRequestAsync("maps", Method.Get);

            if (gameType.HasValue)
                request.AddQueryParameter("gameType", gameType.ToString());

            if (mapNames != null && mapNames.Length > 0)
                request.AddQueryParameter("mapNames", string.Join(",", mapNames));

            if (filter.HasValue)
                request.AddQueryParameter("filter", filter.ToString());

            if (!string.IsNullOrEmpty(filterString))
                request.AddQueryParameter("filterString", filterString);

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<MapsCollectionDto>();
        }

        public async Task<ApiResponseDto> CreateMap(CreateMapDto createMapDto)
        {
            var request = await CreateRequestAsync("maps", Method.Post);
            request.AddJsonBody(new List<CreateMapDto> { createMapDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> CreateMaps(List<CreateMapDto> createMapDtos)
        {
            var request = await CreateRequestAsync("maps", Method.Post);
            request.AddJsonBody(createMapDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdateMap(EditMapDto editMapDto)
        {
            var request = await CreateRequestAsync("maps", Method.Put);
            request.AddJsonBody(new List<EditMapDto> { editMapDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdateMaps(List<EditMapDto> editMapDtos)
        {
            var request = await CreateRequestAsync("maps", Method.Put);
            request.AddJsonBody(editMapDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> DeleteMap(Guid mapId)
        {
            var request = await CreateRequestAsync($"maps/{mapId}", Method.Delete);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> RebuildMapPopularity()
        {
            var request = await CreateRequestAsync($"maps/popularity", Method.Post);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpsertMapVote(UpsertMapVoteDto upsertMapVoteDto)
        {
            var request = await CreateRequestAsync($"maps/votes", Method.Post);
            request.AddJsonBody(new List<UpsertMapVoteDto> { upsertMapVoteDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpsertMapVotes(List<UpsertMapVoteDto> upsertMapVoteDtos)
        {
            var request = await CreateRequestAsync($"maps/votes", Method.Post);
            request.AddJsonBody(upsertMapVoteDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdateMapImage(Guid mapId, string filePath)
        {
            var request = await CreateRequestAsync($"maps/{mapId}/image", Method.Post);
            request.AddFile("map.jpg", filePath);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
