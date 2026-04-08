using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;

/// <summary>
/// Per-server utilization data including average and peak player counts over a period.
/// </summary>
public record ServerUtilizationDto : IDto
{
    [JsonProperty]
    public Guid ServerId { get; internal set; }

    [JsonProperty]
    public string Title { get; internal set; } = string.Empty;

    [JsonProperty]
    public string GameType { get; internal set; } = string.Empty;

    [JsonProperty]
    public double AvgPlayers { get; internal set; }

    [JsonProperty]
    public int PeakPlayers { get; internal set; }

    [JsonProperty]
    public int MaxPlayers { get; internal set; }

    /// <summary>
    /// Utilization as a ratio 0.0–1.0 (AvgPlayers / MaxPlayers).
    /// </summary>
    [JsonProperty]
    public double Utilization { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}

/// <summary>
/// Aggregate server utilization across all servers.
/// </summary>
public record ServerUtilizationCollectionDto : IDto
{
    [JsonProperty]
    public List<ServerUtilizationDto> Servers { get; internal set; } = [];

    [JsonProperty]
    public double TotalAvgPlayers { get; internal set; }

    [JsonProperty]
    public int TotalPeakPlayers { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
