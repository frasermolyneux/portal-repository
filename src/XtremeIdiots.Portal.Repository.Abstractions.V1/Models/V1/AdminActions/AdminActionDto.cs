using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions
{
    public record AdminActionDto : IDto
    {
        [JsonProperty]
        public Guid AdminActionId { get; internal set; }

        [JsonProperty]
        public Guid PlayerId { get; internal set; }

        [JsonProperty]
        public Guid? UserProfileId { get; internal set; }

        [JsonProperty]
        public int? ForumTopicId { get; internal set; }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public AdminActionType Type { get; internal set; }

        [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Text { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonProperty]
        public DateTime Created { get; internal set; }

        [JsonProperty]
        public DateTime? Expires { get; internal set; }

        [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public PlayerDto Player { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonProperty]
        public UserProfileDto? UserProfile { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(AdminActionId), AdminActionId.ToString() },
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(Type), Type.ToString() }
                };

                if (UserProfile is not null)
                    telemetryProperties.AddAdditionalProperties(UserProfile.TelemetryProperties);

                if (Player is not null)
                    telemetryProperties.AddAdditionalProperties(Player.TelemetryProperties);

                return telemetryProperties;
            }
        }
    }
}




