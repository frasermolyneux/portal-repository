using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
{
    public record AliasDto : IDto
    {
        [JsonProperty]
        public string Name { get; internal set; } = string.Empty;

        [JsonProperty]
        public DateTime Added { get; internal set; }

        [JsonProperty]
        public DateTime LastUsed { get; internal set; }

        [JsonProperty]
        public int ConfidenceScore { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>();
                return telemetryProperties;
            }
        }
    }
}