using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;

namespace XtremeIdiots.Portal.RepositoryApiClient.Api
{
    public class RootApi : BaseApi, IRootApi
    {
        public RootApi(ILogger<RootApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options) : base(logger, apiTokenProvider, options)
        {
        }

        public async Task<ApiResponseDto> GetRoot()
        {
            var request = await CreateRequest($"/", Method.Post);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
