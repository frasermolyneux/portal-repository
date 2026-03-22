using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeNotificationTypesApi : INotificationTypesApi
{
    private readonly ConcurrentDictionary<string, NotificationTypeDto> _notificationTypes = new(StringComparer.OrdinalIgnoreCase);

    public FakeNotificationTypesApi AddNotificationType(NotificationTypeDto dto) { _notificationTypes[dto.NotificationTypeId] = dto; return this; }
    public FakeNotificationTypesApi Reset() { _notificationTypes.Clear(); return this; }

    public Task<ApiResult<CollectionModel<NotificationTypeDto>>> GetNotificationTypes(CancellationToken cancellationToken = default)
    {
        var items = _notificationTypes.Values.ToList();
        var collection = new CollectionModel<NotificationTypeDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<NotificationTypeDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<NotificationTypeDto>>(collection)));
    }
}
