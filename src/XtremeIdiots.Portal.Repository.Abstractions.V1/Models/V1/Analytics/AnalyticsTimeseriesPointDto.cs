using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;

public record AnalyticsTimeseriesPointDto : IDto
{
    [JsonProperty]
    public DateTime BucketStartUtc { get; internal set; }

    [JsonProperty]
    public DateTime BucketEndUtc { get; internal set; }

    [JsonProperty]
    public int Value { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
