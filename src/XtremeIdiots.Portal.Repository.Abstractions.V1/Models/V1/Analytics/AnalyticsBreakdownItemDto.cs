using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;

public record AnalyticsBreakdownItemDto : IDto
{
    [JsonProperty]
    public string Key { get; internal set; } = string.Empty;

    [JsonProperty]
    public string Label { get; internal set; } = string.Empty;

    [JsonProperty]
    public int Count { get; internal set; }

    [JsonProperty]
    public double? Score { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
