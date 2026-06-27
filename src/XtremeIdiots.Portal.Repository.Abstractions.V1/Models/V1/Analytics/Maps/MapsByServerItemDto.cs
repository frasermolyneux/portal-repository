using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;

public record MapsByServerItemDto : IDto
{
    [JsonProperty]
    public string MapName { get; internal set; } = string.Empty;

    [JsonProperty]
    public int PlaysCount { get; internal set; }

    [JsonProperty]
    public double AvgPlayers { get; internal set; }

    [JsonProperty]
    public double SharePercent { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
