﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.BanFileMonitors;

namespace XtremeIdiots.Portal.RepositoryApiClient.Api
{
    public class BanFileMonitorsApi : BaseApi, IBanFileMonitorsApi
    {
        public BanFileMonitorsApi(ILogger<BanFileMonitorsApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<BanFileMonitorDto>> GetBanFileMonitor(Guid banFileMonitorId)
        {
            var request = await CreateRequestAsync($"ban-file-monitors/{banFileMonitorId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<BanFileMonitorDto>();
        }

        public async Task<ApiResponseDto<BanFileMonitorCollectionDto>> GetBanFileMonitors(GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId, int skipEntries, int takeEntries, BanFileMonitorOrder? order)
        {
            var request = await CreateRequestAsync("ban-file-monitors", Method.Get);

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

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<BanFileMonitorCollectionDto>();
        }

        public async Task<ApiResponseDto> CreateBanFileMonitor(CreateBanFileMonitorDto createBanFileMonitorDto)
        {
            var request = await CreateRequestAsync($"ban-file-monitors", Method.Post);
            request.AddJsonBody(createBanFileMonitorDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdateBanFileMonitor(EditBanFileMonitorDto editBanFileMonitorDto)
        {
            var request = await CreateRequestAsync($"ban-file-monitors/{editBanFileMonitorDto.BanFileMonitorId}", Method.Patch);
            request.AddJsonBody(editBanFileMonitorDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> DeleteBanFileMonitor(Guid banFileMonitorId)
        {
            var request = await CreateRequestAsync($"ban-file-monitors/{banFileMonitorId}", Method.Delete);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
