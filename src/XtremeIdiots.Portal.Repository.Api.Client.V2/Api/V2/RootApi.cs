using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;

namespace XtremeIdiots.Portal.Repository.Api.Client.V2
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

        public async Task<ApiResult> GetRoot()
        {
            var request = await CreateRequestAsync($"v2/", Method.Post).ConfigureAwait(false);
            var response = await ExecuteAsync(request).ConfigureAwait(false);

            return response.ToApiResult();
        }
    }
}
