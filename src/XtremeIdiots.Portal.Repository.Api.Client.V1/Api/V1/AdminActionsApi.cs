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
            var request = await CreateRequestAsync($"v1/admin-actions/{adminActionId}", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<AdminActionDto>();
        }

        public Task<ApiResult<CollectionModel<AdminActionDto>>> GetAdminActions(GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, int skipEntries, int takeEntries, AdminActionOrder? order, CancellationToken cancellationToken = default)
            => GetAdminActions(gameType, playerId, adminId, filter, skipEntries, takeEntries, order, null, null, null, cancellationToken);

        public async Task<ApiResult<CollectionModel<AdminActionDto>>> GetAdminActions(GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, int skipEntries, int takeEntries, AdminActionOrder? order, ActionSource? source, AutomationFeature? automationFeature, string? automationRuleId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/admin-actions", Method.Get).ConfigureAwait(false);

            if (gameType.HasValue)
            {
                request.AddQueryParameter("gameType", gameType.ToString());
            }

            if (playerId.HasValue)
            {
                request.AddQueryParameter("playerId", playerId.ToString());
            }

            if (!string.IsNullOrWhiteSpace(adminId))
            {
                request.AddQueryParameter("adminId", adminId);
            }

            if (filter.HasValue)
            {
                request.AddQueryParameter("filter", filter.ToString());
            }

            request.AddQueryParameter("takeEntries", takeEntries.ToString());
            request.AddQueryParameter("skipEntries", skipEntries.ToString());

            if (order.HasValue)
            {
                request.AddQueryParameter("order", order.ToString());
            }

            if (source.HasValue)
            {
                request.AddQueryParameter("source", source.ToString());
            }

            if (automationFeature.HasValue)
            {
                request.AddQueryParameter("automationFeature", automationFeature.ToString());
            }

            if (!string.IsNullOrWhiteSpace(automationRuleId))
            {
                request.AddQueryParameter("automationRuleId", automationRuleId);
            }

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<CollectionModel<AdminActionDto>>();
        }

        public async Task<ApiResult<CollectionModel<ActiveBanCountsDto>>> GetActiveBanCounts(GameType? gameType, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/admin-actions/active-ban-counts", Method.Get).ConfigureAwait(false);

            if (gameType.HasValue)
            {
                request.AddQueryParameter("gameType", gameType.ToString());
            }

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            return response.ToApiResult<CollectionModel<ActiveBanCountsDto>>();
        }

        public async Task<ApiResult> CreateAdminAction(CreateAdminActionDto createAdminActionDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/admin-actions", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(createAdminActionDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult<EnsureAutomatedActionResultDto>> EnsureAutomatedAction(EnsureAutomatedActionDto ensureAutomatedActionDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/admin-actions/ensure-automated", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(ensureAutomatedActionDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<EnsureAutomatedActionResultDto>();
        }

        public async Task<ApiResult<ForumTopicPublicationClaimResultDto>> ClaimForumTopicPublication(Guid adminActionId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/admin-actions/{adminActionId}/forum-topic-publication/claim", Method.Post).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<ForumTopicPublicationClaimResultDto>();
        }

        public async Task<ApiResult> CompleteForumTopicPublication(Guid adminActionId, CompleteForumTopicPublicationDto dto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/admin-actions/{adminActionId}/forum-topic-publication/complete", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(dto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            return response.ToApiResult();
        }

        public async Task<ApiResult> UpdateAdminAction(EditAdminActionDto editAdminActionDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/admin-actions/{editAdminActionDto.AdminActionId}", Method.Patch).ConfigureAwait(false);
            request.AddJsonBody(editAdminActionDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeleteAdminAction(Guid adminActionId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/admin-actions/{adminActionId}", Method.Delete).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }
    }
}


