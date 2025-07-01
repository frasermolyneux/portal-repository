using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Demos;

namespace XtremeIdiots.Portal.RepositoryApiClient.V1
{
    public class DemosApi : BaseApi, IDemosApi
    {
        public DemosApi(ILogger<DemosApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<DemoDto>> GetDemo(Guid demoId)
        {
            var request = await CreateRequestAsync($"v1/demos/{demoId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<DemoDto>();
        }

        public async Task<ApiResponseDto<DemosCollectionDto>> GetDemos(GameType[]? gameTypes, string? userId, string? filterString, int skipEntries, int takeEntries, DemoOrder? order)
        {
            var request = await CreateRequestAsync("v1/demos", Method.Get);

            if (gameTypes != null)
                request.AddQueryParameter("gameTypes", string.Join(",", gameTypes));

            if (!string.IsNullOrWhiteSpace(userId))
                request.AddQueryParameter("userId", userId);

            if (!string.IsNullOrWhiteSpace(filterString))
                request.AddQueryParameter("filterString", filterString);

            request.AddQueryParameter("takeEntries", takeEntries.ToString());
            request.AddQueryParameter("skipEntries", skipEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<DemosCollectionDto>();
        }

        public async Task<ApiResponseDto<DemoDto>> CreateDemo(CreateDemoDto createDemoDto)
        {
            var request = await CreateRequestAsync("v1/demos", Method.Post);
            request.AddJsonBody(createDemoDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<DemoDto>();
        }

        public async Task<ApiResponseDto> SetDemoFile(Guid demoId, string fileName, string filePath)
        {
            var request = await CreateRequestAsync($"v1/demos/{demoId}/file", Method.Post);
            request.AddFile(fileName, filePath);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> DeleteDemo(Guid demoId)
        {
            var request = await CreateRequestAsync($"v1/demos/{demoId}", Method.Delete);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
