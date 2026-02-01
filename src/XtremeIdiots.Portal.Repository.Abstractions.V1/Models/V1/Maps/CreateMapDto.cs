using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps
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
        public List<MapFileDto> MapFiles { get; set; } = [];

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(GameType), GameType.ToString() }
        };
    }
}
