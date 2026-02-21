using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;
using XtremeIdiots.Portal.Repository.Abstractions.Models;

namespace XtremeIdiots.Portal.Repository.Api.Client.V2
{
    public class ApiInfoApi : BaseApi<RepositoryApiClientOptions>, IApiInfoApi
    {
        public ApiInfoApi(
            ILogger<BaseApi<RepositoryApiClientOptions>> logger,
            IApiTokenProvider? apiTokenProvider,
            IRestClientService restClientService,
            RepositoryApiClientOptions options)
            : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<ApiInfoDto>> GetApiInfo(CancellationToken cancellationToken = default)
        {
            try
            {
                var request = await CreateRequestAsync("v2/info", Method.Get, cancellationToken);
                var response = await ExecuteAsync(request, cancellationToken);

                return response.ToApiResult<ApiInfoDto>();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var errorResponse = new ApiResponse<ApiInfoDto>(
                    new ApiError("CLIENT_ERROR", "Failed to retrieve API info"));
                return new ApiResult<ApiInfoDto>(System.Net.HttpStatusCode.InternalServerError, errorResponse);
            }
        }
    }
}
