using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

public record CreateMapRotationServerAssignmentDto : IDto
{
    public CreateMapRotationServerAssignmentDto(Guid mapRotationId, Guid gameServerId)
    {
        MapRotationId = mapRotationId;
        GameServerId = gameServerId;
    }

    [JsonProperty]
    public Guid MapRotationId { get; set; }

    [JsonProperty]
    public Guid GameServerId { get; set; }

    [JsonProperty]
    public string? ConfigFilePath { get; set; }

    [JsonProperty]
    public string? ConfigVariableName { get; set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
