using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions
{
    public record CreateAdminActionDto : IDto
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
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(PlayerId), PlayerId.ToString() },
            { nameof(Type), Type.ToString() },
            { nameof(AdminId), AdminId ?? string.Empty }
        };
    }
}
