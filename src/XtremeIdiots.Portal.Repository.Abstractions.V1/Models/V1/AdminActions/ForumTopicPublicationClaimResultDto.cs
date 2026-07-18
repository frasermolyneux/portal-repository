using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;

/// <summary>
/// Describes whether a caller may post a forum topic for an admin action.
/// </summary>
public sealed record ForumTopicPublicationClaimResultDto : IDto
{
    [JsonProperty]
    public Guid AdminActionId { get; internal set; }

    [JsonProperty]
    public int? ForumTopicId { get; internal set; }

    [JsonProperty]
    public Guid? ClaimId { get; internal set; }

    [JsonProperty]
    public bool RequiresManualRecovery { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => new()
    {
        { nameof(AdminActionId), AdminActionId.ToString() },
        { nameof(ForumTopicId), ForumTopicId?.ToString() ?? string.Empty },
        { nameof(ClaimId), ClaimId?.ToString() ?? string.Empty },
        { nameof(RequiresManualRecovery), RequiresManualRecovery.ToString() }
    };
}