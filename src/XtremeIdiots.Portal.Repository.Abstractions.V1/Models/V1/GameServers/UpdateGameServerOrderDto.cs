using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

public record UpdateGameServerOrderDto : IDto
{
    [JsonProperty]
    public List<Guid> GameServerIds { get; set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => new()
    {
        { "ServerCount", GameServerIds.Count.ToString() }
    };
}
