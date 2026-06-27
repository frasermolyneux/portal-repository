using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;

public record MapsOverviewDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public int TotalMaps { get; internal set; }

    [JsonProperty]
    public int TotalPlays { get; internal set; }

    [JsonProperty]
    public int TotalVotes { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
