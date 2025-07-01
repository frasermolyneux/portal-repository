using Newtonsoft.Json;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.GameServers
{
    public record GameServerStatDto : IDto
    {
        [JsonProperty]
        public Guid GameServerStatId { get; internal set; }

        [JsonProperty]
        public Guid GameServerId { get; internal set; }

        [JsonProperty]
        public int PlayerCount { get; internal set; }

        [JsonProperty]
        public string MapName { get; internal set; } = string.Empty;

        [JsonProperty]
        public DateTime Timestamp { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(GameServerStatId), GameServerStatId.ToString() },
                    { nameof(GameServerId), GameServerId.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}
