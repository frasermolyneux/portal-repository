using Newtonsoft.Json;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications
{
    public record EditNotificationPreferenceDto : IDto
    {
        public EditNotificationPreferenceDto(string notificationTypeId)
        {
            NotificationTypeId = notificationTypeId;
        }

        [JsonProperty]
        public string NotificationTypeId { get; private set; }

        [JsonProperty]
        public bool? InSiteEnabled { get; set; }

        [JsonProperty]
        public bool? EmailEnabled { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(NotificationTypeId), NotificationTypeId ?? string.Empty }
        };
    }
}
