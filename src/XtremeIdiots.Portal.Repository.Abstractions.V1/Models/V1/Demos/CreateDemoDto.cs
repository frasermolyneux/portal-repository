using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Demos
{
    public record CreateDemoDto : IDto
    {
        public CreateDemoDto(GameType gameType, Guid userProfileId)
        {
            GameType = gameType;
            UserProfileId = userProfileId;
        }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; private set; }

        [JsonProperty]
        public Guid UserProfileId { get; private set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(GameType), GameType.ToString() },
            { nameof(UserProfileId), UserProfileId.ToString() }
        };
    }
}
