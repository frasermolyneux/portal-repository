using Microsoft.Extensions.Logging;



using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Demos;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class DemosApi : BaseApi<RepositoryApiClientOptions>, IDemosApi
    {
        public DemosApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<DemoDto>> GetDemo(Guid demoId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/demos/{demoId}", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<DemoDto>();
        }

        public async Task<ApiResult<CollectionModel<DemoDto>>> GetDemos(GameType[]? gameTypes, string? userId, string? filterString, int skipEntries, int takeEntries, DemoOrder? order, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/demos", Method.Get).ConfigureAwait(false);

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

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<CollectionModel<DemoDto>>();
        }

        public async Task<ApiResult<DemoDto>> CreateDemo(CreateDemoDto createDemoDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/demos", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(createDemoDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<DemoDto>();
        }

        public async Task<ApiResult> SetDemoFile(Guid demoId, string fileName, string filePath, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/demos/{demoId}/file", Method.Post).ConfigureAwait(false);
            request.AddFile(fileName, filePath);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeleteDemo(Guid demoId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/demos/{demoId}", Method.Delete).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }
    }
}


