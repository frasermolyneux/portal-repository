using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;

public record AnalyticsSeriesValueDto : IDto
{
    [JsonProperty]
    public DateTime BucketStartUtc { get; internal set; }

    [JsonProperty]
    public double Value { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
