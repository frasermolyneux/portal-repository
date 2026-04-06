using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

public record MapRotationServerAssignmentDto : IDto
{
    public MapRotationServerAssignmentDto(Guid mapRotationServerAssignmentId, Guid mapRotationId, Guid gameServerId, DeploymentState deploymentState, ActivationState activationState, int? deployedVersion, int? activatedVersion, string? configFilePath, string? configVariableName, string? lastError, DateTime? lastErrorAt, DateTime createdAt, DateTime updatedAt, DateTime? unassignedAt)
    {
        MapRotationServerAssignmentId = mapRotationServerAssignmentId;
        MapRotationId = mapRotationId;
        GameServerId = gameServerId;
        DeploymentState = deploymentState;
        ActivationState = activationState;
        DeployedVersion = deployedVersion;
        ActivatedVersion = activatedVersion;
        ConfigFilePath = configFilePath;
        ConfigVariableName = configVariableName;
        LastError = lastError;
        LastErrorAt = lastErrorAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        UnassignedAt = unassignedAt;
    }

    [JsonProperty]
    public Guid MapRotationServerAssignmentId { get; set; }

    [JsonProperty]
    public Guid MapRotationId { get; set; }

    [JsonProperty]
    public Guid GameServerId { get; set; }

    [JsonProperty]
    public DeploymentState DeploymentState { get; set; }

    [JsonProperty]
    public ActivationState ActivationState { get; set; }

    [JsonProperty]
    public int? DeployedVersion { get; set; }

    [JsonProperty]
    public int? ActivatedVersion { get; set; }

    [JsonProperty]
    public string? ConfigFilePath { get; set; }

    [JsonProperty]
    public string? ConfigVariableName { get; set; }

    [JsonProperty]
    public string? LastError { get; set; }

    [JsonProperty]
    public DateTime? LastErrorAt { get; set; }

    [JsonProperty]
    public DateTime CreatedAt { get; set; }

    [JsonProperty]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty]
    public DateTime? UnassignedAt { get; set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
