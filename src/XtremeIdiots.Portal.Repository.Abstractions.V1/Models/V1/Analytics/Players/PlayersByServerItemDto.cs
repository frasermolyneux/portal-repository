using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

public record PlayersByServerItemDto : IDto
{
    [JsonProperty]
    public Guid GameServerId { get; internal set; }

    [JsonProperty]
    public string Title { get; internal set; } = string.Empty;

    [JsonProperty]
    public GameType GameType { get; internal set; }

    [JsonProperty]
    public int ActivePlayers { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
