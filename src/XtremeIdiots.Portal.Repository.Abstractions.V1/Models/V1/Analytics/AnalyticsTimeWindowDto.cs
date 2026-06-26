using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;

public record AnalyticsTimeWindowDto : IDto
{
    [JsonProperty]
    public DateTime FromUtc { get; internal set; }

    [JsonProperty]
    public DateTime ToUtc { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
