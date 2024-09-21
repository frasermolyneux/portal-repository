using System;
using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.MapPacks;

public class UpdateMapPackDto : IDto
{
    public UpdateMapPackDto(Guid mapPackId)
    {
        MapPackId = mapPackId;
    }

    [JsonProperty]
    public Guid MapPackId { get; private set; }

    [JsonProperty]
    public Guid? GameServerId { get; set; }

    [JsonProperty]
    public string? Title { get; set; }

    [JsonProperty]
    public string? Description { get; set; }

    [JsonProperty]
    public string? GameMode { get; set; }

    [JsonProperty]
    public bool? SyncToGameServer { get; set; }

    [JsonProperty]
    public bool? SyncCompleted { get; set; }

    [JsonProperty]
    public bool? Deleted { get; set; }

    [JsonProperty]
    public List<Guid>? MapIds { get; set; }

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
