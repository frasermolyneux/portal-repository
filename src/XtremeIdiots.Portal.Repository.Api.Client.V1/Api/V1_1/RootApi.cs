using Microsoft.Extensions.Logging;
using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1_1;
using XtremeIdiots.Portal.Repository.Api.Client.V1;
using XtremeIdiots.Portal.Repository.Abstractions.V1.Models.Root;
using V1IRootApi = XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1.IRootApi;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1_1
{
    public class RootApi : BaseApi<RepositoryApiClientOptions>, IRootApi, V1IRootApi
    {
        public RootApi(
            ILogger<RootApi> logger,
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
                var request = await CreateRequestAsync($"v1.1/", Method.Get, cancellationToken);
                var response = await ExecuteAsync(request, cancellationToken);
                return response.ToApiResult<RootDto>();
            }
            catch (Exception)
            {
                var errorResponse = new ApiResponse<RootDto>(
                    new ApiError("CLIENT_ERROR", "Failed to retrieve root information"));
                return new ApiResult<RootDto>(System.Net.HttpStatusCode.InternalServerError, errorResponse);
            }
        }
    }
}


