using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Servers;

public record ServerSummaryDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public int RecentPlayersCount { get; internal set; }

    [JsonProperty]
    public int AdminActionsCount { get; internal set; }

    [JsonProperty]
    public int ReportsCount { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}