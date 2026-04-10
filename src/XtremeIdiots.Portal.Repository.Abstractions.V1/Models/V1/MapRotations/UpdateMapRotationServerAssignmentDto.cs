using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

public record UpdateMapRotationServerAssignmentDto : IDto
{
    public UpdateMapRotationServerAssignmentDto(Guid mapRotationServerAssignmentId)
    {
        MapRotationServerAssignmentId = mapRotationServerAssignmentId;
    }

    [JsonProperty]
    public Guid MapRotationServerAssignmentId { get; private set; }

    [JsonProperty]
    public DeploymentState? DeploymentState { get; set; }

    [JsonProperty]
    public ActivationState? ActivationState { get; set; }

    [JsonProperty]
    public int? DeployedVersion { get; set; }

    [JsonProperty]
    public int? ActivatedVersion { get; set; }

    [JsonProperty]
    public string? ConfigFilePath { get; set; }

    [JsonProperty]
    public string? ConfigVariableName { get; set; }

    [JsonProperty]
    public int? PlayerCountMin { get; set; }

    [JsonProperty]
    public int? PlayerCountMax { get; set; }

    [JsonProperty]
    public string? LastError { get; set; }

    [JsonProperty]
    public DateTime? LastErrorAt { get; set; }

    [JsonProperty]
    public DateTime? UnassignedAt { get; set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
