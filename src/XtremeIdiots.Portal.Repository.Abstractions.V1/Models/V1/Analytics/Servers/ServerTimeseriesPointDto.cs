using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Servers;

public record ServerTimeseriesPointDto : IDto
{
    [JsonProperty]
    public DateTime Timestamp { get; internal set; }

    [JsonProperty]
    public int UniquePlayers { get; internal set; }

    [JsonProperty]
    public int EventsCount { get; internal set; }

    [JsonProperty]
    public int ChatMessagesCount { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}