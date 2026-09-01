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
        {
            return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(up)));
        }

        return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.NotFound, new ApiResponse<UserProfileDto>(new ApiError("NOT_FOUND", "User profile not found"))));
    }

    public Task<ApiResult<UserProfileDto>> GetUserProfileByIdentityId(string identityId, CancellationToken cancellationToken = default)
    {
        var up = _userProfiles.Values.FirstOrDefault(u => u.IdentityOid == identityId);
        if (up != null)
        {
            return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(up)));
        }

        return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.NotFound, new ApiResponse<UserProfileDto>(new ApiError("NOT_FOUND", "User profile not found"))));
    }

    public Task<ApiResult<UserProfileDto>> GetUserProfileByXtremeIdiotsId(string xtremeIdiotsId, CancellationToken cancellationToken = default)
    {
        var up = _userProfiles.Values.FirstOrDefault(u => u.XtremeIdiotsForumId == xtremeIdiotsId);
        if (up != null)
        {
            return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(up)));
        }

        return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.NotFound, new ApiResponse<UserProfileDto>(new ApiError("NOT_FOUND", "User profile not found"))));
    }

    public Task<ApiResult<UserProfileDto>> GetUserProfileByDemoAuthKey(string demoAuthKey, CancellationToken cancellationToken = default)
    {
        var up = _userProfiles.Values.FirstOrDefault(u => u.DemoAuthKey == demoAuthKey);
        if (up != null)
        {
            return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(up)));
        }

        return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.NotFound, new ApiResponse<UserProfileDto>(new ApiError("NOT_FOUND", "User profile not found"))));
    }

    public Task<ApiResult<CollectionModel<UserProfileDto>>> GetUserProfiles(string? filterString, UserProfileFilter? filter, int skipEntries, int takeEntries, UserProfilesOrder? order, CancellationToken cancellationToken = default)
        => GetUserProfiles(filterString, filter, null, skipEntries, takeEntries, order, cancellationToken);

    public Task<ApiResult<CollectionModel<UserProfileDto>>> GetUserProfiles(string? filterString, UserProfileFilter? filter, GameType? gameType, int skipEntries, int takeEntries, UserProfilesOrder? order, CancellationToken cancellationToken = default)
    {
        IEnumerable<UserProfileDto> query = _userProfiles.Values;

        var totalCount = query.Count();

        if (!string.IsNullOrWhiteSpace(filterString))
        {
            var textFilter = filterString.Trim().ToLowerInvariant();
            query = query.Where(up => (up.IdentityOid != null && up.IdentityOid.ToLowerInvariant().Contains(textFilter)) ||
                                       (up.XtremeIdiotsForumId != null && up.XtremeIdiotsForumId.ToLowerInvariant().Contains(textFilter)) ||
                                       (up.DemoAuthKey != null && up.DemoAuthKey.ToLowerInvariant().Contains(textFilter)) ||
                                       (up.DisplayName != null && up.DisplayName.ToLowerInvariant().Contains(textFilter)) ||
                                       (up.Email != null && up.Email.ToLowerInvariant().Contains(textFilter)));
        }

        // GameType.Unknown is treated the same as no game filter being supplied.
        var gameTypeFilter = gameType.HasValue && gameType.Value != GameType.Unknown ? gameType : null;

        if (filter.HasValue)
        {
            query = filter.Value switch
            {
                UserProfileFilter.Webmasters => query.Where(up => up.UserProfileClaims.Any(c => c.ClaimType == UserProfileClaimType.Webmaster)),
                UserProfileFilter.SeniorAdmins => query.Where(up => up.UserProfileClaims.Any(c => c.ClaimType == UserProfileClaimType.SeniorAdmin)),
                UserProfileFilter.HeadAdmins => ApplyGameScopedRoleFilter(query, UserProfileClaimType.HeadAdmin, gameTypeFilter),
                UserProfileFilter.GameAdmins => ApplyGameScopedRoleFilter(query, UserProfileClaimType.GameAdmin, gameTypeFilter),
                UserProfileFilter.Moderators => ApplyGameScopedRoleFilter(query, UserProfileClaimType.Moderator, gameTypeFilter),
                UserProfileFilter.AnyAdmin => ApplyAnyAdminFilter(query, gameTypeFilter),
                UserProfileFilter.HasAdditionalPermissions => query.Where(up => up.UserProfileClaims.Any(c => !c.SystemGenerated)),
                _ => query
            };
        }

        var filtered = query.ToList();
        var filteredCount = filtered.Count;

        var ordered = order switch
        {
            UserProfilesOrder.DisplayNameDesc => filtered.OrderByDescending(up => up.DisplayName),
            _ => filtered.OrderBy(up => up.DisplayName)
        };

        var items = ordered.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<UserProfileDto> { Items = items };

        return Task.FromResult(new ApiResult<CollectionModel<UserProfileDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<UserProfileDto>>(collection)
        {
            Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
        }));
    }

    private static IEnumerable<UserProfileDto> ApplyGameScopedRoleFilter(IEnumerable<UserProfileDto> query, string claimType, GameType? gameType)
    {
        if (gameType.HasValue)
        {
            var gameTypeString = gameType.Value.ToString();
            return query.Where(up => up.UserProfileClaims.Any(c => c.ClaimType == claimType && c.ClaimValue == gameTypeString));
        }

        return query.Where(up => up.UserProfileClaims.Any(c => c.ClaimType == claimType));
    }

    private static IEnumerable<UserProfileDto> ApplyAnyAdminFilter(IEnumerable<UserProfileDto> query, GameType? gameType)
    {
        if (gameType.HasValue)
        {
            var gameTypeString = gameType.Value.ToString();
            return query.Where(up => up.UserProfileClaims.Any(c =>
                c.ClaimType == UserProfileClaimType.Webmaster ||
                c.ClaimType == UserProfileClaimType.SeniorAdmin ||
                ((c.ClaimType == UserProfileClaimType.HeadAdmin || c.ClaimType == UserProfileClaimType.GameAdmin || c.ClaimType == UserProfileClaimType.Moderator) && c.ClaimValue == gameTypeString)));
        }

        return query.Where(up => up.UserProfileClaims.Any(c => c.ClaimType == UserProfileClaimType.Webmaster || c.ClaimType == UserProfileClaimType.SeniorAdmin || c.ClaimType == UserProfileClaimType.HeadAdmin || c.ClaimType == UserProfileClaimType.GameAdmin || c.ClaimType == UserProfileClaimType.Moderator));
    }

    public Task<ApiResult> CreateUserProfile(CreateUserProfileDto createUserProfileDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> CreateUserProfiles(List<CreateUserProfileDto> createUserProfileDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdateUserProfile(EditUserProfileDto editUserProfileDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdateUserProfiles(List<EditUserProfileDto> editUserProfileDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> CreateUserProfileClaim(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> SetUserProfileClaims(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteUserProfileClaim(Guid userProfileId, Guid userProfileClaimId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult<CollectionModel<PermissionReportEntryDto>>> GetPermissionsReport(GameType? gameType, string? claimType, CancellationToken cancellationToken = default)
    {
        var collection = new CollectionModel<PermissionReportEntryDto>(new List<PermissionReportEntryDto>());
        return Task.FromResult(new ApiResult<CollectionModel<PermissionReportEntryDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PermissionReportEntryDto>>(collection)));
    }
}
