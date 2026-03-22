using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface INotificationsApi
    {
        Task<ApiResult<CollectionModel<NotificationDto>>> GetNotifications(Guid userProfileId, bool? unreadOnly, int skipEntries, int takeEntries, NotificationOrder? order, CancellationToken cancellationToken = default);
        Task<ApiResult<int>> GetUnreadNotificationCount(Guid userProfileId, CancellationToken cancellationToken = default);
        Task<ApiResult> CreateNotification(CreateNotificationDto createNotificationDto, CancellationToken cancellationToken = default);
        Task<ApiResult> MarkNotificationAsRead(Guid notificationId, CancellationToken cancellationToken = default);
        Task<ApiResult> MarkAllNotificationsAsRead(Guid userProfileId, CancellationToken cancellationToken = default);
    }
}
