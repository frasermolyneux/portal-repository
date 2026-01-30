using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

[Obsolete("Use CollectionModel<UserProfileClaimDto> from MX.Api.Abstractions instead")]
public record UserProfileClaimsCollectionDto
{
    public List<UserProfileClaimDto> Entries { get; set; } = [];
    public int TotalRecords { get; set; }
}
