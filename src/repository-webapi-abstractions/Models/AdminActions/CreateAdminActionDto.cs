using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.AdminActions
{
    public class CreateAdminActionDto : IDto
    {
        public CreateAdminActionDto(Guid playerId, AdminActionType type, string text)
        {
            PlayerId = playerId;
            Type = type;
            Text = text;
        }

        [JsonProperty]
        public Guid PlayerId { get; private set; }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public AdminActionType Type { get; private set; }

        [JsonProperty]
        public string Text { get; private set; }

        [JsonProperty]
        public DateTime? Expires { get; set; }

        [JsonProperty]
        public int? ForumTopicId { get; set; }

        [JsonProperty]
        public string? AdminId { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(Type), Type.ToString() },
                    { nameof(AdminId), AdminId is not null ? AdminId.ToString() : string.Empty }
                };

                return telemetryProperties;
            }
        }
    }
}
