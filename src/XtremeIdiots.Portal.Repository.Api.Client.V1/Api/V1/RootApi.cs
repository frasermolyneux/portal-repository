using Microsoft.Extensions.Logging;
using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.V1.Models.Root;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class RootApi : BaseApi<RepositoryApiClientOptions>, IRootApi
    {
        public RootApi(
            ILogger<BaseApi<RepositoryApiClientOptions>> logger,
            IApiTokenProvider? apiTokenProvider,
            IRestClientService restClientService,
            RepositoryApiClientOptions options)
            : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<RootDto>> GetRoot(CancellationToken cancellationToken = default)
        {
            try
            {
                var request = await CreateRequestAsync($"v1/", Method.Get, cancellationToken).ConfigureAwait(false);
                var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
                return response.ToApiResult<RootDto>();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var errorResponse = new ApiResponse<RootDto>(
                    new ApiError("CLIENT_ERROR", "Failed to retrieve root information"));
                return new ApiResult<RootDto>(System.Net.HttpStatusCode.InternalServerError, errorResponse);
            }
        }
    }
}


