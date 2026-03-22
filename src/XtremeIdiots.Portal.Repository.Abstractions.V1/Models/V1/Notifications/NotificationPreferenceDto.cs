using Newtonsoft.Json;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications
{
    public record NotificationPreferenceDto : IDto
    {
        [JsonProperty]
        public Guid NotificationPreferenceId { get; internal set; }

        [JsonProperty]
        public Guid UserProfileId { get; internal set; }

        [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string NotificationTypeId { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonProperty]
        public bool InSiteEnabled { get; internal set; }

        [JsonProperty]
        public bool EmailEnabled { get; internal set; }

        [JsonProperty]
        public DateTime Created { get; internal set; }

        [JsonProperty]
        public DateTime LastModified { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(NotificationPreferenceId), NotificationPreferenceId.ToString() },
            { nameof(UserProfileId), UserProfileId.ToString() },
            { nameof(NotificationTypeId), NotificationTypeId ?? string.Empty }
        };
    }
}
