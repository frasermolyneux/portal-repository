using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Maps
{
    public record CreateMapDto : IDto
    {
        public CreateMapDto(GameType gameType, string mapName)
        {
            GameType = gameType;
            MapName = mapName;
        }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; set; }

        [JsonProperty]
        public string MapName { get; set; }

        [JsonProperty]
        public List<MapFileDto> MapFiles { get; set; } = new List<MapFileDto>();

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(GameType), GameType.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}
