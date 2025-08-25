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
        public BanFileMonitorsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<BanFileMonitorDto>> GetBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/ban-file-monitors/{banFileMonitorId}", Method.Get);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<BanFileMonitorDto>();
        }

        public async Task<ApiResult<CollectionModel<BanFileMonitorDto>>> GetBanFileMonitors(GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId, int skipEntries, int takeEntries, BanFileMonitorOrder? order, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/ban-file-monitors", Method.Get);

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

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<CollectionModel<BanFileMonitorDto>>();
        }

        public async Task<ApiResult> CreateBanFileMonitor(CreateBanFileMonitorDto createBanFileMonitorDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/ban-file-monitors", Method.Post);
            request.AddJsonBody(createBanFileMonitorDto);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpdateBanFileMonitor(EditBanFileMonitorDto editBanFileMonitorDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/ban-file-monitors/{editBanFileMonitorDto.BanFileMonitorId}", Method.Patch);
            request.AddJsonBody(editBanFileMonitorDto);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeleteBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/ban-file-monitors/{banFileMonitorId}", Method.Delete);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }
    }
}


