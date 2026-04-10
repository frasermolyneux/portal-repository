using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;

public record MapFlagDto : IDto
{
    [JsonProperty]
    public Guid MapFlagId { get; set; }

    [JsonProperty]
    public Guid MapId { get; set; }

    [JsonProperty]
    public MapFlagType FlagType { get; set; }

    [JsonProperty]
    public string? Reason { get; set; }

    [JsonProperty]
    public string? ReportedBy { get; set; }

    [JsonProperty]
    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => new()
    {
        { nameof(MapId), MapId.ToString() }
    };
}

public record CreateMapFlagDto : IDto
{
    public CreateMapFlagDto(Guid mapId, MapFlagType flagType)
    {
        MapId = mapId;
        FlagType = flagType;
    }

    [JsonProperty]
    public Guid MapId { get; set; }

    [JsonProperty]
    public MapFlagType FlagType { get; set; }

    [JsonProperty]
    public string? Reason { get; set; }

    [JsonProperty]
    public string? ReportedBy { get; set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
