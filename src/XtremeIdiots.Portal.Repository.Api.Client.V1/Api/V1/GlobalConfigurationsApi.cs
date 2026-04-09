using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class GlobalConfigurationsApi : BaseApi<RepositoryApiClientOptions>, IGlobalConfigurationsApi
    {
        public GlobalConfigurationsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<CollectionModel<ConfigurationDto>>> GetConfigurations(CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/configurations", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<CollectionModel<ConfigurationDto>>();
        }

        public async Task<ApiResult<ConfigurationDto>> GetConfiguration(string ns, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/configurations/{Uri.EscapeDataString(ns)}", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<ConfigurationDto>();
        }

        public async Task<ApiResult> UpsertConfiguration(string ns, UpsertConfigurationDto dto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/configurations/{Uri.EscapeDataString(ns)}", Method.Put).ConfigureAwait(false);
            request.AddJsonBody(dto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeleteConfiguration(string ns, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/configurations/{Uri.EscapeDataString(ns)}", Method.Delete).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }
    }
}
