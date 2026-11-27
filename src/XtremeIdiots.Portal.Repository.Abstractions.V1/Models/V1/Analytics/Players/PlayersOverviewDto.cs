using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

public record PlayersOverviewDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public int TotalPlayers { get; internal set; }

    [JsonProperty]
    public int NewPlayers { get; internal set; }

    [JsonProperty]
    public int ActivePlayers { get; internal set; }

    [JsonProperty]
    public int ReturningPlayers { get; internal set; }

    [JsonProperty]
    public int? ObservedPlayerRecords { get; internal set; }

    [JsonProperty]
    public int? RecordedPlayers { get; internal set; }

    [JsonProperty]
    public int? SessionStarts { get; internal set; }

    [JsonProperty]
    public string? TrackingCoverage { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
