using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class BanFileMonitorsApi : BaseApi<RepositoryApiClientOptions>, IBanFileMonitorsApi
    {
        public BanFileMonitorsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options)
            : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<BanFileMonitorDto>> GetBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/ban-file-monitors/{banFileMonitorId}", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<BanFileMonitorDto>();
        }

        public async Task<ApiResult<CollectionModel<BanFileMonitorDto>>> GetBanFileMonitors(GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId, int skipEntries, int takeEntries, BanFileMonitorOrder? order, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/ban-file-monitors", Method.Get).ConfigureAwait(false);

            if (gameTypes != null)
                request.AddQueryParameter("gameTypes", string.Join(",", gameTypes));

            if (banFileMonitorIds != null)
                request.AddQueryParameter("banFileMonitorIds", string.Join(",", banFileMonitorIds));

            if (gameServerId.HasValue)
                request.AddQueryParameter("gameServerId", gameServerId.ToString());

            request.AddQueryParameter("takeEntries", takeEntries.ToString());
            request.AddQueryParameter("skipEntries", skipEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<CollectionModel<BanFileMonitorDto>>();
        }

        public async Task<ApiResult<BanFileMonitorDto>> UpsertBanFileMonitorStatus(UpsertBanFileMonitorStatusDto upsertDto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(upsertDto);

            var request = await CreateRequestAsync($"v1/ban-file-monitors/by-game-server/{upsertDto.GameServerId}/status", Method.Put).ConfigureAwait(false);
            request.AddJsonBody(upsertDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            return response.ToApiResult<BanFileMonitorDto>();
        }
    }
}
