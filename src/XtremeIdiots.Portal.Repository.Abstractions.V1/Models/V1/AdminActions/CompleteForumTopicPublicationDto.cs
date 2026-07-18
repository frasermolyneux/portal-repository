using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;

/// <summary>
/// Completes a previously claimed forum-topic publication for an admin action.
/// </summary>
public sealed record CompleteForumTopicPublicationDto : IDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteForumTopicPublicationDto"/> class.
    /// </summary>
    public CompleteForumTopicPublicationDto(Guid claimId, int forumTopicId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(forumTopicId, 0);

        ClaimId = claimId;
        ForumTopicId = forumTopicId;
    }

    [JsonProperty]
    public Guid ClaimId { get; }

    [JsonProperty]
    public int ForumTopicId { get; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => new()
    {
        { nameof(ClaimId), ClaimId.ToString() },
        { nameof(ForumTopicId), ForumTopicId.ToString() }
    };
}