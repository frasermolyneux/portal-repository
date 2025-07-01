using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;
using RestSharp;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.MapPacks;

namespace XtremeIdiots.Portal.RepositoryApiClient.V1
{
    public class MapPacksApi : BaseApi, IMapPacksApi
    {
        public MapPacksApi(ILogger<MapsApi> logger, IApiTokenProvider apiTokenProvider, IMemoryCache memoryCache, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {

        }

        public async Task<ApiResponseDto<MapPackDto>> GetMapPack(Guid mapPackId)
        {
            var request = await CreateRequestAsync($"v1/maps/pack/{mapPackId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<MapPackDto>();
        }

        public async Task<ApiResponseDto<MapPackCollectionDto>> GetMapPacks(GameType[]? gameTypes, Guid[]? gameServerIds, MapPacksFilter? filter, int skipEntries, int takeEntries, MapPacksOrder? order)
        {
            var request = await CreateRequestAsync("v1/maps/pack", Method.Get);

            if (gameTypes != null)
                request.AddQueryParameter("gameTypes", string.Join(",", gameTypes));

            if (gameServerIds != null)
                request.AddQueryParameter("gameServerIds", string.Join(",", gameServerIds));

            if (filter.HasValue)
                request.AddQueryParameter("filter", filter.ToString());

            request.AddQueryParameter("takeEntries", takeEntries.ToString());
            request.AddQueryParameter("skipEntries", skipEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<MapPackCollectionDto>();
        }

        public async Task<ApiResponseDto> CreateMapPack(CreateMapPackDto createMapPackDto)
        {
            var request = await CreateRequestAsync("v1/maps/pack", Method.Post);
            request.AddJsonBody(new List<CreateMapPackDto> { createMapPackDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> CreateMapPacks(List<CreateMapPackDto> createMapPackDtos)
        {
            var request = await CreateRequestAsync("v1/maps/pack", Method.Post);
            request.AddJsonBody(createMapPackDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdateMapPack(UpdateMapPackDto updateMapPackDto)
        {
            var request = await CreateRequestAsync($"v1/maps/pack/{updateMapPackDto.MapPackId}", Method.Patch);
            request.AddJsonBody(updateMapPackDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> DeleteMapPack(Guid mapPackId)
        {
            var request = await CreateRequestAsync($"v1/maps/pack/{mapPackId}", Method.Delete);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
