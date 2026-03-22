using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface INotificationPreferencesApi
    {
        Task<ApiResult<CollectionModel<NotificationPreferenceDto>>> GetNotificationPreferences(Guid userProfileId, CancellationToken cancellationToken = default);
        Task<ApiResult> UpdateNotificationPreferences(Guid userProfileId, List<EditNotificationPreferenceDto> editNotificationPreferenceDtos, CancellationToken cancellationToken = default);
    }
}
