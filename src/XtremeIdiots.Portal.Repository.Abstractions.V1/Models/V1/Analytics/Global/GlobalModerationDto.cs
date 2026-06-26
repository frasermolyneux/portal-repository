using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Global;

public record GlobalModerationDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public GlobalModerationSummaryDto Summary { get; internal set; } = new();

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}