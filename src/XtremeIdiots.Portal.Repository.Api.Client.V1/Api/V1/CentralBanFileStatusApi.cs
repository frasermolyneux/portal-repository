using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.CentralBanFileStatus;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class CentralBanFileStatusApi : BaseApi<RepositoryApiClientOptions>, ICentralBanFileStatusApi
    {
        public CentralBanFileStatusApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options)
            : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<CentralBanFileStatusDto>> GetCentralBanFileStatus(GameType gameType, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/central-ban-file-status/{gameType}", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            return response.ToApiResult<CentralBanFileStatusDto>();
        }

        public async Task<ApiResult<CollectionModel<CentralBanFileStatusDto>>> GetCentralBanFileStatuses(CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/central-ban-file-status", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            return response.ToApiResult<CollectionModel<CentralBanFileStatusDto>>();
        }

        public async Task<ApiResult<CentralBanFileStatusDto>> UpsertCentralBanFileStatus(UpsertCentralBanFileStatusDto upsertDto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(upsertDto);

            var request = await CreateRequestAsync($"v1/central-ban-file-status/{upsertDto.GameType}", Method.Put).ConfigureAwait(false);
            request.AddJsonBody(upsertDto);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            return response.ToApiResult<CentralBanFileStatusDto>();
        }
    }
}
