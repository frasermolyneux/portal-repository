using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

public record PlayerDetailDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public Guid PlayerId { get; internal set; }

    [JsonProperty]
    public string Username { get; internal set; } = string.Empty;

    [JsonProperty]
    public GameType GameType { get; internal set; }

    [JsonProperty]
    public DateTime FirstSeenUtc { get; internal set; }

    [JsonProperty]
    public DateTime LastSeenUtc { get; internal set; }

    [JsonProperty]
    public int SessionsCount { get; internal set; }

    [JsonProperty]
    public int TotalPlayTimeMinutes { get; internal set; }

    [JsonProperty]
    public PlayerModerationSummaryDto Moderation { get; internal set; } = new();

    [JsonProperty]
    public PlayerRelatedActivityDto Related { get; internal set; } = new();

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
