using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;

public record MapGamesDto : IDto
{
    public List<GameType> Games { get; init; } = [];
    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
