using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;

public record AnalyticsTopItemDto : IDto
{
    [JsonProperty]
    public string Key { get; internal set; } = string.Empty;

    [JsonProperty]
    public string? Label { get; internal set; }

    [JsonProperty]
    public int Count { get; internal set; }

    [JsonProperty]
    public double? Percentage { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
