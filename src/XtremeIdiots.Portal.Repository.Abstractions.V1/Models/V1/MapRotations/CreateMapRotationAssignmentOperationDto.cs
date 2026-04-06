using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

public record CreateMapRotationAssignmentOperationDto : IDto
{
    public CreateMapRotationAssignmentOperationDto(Guid mapRotationServerAssignmentId, AssignmentOperationType operationType)
    {
        MapRotationServerAssignmentId = mapRotationServerAssignmentId;
        OperationType = operationType;
    }

    [JsonProperty]
    public Guid MapRotationServerAssignmentId { get; set; }

    [JsonProperty]
    public AssignmentOperationType OperationType { get; set; }

    [JsonProperty]
    public string? DurableFunctionInstanceId { get; set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
