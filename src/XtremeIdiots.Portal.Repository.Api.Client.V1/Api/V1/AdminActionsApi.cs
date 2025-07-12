using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class AdminActionsApi : BaseApi<RepositoryApiClientOptions>, IAdminActionsApi
    {
        public AdminActionsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<AdminActionDto>> GetAdminAction(Guid adminActionId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/admin-actions/{adminActionId}", Method.Get);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<AdminActionDto>();
        }

        public async Task<ApiResult<CollectionModel<AdminActionDto>>> GetAdminActions(GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, int skipEntries, int takeEntries, AdminActionOrder? order, CancellationToken cancellationToken = default)
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

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<CollectionModel<AdminActionDto>>();
        }

        public async Task<ApiResult> CreateAdminAction(CreateAdminActionDto createAdminActionDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/admin-actions", Method.Post);
            request.AddJsonBody(createAdminActionDto);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpdateAdminAction(EditAdminActionDto editAdminActionDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/admin-actions/{editAdminActionDto.AdminActionId}", Method.Patch);
            request.AddJsonBody(editAdminActionDto);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeleteAdminAction(Guid adminActionId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/admin-actions/{adminActionId}", Method.Delete);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }
    }
}


