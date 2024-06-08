using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players
{
    public class CreatePlayerDto : IDto
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
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(GameType), GameType.ToString() },
                    { nameof(Username), Username.ToString() },
                    { nameof(Guid), Guid.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}
