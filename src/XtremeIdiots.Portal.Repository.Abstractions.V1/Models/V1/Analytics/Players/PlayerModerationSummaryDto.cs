using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

public record PlayerModerationSummaryDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public int WarningsCount { get; internal set; }

    [JsonProperty]
    public int MutesCount { get; internal set; }

    [JsonProperty]
    public int KicksCount { get; internal set; }

    [JsonProperty]
    public int BansCount { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}