using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;

public record AnalyticsSummaryMetricDto : IDto
{
    [JsonProperty]
    public string Name { get; internal set; } = string.Empty;

    [JsonProperty]
    public double Value { get; internal set; }

    [JsonProperty]
    public string? Unit { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
