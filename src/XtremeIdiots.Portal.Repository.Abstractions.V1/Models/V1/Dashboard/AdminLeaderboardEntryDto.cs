using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;

/// <summary>
/// Admin activity summary for leaderboard display, showing moderation action counts per admin.
/// </summary>
public record AdminLeaderboardEntryDto : IDto
{
    [JsonProperty]
    public Guid AdminId { get; internal set; }

    [JsonProperty]
    public string DisplayName { get; internal set; } = string.Empty;

    [JsonProperty]
    public int Bans { get; internal set; }

    [JsonProperty]
    public int TempBans { get; internal set; }

    [JsonProperty]
    public int Kicks { get; internal set; }

    [JsonProperty]
    public int Warnings { get; internal set; }

    [JsonProperty]
    public int Observations { get; internal set; }

    [JsonProperty]
    public int Total { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
