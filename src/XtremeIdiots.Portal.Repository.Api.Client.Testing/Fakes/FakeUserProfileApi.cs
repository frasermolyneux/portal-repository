using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeUserProfileApi : IUserProfileApi
{
    private readonly ConcurrentDictionary<Guid, UserProfileDto> _userProfiles = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);

    public FakeUserProfileApi AddUserProfile(UserProfileDto userProfile) { _userProfiles[userProfile.UserProfileId] = userProfile; return this; }
    public FakeUserProfileApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeUserProfileApi Reset() { _userProfiles.Clear(); _errorResponses.Clear(); return this; }

    public Task<ApiResult<UserProfileDto>> GetUserProfile(Guid userProfileId, CancellationToken cancellationToken = default)
    {
        if (_userProfiles.TryGetValue(userProfileId, out var up))
            return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(up)));
        return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.NotFound, new ApiResponse<UserProfileDto>(new ApiError("NOT_FOUND", "User profile not found"))));
    }

    public Task<ApiResult<UserProfileDto>> GetUserProfileByIdentityId(string identityId, CancellationToken cancellationToken = default)
    {
        var up = _userProfiles.Values.FirstOrDefault(u => u.IdentityOid == identityId);
        if (up != null)
            return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(up)));
        return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.NotFound, new ApiResponse<UserProfileDto>(new ApiError("NOT_FOUND", "User profile not found"))));
    }

    public Task<ApiResult<UserProfileDto>> GetUserProfileByXtremeIdiotsId(string xtremeIdiotsId, CancellationToken cancellationToken = default)
    {
        var up = _userProfiles.Values.FirstOrDefault(u => u.XtremeIdiotsForumId == xtremeIdiotsId);
        if (up != null)
            return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(up)));
        return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.NotFound, new ApiResponse<UserProfileDto>(new ApiError("NOT_FOUND", "User profile not found"))));
    }

    public Task<ApiResult<UserProfileDto>> GetUserProfileByDemoAuthKey(string demoAuthKey, CancellationToken cancellationToken = default)
    {
        var up = _userProfiles.Values.FirstOrDefault(u => u.DemoAuthKey == demoAuthKey);
        if (up != null)
            return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(up)));
        return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.NotFound, new ApiResponse<UserProfileDto>(new ApiError("NOT_FOUND", "User profile not found"))));
    }

    public Task<ApiResult<CollectionModel<UserProfileDto>>> GetUserProfiles(string? filterString, UserProfileFilter? filter, int skipEntries, int takeEntries, UserProfilesOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _userProfiles.Values.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<UserProfileDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<UserProfileDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<UserProfileDto>>(collection)));
    }

    public Task<ApiResult> CreateUserProfile(CreateUserProfileDto createUserProfileDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> CreateUserProfiles(List<CreateUserProfileDto> createUserProfileDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdateUserProfile(EditUserProfileDto editUserProfileDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdateUserProfiles(List<EditUserProfileDto> editUserProfileDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> CreateUserProfileClaim(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> SetUserProfileClaims(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteUserProfileClaim(Guid userProfileId, Guid userProfileClaimId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
