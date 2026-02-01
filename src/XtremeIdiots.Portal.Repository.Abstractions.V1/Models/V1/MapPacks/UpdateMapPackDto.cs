using System;
using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapPacks;

public record UpdateMapPackDto : IDto
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
    public Dictionary<string, string> TelemetryProperties => [];
}
