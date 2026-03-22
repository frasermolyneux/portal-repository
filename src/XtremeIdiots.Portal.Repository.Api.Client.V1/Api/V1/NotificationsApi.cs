using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class NotificationsApi : BaseApi<RepositoryApiClientOptions>, INotificationsApi
    {
        public NotificationsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<CollectionModel<NotificationDto>>> GetNotifications(Guid userProfileId, bool? unreadOnly, int skipEntries, int takeEntries, NotificationOrder? order, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/notifications/{userProfileId}", Method.Get).ConfigureAwait(false);

            if (unreadOnly.HasValue)
                request.AddQueryParameter("unreadOnly", unreadOnly.ToString());

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<CollectionModel<NotificationDto>>();
        }

        public async Task<ApiResult<int>> GetUnreadNotificationCount(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/notifications/{userProfileId}/unread-count", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<int>();
        }

        public async Task<ApiResult> CreateNotification(CreateNotificationDto createNotificationDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/notifications", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(createNotificationDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> MarkNotificationAsRead(Guid notificationId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/notifications/{notificationId}/read", Method.Patch).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> MarkAllNotificationsAsRead(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/notifications/{userProfileId}/read-all", Method.Patch).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }
    }
}
