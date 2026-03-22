using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeNotificationPreferencesApi : INotificationPreferencesApi
{
    private readonly ConcurrentDictionary<string, NotificationPreferenceDto> _preferences = new();

    public FakeNotificationPreferencesApi AddPreference(NotificationPreferenceDto dto) { _preferences[$"{dto.UserProfileId}:{dto.NotificationTypeId}"] = dto; return this; }
    public FakeNotificationPreferencesApi Reset() { _preferences.Clear(); return this; }

    public Task<ApiResult<CollectionModel<NotificationPreferenceDto>>> GetNotificationPreferences(Guid userProfileId, CancellationToken cancellationToken = default)
    {
        var items = _preferences.Values.Where(p => p.UserProfileId == userProfileId).ToList();
        var collection = new CollectionModel<NotificationPreferenceDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<NotificationPreferenceDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<NotificationPreferenceDto>>(collection)));
    }

    public Task<ApiResult> UpdateNotificationPreferences(Guid userProfileId, List<EditNotificationPreferenceDto> editNotificationPreferenceDtos, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    }
}
