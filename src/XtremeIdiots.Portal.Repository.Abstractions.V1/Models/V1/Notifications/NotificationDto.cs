using Newtonsoft.Json;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications
{
    public record NotificationDto : IDto
    {
        [JsonProperty]
        public Guid NotificationId { get; internal set; }

        [JsonProperty]
        public Guid UserProfileId { get; internal set; }

        [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string NotificationTypeId { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Title { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Message { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonProperty]
        public string? ActionUrl { get; internal set; }

        [JsonProperty]
        public string? MetadataJson { get; internal set; }

        [JsonProperty]
        public bool IsRead { get; internal set; }

        [JsonProperty]
        public DateTime? ReadAt { get; internal set; }

        [JsonProperty]
        public DateTime CreatedAt { get; internal set; }

        [JsonProperty]
        public bool EmailSent { get; internal set; }

        [JsonProperty]
        public DateTime? EmailSentAt { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(NotificationId), NotificationId.ToString() },
            { nameof(UserProfileId), UserProfileId.ToString() },
            { nameof(NotificationTypeId), NotificationTypeId ?? string.Empty }
        };
    }
}
