using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
{
    public record PlayerAnalyticEntryDto : IDto
    {
        [JsonProperty]
        public DateTime Created { get; internal set; }

        [JsonProperty]
        public int Count { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => [];
    }
}