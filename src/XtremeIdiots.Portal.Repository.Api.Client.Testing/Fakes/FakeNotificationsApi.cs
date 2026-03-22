using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeNotificationsApi : INotificationsApi
{
    private readonly ConcurrentDictionary<Guid, NotificationDto> _notifications = new();

    public FakeNotificationsApi AddNotification(NotificationDto dto) { _notifications[dto.NotificationId] = dto; return this; }
    public FakeNotificationsApi Reset() { _notifications.Clear(); return this; }

    public Task<ApiResult<CollectionModel<NotificationDto>>> GetNotifications(Guid userProfileId, bool? unreadOnly, int skipEntries, int takeEntries, NotificationOrder? order, CancellationToken cancellationToken = default)
    {
        var query = _notifications.Values.Where(n => n.UserProfileId == userProfileId);
        if (unreadOnly == true) query = query.Where(n => !n.IsRead);
        var items = query.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<NotificationDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<NotificationDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<NotificationDto>>(collection)));
    }

    public Task<ApiResult<int>> GetUnreadNotificationCount(Guid userProfileId, CancellationToken cancellationToken = default)
    {
        var count = _notifications.Values.Count(n => n.UserProfileId == userProfileId && !n.IsRead);
        return Task.FromResult(new ApiResult<int>(HttpStatusCode.OK, new ApiResponse<int>(count)));
    }

    public Task<ApiResult> CreateNotification(CreateNotificationDto createNotificationDto, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult(HttpStatusCode.Created, new ApiResponse()));
    }

    public Task<ApiResult> MarkNotificationAsRead(Guid notificationId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    }

    public Task<ApiResult> MarkAllNotificationsAsRead(Guid userProfileId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    }
}
