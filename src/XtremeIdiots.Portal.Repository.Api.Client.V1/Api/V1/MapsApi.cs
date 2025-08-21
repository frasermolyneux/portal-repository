
using Microsoft.Extensions.Logging;



using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class MapsApi : BaseApi<RepositoryApiClientOptions>, IMapsApi
    {
        public MapsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {

        }

        public async Task<ApiResult<MapDto>> GetMap(Guid mapId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/maps/{mapId}", Method.Get);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<MapDto>();
        }

        public async Task<ApiResult<MapDto>> GetMap(GameType gameType, string mapName, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/maps/{gameType}/{mapName}", Method.Get);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<MapDto>();
        }

        public async Task<ApiResult<CollectionModel<MapDto>>> GetMaps(GameType? gameType, string[]? mapNames, MapsFilter? filter, string? filterString, int skipEntries, int takeEntries, MapsOrder? order, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/maps", Method.Get);

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

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<CollectionModel<MapDto>>();
        }

        public async Task<ApiResult> CreateMap(CreateMapDto createMapDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/maps", Method.Post);
            request.AddJsonBody(new List<CreateMapDto> { createMapDto });

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> CreateMaps(List<CreateMapDto> createMapDtos, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/maps", Method.Post);
            request.AddJsonBody(createMapDtos);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpdateMap(EditMapDto editMapDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/maps", Method.Put);
            request.AddJsonBody(new List<EditMapDto> { editMapDto });

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpdateMaps(List<EditMapDto> editMapDtos, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/maps", Method.Put);
            request.AddJsonBody(editMapDtos);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeleteMap(Guid mapId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/maps/{mapId}", Method.Delete);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> RebuildMapPopularity(CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/maps/popularity", Method.Post);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpsertMapVote(UpsertMapVoteDto upsertMapVoteDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/maps/votes", Method.Post);
            request.AddJsonBody(new List<UpsertMapVoteDto> { upsertMapVoteDto });

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpsertMapVotes(List<UpsertMapVoteDto> upsertMapVoteDtos, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/maps/votes", Method.Post);
            request.AddJsonBody(upsertMapVoteDtos);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpdateMapImage(Guid mapId, string filePath, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/maps/{mapId}/image", Method.Post);
            request.AddFile("map.jpg", filePath);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> ClearMapImage(Guid mapId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/maps/{mapId}/image", Method.Delete);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }
    }
}


