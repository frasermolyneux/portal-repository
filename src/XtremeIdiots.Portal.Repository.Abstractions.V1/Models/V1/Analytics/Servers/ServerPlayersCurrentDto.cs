using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Servers;

public record ServerPlayersCurrentDto : IDto
{
    [JsonProperty]
    public Guid GameServerId { get; internal set; }

    [JsonProperty]
    public bool Online { get; internal set; }

    [JsonProperty]
    public int CurrentPlayers { get; internal set; }

    [JsonProperty]
    public int MaxPlayers { get; internal set; }

    [JsonProperty]
    public string? MapName { get; internal set; }

    [JsonProperty]
    public GameType GameType { get; internal set; }

    [JsonProperty]
    public DateTime? LastUpdatedUtc { get; internal set; }

    [JsonProperty]
    public List<LivePlayerDto> Players { get; internal set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
