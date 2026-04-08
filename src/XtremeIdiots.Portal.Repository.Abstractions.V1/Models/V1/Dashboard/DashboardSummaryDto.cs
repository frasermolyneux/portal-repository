using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;

/// <summary>
/// Aggregated summary data for the admin dashboard, returned as a single payload.
/// </summary>
public record DashboardSummaryDto : IDto
{
    [JsonProperty]
    public int TotalServers { get; internal set; }

    [JsonProperty]
    public int OnlineServerCount { get; internal set; }

    [JsonProperty]
    public int OfflineServerCount { get; internal set; }

    [JsonProperty]
    public int TotalPlayersOnline { get; internal set; }

    [JsonProperty]
    public int UnclaimedBanCount { get; internal set; }

    [JsonProperty]
    public int OpenReportCount { get; internal set; }

    [JsonProperty]
    public AdminActionCountsDto RecentActions24h { get; internal set; } = new();

    [JsonProperty]
    public AdminActionCountsDto RecentActions7d { get; internal set; } = new();

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}

/// <summary>
/// Breakdown of admin action counts by type.
/// </summary>
public record AdminActionCountsDto
{
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
}
