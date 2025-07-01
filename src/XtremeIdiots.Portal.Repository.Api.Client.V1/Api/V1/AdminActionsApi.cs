using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class AdminActionsApi : BaseApi, IAdminActionsApi
    {
        public AdminActionsApi(ILogger<AdminActionsApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<AdminActionDto>> GetAdminAction(Guid adminActionId)
        {
            var request = await CreateRequestAsync($"v1/admin-actions/{adminActionId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<AdminActionDto>();
        }

        public async Task<ApiResponseDto<AdminActionCollectionDto>> GetAdminActions(GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, int skipEntries, int takeEntries, AdminActionOrder? order)
        {
            var request = await CreateRequestAsync($"v1/admin-actions", Method.Get);

            if (gameType.HasValue)
                request.AddQueryParameter("gameType", gameType.ToString());

            if (playerId.HasValue)
                request.AddQueryParameter("playerId", playerId.ToString());

            if (!string.IsNullOrWhiteSpace(adminId))
                request.AddQueryParameter("adminId", adminId);

            if (filter.HasValue)
                request.AddQueryParameter("filter", filter.ToString());

            request.AddQueryParameter("takeEntries", takeEntries.ToString());
            request.AddQueryParameter("skipEntries", skipEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<AdminActionCollectionDto>();
        }

        public async Task<ApiResponseDto> CreateAdminAction(CreateAdminActionDto createAdminActionDto)
        {
            var request = await CreateRequestAsync($"v1/admin-actions", Method.Post);
            request.AddJsonBody(createAdminActionDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdateAdminAction(EditAdminActionDto editAdminActionDto)
        {
            var request = await CreateRequestAsync($"v1/admin-actions/{editAdminActionDto.AdminActionId}", Method.Patch);
            request.AddJsonBody(editAdminActionDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> DeleteAdminAction(Guid adminActionId)
        {
            var request = await CreateRequestAsync($"v1/admin-actions/{adminActionId}", Method.Delete);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
