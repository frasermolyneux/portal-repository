using Newtonsoft.Json;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications
{
    public record CreateNotificationDto : IDto
    {
        public CreateNotificationDto(Guid userProfileId, string notificationTypeId, string title, string message)
        {
            UserProfileId = userProfileId;
            NotificationTypeId = notificationTypeId;
            Title = title;
            Message = message;
        }

        [JsonProperty]
        public Guid UserProfileId { get; private set; }

        [JsonProperty]
        public string NotificationTypeId { get; private set; }

        [JsonProperty]
        public string Title { get; private set; }

        [JsonProperty]
        public string Message { get; private set; }

        [JsonProperty]
        public string? ActionUrl { get; set; }

        [JsonProperty]
        public string? MetadataJson { get; set; }

        [JsonProperty]
        public bool EmailSent { get; set; }

        [JsonProperty]
        public DateTime? EmailSentAt { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(UserProfileId), UserProfileId.ToString() },
            { nameof(NotificationTypeId), NotificationTypeId ?? string.Empty }
        };
    }
}
