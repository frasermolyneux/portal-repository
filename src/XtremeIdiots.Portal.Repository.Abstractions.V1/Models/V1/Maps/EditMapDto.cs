using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps
{
    public record EditMapDto : IDto
    {
        public EditMapDto(Guid mapId)
        {
            MapId = mapId;
        }

        [JsonProperty]
        public Guid MapId { get; private set; }

        [JsonProperty]
        public List<MapFileDto> MapFiles { get; set; } = [];

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(MapId), MapId.ToString() }
        };
    }
}
