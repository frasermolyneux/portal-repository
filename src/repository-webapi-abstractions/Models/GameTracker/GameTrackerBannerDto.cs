using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameTracker
{
    public record GameTrackerBannerDto : IDto
    {
        [JsonProperty]
        public string BannerUrl { get; internal set; } = string.Empty;

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
