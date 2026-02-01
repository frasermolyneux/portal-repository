using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
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
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(PlayerAliasId), PlayerAliasId.ToString() },
            { nameof(PlayerId), PlayerId.ToString() },
            { nameof(Name), Name }
        };
    }
}
