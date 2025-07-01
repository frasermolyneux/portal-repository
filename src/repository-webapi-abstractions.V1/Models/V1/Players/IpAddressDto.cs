using Newtonsoft.Json;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Players
{
    public record IpAddressDto : IDto
    {
        [JsonProperty]
        public string Address { get; internal set; } = string.Empty;

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