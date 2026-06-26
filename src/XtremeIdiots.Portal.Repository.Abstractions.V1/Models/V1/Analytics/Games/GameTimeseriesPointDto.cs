using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Games;

public record GameTimeseriesPointDto : IDto
{
    [JsonProperty]
    public DateTime BucketStartUtc { get; internal set; }

    [JsonProperty]
    public double AvgPlayers { get; internal set; }

    [JsonProperty]
    public int EventsCount { get; internal set; }

    [JsonProperty]
    public int ChatCount { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
