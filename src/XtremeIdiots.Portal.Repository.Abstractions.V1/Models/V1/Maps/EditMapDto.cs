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
        public List<MapFileDto> MapFiles { get; set; } = new List<MapFileDto>();

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(MapId), MapId.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}
