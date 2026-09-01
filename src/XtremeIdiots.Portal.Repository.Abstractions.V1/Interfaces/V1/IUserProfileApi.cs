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

        /// <summary>
        /// Retrieves a paginated list of user profiles with optional filtering, game scoping and sorting.
        /// </summary>
        /// <param name="filterString">Optional filter string to search for user profiles.</param>
        /// <param name="filter">Optional role-based filter to constrain results by claim type.</param>
        /// <param name="gameType">
        /// Optional game type used to constrain game-scoped role filters (<see cref="UserProfileFilter.HeadAdmins"/>,
        /// <see cref="UserProfileFilter.GameAdmins"/>, <see cref="UserProfileFilter.Moderators"/> and
        /// <see cref="UserProfileFilter.AnyAdmin"/>) to the claim matching that specific game. The role type and game
        /// value must be present on the same claim. <see cref="GameType.Unknown"/> is treated as no game filter.
        /// </param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of user profiles.</returns>
        Task<ApiResult<CollectionModel<UserProfileDto>>> GetUserProfiles(string? filterString, UserProfileFilter? filter, GameType? gameType, int skipEntries, int takeEntries, UserProfilesOrder? order, CancellationToken cancellationToken = default);

        Task<ApiResult> CreateUserProfile(CreateUserProfileDto createUserProfileDto, CancellationToken cancellationToken = default);
        Task<ApiResult> CreateUserProfiles(List<CreateUserProfileDto> createUserProfileDtos, CancellationToken cancellationToken = default);

        Task<ApiResult> UpdateUserProfile(EditUserProfileDto editUserProfileDto, CancellationToken cancellationToken = default);
        Task<ApiResult> UpdateUserProfiles(List<EditUserProfileDto> editUserProfileDtos, CancellationToken cancellationToken = default);

        Task<ApiResult> CreateUserProfileClaim(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto, CancellationToken cancellationToken = default);
        Task<ApiResult> SetUserProfileClaims(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto, CancellationToken cancellationToken = default);

        Task<ApiResult> DeleteUserProfileClaim(Guid userProfileId, Guid userProfileClaimId, CancellationToken cancellationToken = default);

        Task<ApiResult<CollectionModel<PermissionReportEntryDto>>> GetPermissionsReport(GameType? gameType, string? claimType, CancellationToken cancellationToken = default);
    }
}
