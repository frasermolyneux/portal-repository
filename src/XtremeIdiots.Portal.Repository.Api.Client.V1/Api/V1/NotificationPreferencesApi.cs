using Microsoft.Extensions.Logging;

using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class NotificationPreferencesApi : BaseApi<RepositoryApiClientOptions>, INotificationPreferencesApi
    {
        public NotificationPreferencesApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<CollectionModel<NotificationPreferenceDto>>> GetNotificationPreferences(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/notification-preferences/{userProfileId}", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<CollectionModel<NotificationPreferenceDto>>();
        }

        public async Task<ApiResult> UpdateNotificationPreferences(Guid userProfileId, List<EditNotificationPreferenceDto> editNotificationPreferenceDtos, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/notification-preferences/{userProfileId}", Method.Put).ConfigureAwait(false);
            request.AddJsonBody(editNotificationPreferenceDtos);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }
    }
}
