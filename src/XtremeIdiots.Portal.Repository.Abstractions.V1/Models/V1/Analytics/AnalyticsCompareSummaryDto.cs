using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;

public record AnalyticsCompareSummaryDto : IDto
{
    [JsonProperty]
    public double CurrentTotal { get; internal set; }

    [JsonProperty]
    public double BaselineTotal { get; internal set; }

    [JsonProperty]
    public double Delta { get; internal set; }

    [JsonProperty]
    public double DeltaPercent { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
