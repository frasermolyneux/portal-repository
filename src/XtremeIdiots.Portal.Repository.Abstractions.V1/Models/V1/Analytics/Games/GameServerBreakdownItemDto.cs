using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Games;

public record GameServerBreakdownItemDto : IDto
{
    [JsonProperty]
    public Guid GameServerId { get; internal set; }

    [JsonProperty]
    public string Title { get; internal set; } = string.Empty;

    [JsonProperty]
    public double AvgPlayers { get; internal set; }

    [JsonProperty]
    public int PeakPlayers { get; internal set; }

    [JsonProperty]
    public int EventsCount { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
