using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameTracker
{
    public record GameTrackerBannerDto : IDto
    {
        [JsonProperty]
        public string BannerUrl { get; internal set; } = string.Empty;

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => [];
    }
}
