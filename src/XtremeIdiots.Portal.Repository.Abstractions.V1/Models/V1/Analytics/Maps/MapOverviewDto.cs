using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;

public record MapOverviewDto : IDto
{
    [JsonProperty]
    public Guid MapId { get; internal set; }

    [JsonProperty]
    public string MapName { get; internal set; } = string.Empty;

    [JsonProperty]
    public int VotesCount { get; internal set; }

    [JsonProperty]
    public int PlaysCount { get; internal set; }

    [JsonProperty]
    public double AveragePosition { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}