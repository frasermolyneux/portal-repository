using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IUserProfileApi
    {
        Task<ApiResponseDto<UserProfileDto>> GetUserProfile(Guid userProfileId);
        Task<ApiResponseDto<UserProfileDto>> GetUserProfileByIdentityId(string identityId);
        Task<ApiResponseDto<UserProfileDto>> GetUserProfileByXtremeIdiotsId(string xtremeIdiotsId);
        Task<ApiResponseDto<UserProfileDto>> GetUserProfileByDemoAuthKey(string demoAuthKey);
        Task<ApiResponseDto<UserProfileCollectionDto>> GetUserProfiles(string? filterString, int skipEntries, int takeEntries, UserProfilesOrder? order);

        Task<ApiResponseDto> CreateUserProfile(CreateUserProfileDto createUserProfileDto);
        Task<ApiResponseDto> CreateUserProfiles(List<CreateUserProfileDto> createUserProfileDtos);

        Task<ApiResponseDto> UpdateUserProfile(EditUserProfileDto editUserProfileDto);
        Task<ApiResponseDto> UpdateUserProfiles(List<EditUserProfileDto> editUserProfileDtos);

        Task<ApiResponseDto> CreateUserProfileClaim(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto);
        Task<ApiResponseDto> SetUserProfileClaims(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto);

        Task<ApiResponseDto> DeleteUserProfileClaim(Guid userProfileId, Guid userProfileClaimId);
    }
}