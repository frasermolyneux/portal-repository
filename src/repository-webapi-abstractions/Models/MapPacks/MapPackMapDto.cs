using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.MapPacks;

public record MapPackMapDto : IDto
{
    [JsonProperty]
    public Guid MapPackMapId { get; set; }

    [JsonProperty]
    public Guid MapId { get; set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties
    {
        get
        {
            var telemetryProperties = new Dictionary<string, string>();
            return telemetryProperties;
        }
    }
}
