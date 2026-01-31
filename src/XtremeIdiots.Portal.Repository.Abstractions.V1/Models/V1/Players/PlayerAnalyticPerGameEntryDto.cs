using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
{
    public record PlayerAnalyticPerGameEntryDto : IDto
    {
        [JsonProperty]
        public DateTime Created { get; internal set; }

        [JsonProperty]
        public Dictionary<GameType, int> GameCounts { get; internal set; } = [];

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => [];
    }
}