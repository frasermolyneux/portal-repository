using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;

/// <summary>
/// Describes whether an automated action was created or an existing action was reused.
/// </summary>
public sealed record EnsureAutomatedActionResultDto : IDto
{
    [JsonProperty]
    public bool Created { get; internal set; }

    [JsonProperty]
    public AdminActionDto AdminAction { get; internal set; } = null!;

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => new()
    {
        { nameof(Created), Created.ToString() },
        { nameof(AdminAction.AdminActionId), AdminAction?.AdminActionId.ToString() ?? string.Empty }
    };
}