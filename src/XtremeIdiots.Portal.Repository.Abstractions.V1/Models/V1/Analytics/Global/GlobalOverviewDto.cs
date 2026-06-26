using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Global;

public record GlobalOverviewDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public int TotalServers { get; internal set; }

    [JsonProperty]
    public int OnlineServers { get; internal set; }

    [JsonProperty]
    public int TotalPlayersOnline { get; internal set; }

    [JsonProperty]
    public int UniquePlayersWindow { get; internal set; }

    [JsonProperty]
    public int TotalEvents { get; internal set; }

    [JsonProperty]
    public int TotalChatMessages { get; internal set; }

    [JsonProperty]
    public int OpenReports { get; internal set; }

    [JsonProperty]
    public int ActiveBans { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
