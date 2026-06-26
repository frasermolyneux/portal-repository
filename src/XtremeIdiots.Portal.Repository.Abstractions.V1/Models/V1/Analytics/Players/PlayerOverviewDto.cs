using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

public record PlayerOverviewDto : IDto
{
    [JsonProperty]
    public Guid PlayerId { get; internal set; }

    [JsonProperty]
    public string Username { get; internal set; } = string.Empty;

    [JsonProperty]
    public int SessionsCount { get; internal set; }

    [JsonProperty]
    public int TotalPlayTimeMinutes { get; internal set; }

    [JsonProperty]
    public int AdminActionsCount { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}