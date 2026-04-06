using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

public record MapRotationAssignmentOperationDto : IDto
{
    public MapRotationAssignmentOperationDto(Guid mapRotationAssignmentOperationId, Guid mapRotationServerAssignmentId, AssignmentOperationType operationType, AssignmentOperationStatus status, string? durableFunctionInstanceId, DateTime startedAt, DateTime? completedAt, string? error)
    {
        MapRotationAssignmentOperationId = mapRotationAssignmentOperationId;
        MapRotationServerAssignmentId = mapRotationServerAssignmentId;
        OperationType = operationType;
        Status = status;
        DurableFunctionInstanceId = durableFunctionInstanceId;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Error = error;
    }

    [JsonProperty]
    public Guid MapRotationAssignmentOperationId { get; set; }

    [JsonProperty]
    public Guid MapRotationServerAssignmentId { get; set; }

    [JsonProperty]
    public AssignmentOperationType OperationType { get; set; }

    [JsonProperty]
    public AssignmentOperationStatus Status { get; set; }

    [JsonProperty]
    public string? DurableFunctionInstanceId { get; set; }

    [JsonProperty]
    public DateTime StartedAt { get; set; }

    [JsonProperty]
    public DateTime? CompletedAt { get; set; }

    [JsonProperty]
    public string? Error { get; set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
