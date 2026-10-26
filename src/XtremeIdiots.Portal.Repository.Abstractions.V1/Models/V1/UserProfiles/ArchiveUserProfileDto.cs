using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

public record ArchiveUserProfileDto : IDto
{
    public ArchiveUserProfileDto(Guid userProfileId, Guid archivedByUserProfileId, string reason)
    {
        UserProfileId = userProfileId;
        ArchivedByUserProfileId = archivedByUserProfileId;
        Reason = reason;
    }

    [JsonProperty]
    public Guid UserProfileId { get; private set; }

    [JsonProperty]
    public Guid ArchivedByUserProfileId { get; private set; }

    [JsonProperty]
    [Required]
    [StringLength(500)]
    public string Reason { get; private set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => new()
    {
        { nameof(UserProfileId), UserProfileId.ToString() },
        { nameof(ArchivedByUserProfileId), ArchivedByUserProfileId.ToString() }
    };
}
