using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

public record PlayersByGameItemDto : IDto
{
    [JsonProperty]
    public GameType GameType { get; internal set; }

    [JsonProperty]
    public int TotalPlayers { get; internal set; }

    [JsonProperty]
    public int NewPlayers { get; internal set; }

    [JsonProperty]
    public int ActivePlayers { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
