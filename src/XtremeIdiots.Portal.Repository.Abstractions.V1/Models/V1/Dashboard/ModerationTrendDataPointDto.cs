using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;

/// <summary>
/// Daily moderation action counts for trend visualisation (sparklines/charts).
/// </summary>
public record ModerationTrendDataPointDto : IDto
{
    [JsonProperty]
    public DateTime Date { get; internal set; }

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

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
