using System;

using Microsoft.Extensions.Logging;


using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapPacks;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class MapPacksApi : BaseApi<RepositoryApiClientOptions>, IMapPacksApi
    {
        public MapPacksApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {

        }

        public async Task<ApiResult<MapPackDto>> GetMapPack(Guid mapPackId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/maps/pack/{mapPackId}", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<MapPackDto>();
        }

        public async Task<ApiResult<CollectionModel<MapPackDto>>> GetMapPacks(GameType[]? gameTypes, Guid[]? gameServerIds, MapPacksFilter? filter, int skipEntries, int takeEntries, MapPacksOrder? order, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/maps/pack", Method.Get).ConfigureAwait(false);

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

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<CollectionModel<MapPackDto>>();
        }

        public async Task<ApiResult> CreateMapPack(CreateMapPackDto createMapPackDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/maps/pack", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(new List<CreateMapPackDto> { createMapPackDto });

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> CreateMapPacks(List<CreateMapPackDto> createMapPackDtos, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/maps/pack", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(createMapPackDtos);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpdateMapPack(UpdateMapPackDto updateMapPackDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/maps/pack/{updateMapPackDto.MapPackId}", Method.Patch).ConfigureAwait(false);
            request.AddJsonBody(updateMapPackDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeleteMapPack(Guid mapPackId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/maps/pack/{mapPackId}", Method.Delete).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }
    }
}


