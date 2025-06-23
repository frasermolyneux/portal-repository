using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players
{
    public record PlayerAliasDto : IDto
    {
        [JsonProperty]
        public Guid PlayerAliasId { get; internal set; }

        [JsonProperty]
        public Guid PlayerId { get; internal set; }

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
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(PlayerAliasId), PlayerAliasId.ToString() },
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(Name), Name }
                };

                return telemetryProperties;
            }
        }
    }
}
