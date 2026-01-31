using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
{
    public record CreatePlayerDto : IDto
    {
        public CreatePlayerDto(string username, string guid, GameType gameType)
        {
            Username = username;
            Guid = guid;
            GameType = gameType;
        }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; private set; }

        [JsonProperty]
        public string Username { get; private set; }

        [JsonProperty]
        public string Guid { get; private set; }

        [JsonProperty]
        public string? IpAddress { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(GameType), GameType.ToString() },
            { nameof(Username), Username.ToString() },
            { nameof(Guid), Guid.ToString() }
        };
    }
}
