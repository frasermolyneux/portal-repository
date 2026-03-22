using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface INotificationTypesApi
    {
        Task<ApiResult<CollectionModel<NotificationTypeDto>>> GetNotificationTypes(CancellationToken cancellationToken = default);
    }
}
