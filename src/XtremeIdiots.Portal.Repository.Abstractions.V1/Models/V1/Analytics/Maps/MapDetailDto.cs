using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;

public record MapDetailDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public Guid MapId { get; internal set; }

    [JsonProperty]
    public string MapName { get; internal set; } = string.Empty;

    [JsonProperty]
    public GameType GameType { get; internal set; }

    [JsonProperty]
    public int VotesCount { get; internal set; }

    [JsonProperty]
    public int PlaysCount { get; internal set; }

    [JsonProperty]
    public double AveragePosition { get; internal set; }

    [JsonProperty]
    public double AvgPlayers { get; internal set; }

    [JsonProperty]
    public int PeakPlayers { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
