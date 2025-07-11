using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

[Obsolete("Use CollectionModel<UserProfileDto> from MX.Api.Abstractions instead")]
public record UserProfileCollectionDto
{
    public List<UserProfileDto> Entries { get; set; } = new List<UserProfileDto>();
    public int TotalRecords { get; set; }
}
