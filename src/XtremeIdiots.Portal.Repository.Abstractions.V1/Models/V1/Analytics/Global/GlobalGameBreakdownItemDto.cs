using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Global;

public record GlobalGameBreakdownItemDto : IDto
{
    [JsonProperty]
    public GameType GameType { get; internal set; }

    [JsonProperty]
    public int ServerCount { get; internal set; }

    [JsonProperty]
    public double AvgPlayers { get; internal set; }

    [JsonProperty]
    public int EventsCount { get; internal set; }

    [JsonProperty]
    public int UniquePlayers { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
