using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Servers;

public record ServerChatCommandCountDto : IDto
{
    [JsonProperty]
    public string Command { get; internal set; } = string.Empty;

    [JsonProperty]
    public int Count { get; internal set; }

    [JsonProperty]
    public int DeniedCount { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
