using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;

public record MapsByGameItemDto : IDto
{
    [JsonProperty]
    public GameType GameType { get; internal set; }

    [JsonProperty]
    public int MapsPlayed { get; internal set; }

    [JsonProperty]
    public int TotalPlays { get; internal set; }

    [JsonProperty]
    public int TotalVotes { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
