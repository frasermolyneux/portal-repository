using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;

public record DashboardSummaryDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public int ActiveGamesCount { get; internal set; }

    [JsonProperty]
    public int ActiveServersCount { get; internal set; }

    [JsonProperty]
    public int UniquePlayersCount { get; internal set; }

    [JsonProperty]
    public int ReportsCount { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}