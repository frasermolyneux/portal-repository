using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IUserProfileApi
    {
        Task<ApiResult<UserProfileDto>> GetUserProfile(Guid userProfileId, CancellationToken cancellationToken = default);
        Task<ApiResult<UserProfileDto>> GetUserProfileByIdentityId(string identityId, CancellationToken cancellationToken = default);
        Task<ApiResult<UserProfileDto>> GetUserProfileByXtremeIdiotsId(string xtremeIdiotsId, CancellationToken cancellationToken = default);
        Task<ApiResult<UserProfileDto>> GetUserProfileByDemoAuthKey(string demoAuthKey, CancellationToken cancellationToken = default);
        Task<ApiResult<CollectionModel<UserProfileDto>>> GetUserProfiles(string? filterString, UserProfileFilter? filter, int skipEntries, int takeEntries, UserProfilesOrder? order, CancellationToken cancellationToken = default);

        Task<ApiResult> CreateUserProfile(CreateUserProfileDto createUserProfileDto, CancellationToken cancellationToken = default);
        Task<ApiResult> CreateUserProfiles(List<CreateUserProfileDto> createUserProfileDtos, CancellationToken cancellationToken = default);

        Task<ApiResult> UpdateUserProfile(EditUserProfileDto editUserProfileDto, CancellationToken cancellationToken = default);
        Task<ApiResult> UpdateUserProfiles(List<EditUserProfileDto> editUserProfileDtos, CancellationToken cancellationToken = default);

        Task<ApiResult> CreateUserProfileClaim(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto, CancellationToken cancellationToken = default);
        Task<ApiResult> SetUserProfileClaims(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto, CancellationToken cancellationToken = default);

        Task<ApiResult> DeleteUserProfileClaim(Guid userProfileId, Guid userProfileClaimId, CancellationToken cancellationToken = default);
    }
}