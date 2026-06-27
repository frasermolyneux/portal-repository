using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Servers;

public record ServerChatterDto : IDto
{
    [JsonProperty]
    public Guid? PlayerId { get; internal set; }

    [JsonProperty]
    public string Username { get; internal set; } = string.Empty;

    [JsonProperty]
    public int Count { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
