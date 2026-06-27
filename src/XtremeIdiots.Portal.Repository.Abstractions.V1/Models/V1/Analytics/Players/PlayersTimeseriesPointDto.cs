using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

public record PlayersTimeseriesPointDto : IDto
{
    [JsonProperty]
    public DateTime BucketStartUtc { get; internal set; }

    [JsonProperty]
    public int NewPlayers { get; internal set; }

    [JsonProperty]
    public int ActivePlayers { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
