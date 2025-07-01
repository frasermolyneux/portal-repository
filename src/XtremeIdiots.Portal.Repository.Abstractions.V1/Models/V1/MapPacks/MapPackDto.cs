using System;
using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapPacks;

public record MapPackDto : IDto
{
    public MapPackDto(Guid mapPackId, Guid gameServerId, string title, string description, string gameMode, bool syncToGameServer, bool syncCompleted, bool deleted, List<MapPackMapDto> mapPackMaps)
    {
        MapPackId = mapPackId;
        GameServerId = gameServerId;
        Title = title;
        Description = description;
        GameMode = gameMode;
        SyncToGameServer = syncToGameServer;
        SyncCompleted = syncCompleted;
        Deleted = deleted;
        MapPackMaps = mapPackMaps;
    }

    [JsonProperty]
    public Guid MapPackId { get; set; }

    [JsonProperty]
    public Guid GameServerId { get; set; }

    [JsonProperty]
    public string Title { get; set; }

    [JsonProperty]
    public string Description { get; set; }

    [JsonProperty]
    public string GameMode { get; set; }

    [JsonProperty]
    public bool SyncToGameServer { get; set; }

    [JsonProperty]
    public bool SyncCompleted { get; set; }

    [JsonProperty]
    public bool Deleted { get; set; }

    [JsonProperty]
    public List<MapPackMapDto> MapPackMaps { get; set; }

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
