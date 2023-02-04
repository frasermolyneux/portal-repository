using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameTracker
{
    public class GameTrackerBannerDto
    {
        [JsonProperty]
        public string BannerUrl { get; internal set; } = string.Empty;
    }
}
