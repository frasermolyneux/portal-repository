using Microsoft.Extensions.Logging;
using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1_1;
using XtremeIdiots.Portal.Repository.Abstractions.V1_1.Models.Root;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1_1
{
    public class RootApi : BaseApi<V1.RepositoryApiClientOptions>, IRootApi
    {
        public RootApi(
            ILogger<BaseApi<V1.RepositoryApiClientOptions>> logger,
            IApiTokenProvider? apiTokenProvider,
            IRestClientService restClientService,
            V1.RepositoryApiClientOptions options)
            : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<RootDto>> GetRoot(CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1.1/", Method.Get, cancellationToken);
            var response = await ExecuteAsync(request, cancellationToken);
            return response.ToApiResult<RootDto>();
        }
    }
}


