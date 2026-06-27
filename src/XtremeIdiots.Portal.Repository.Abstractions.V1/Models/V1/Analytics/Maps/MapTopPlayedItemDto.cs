using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;

public record MapTopPlayedItemDto : IDto
{
    [JsonProperty]
    public string MapName { get; internal set; } = string.Empty;

    [JsonProperty]
    public GameType GameType { get; internal set; }

    [JsonProperty]
    public int PlaysCount { get; internal set; }

    [JsonProperty]
    public double SharePercent { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
