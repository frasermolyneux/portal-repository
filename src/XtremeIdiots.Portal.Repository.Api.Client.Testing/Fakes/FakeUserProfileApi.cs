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
        if (_userProfiles.TryGetValue(userProfileId, out var up) && up.ArchivedAtUtc == null)
        {
            return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(up)));
        }

        return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.NotFound, new ApiResponse<UserProfileDto>(new ApiError("NOT_FOUND", "User profile not found"))));
    }

    public Task<ApiResult<UserProfileDto>> GetUserProfileByIdentityId(string identityId, CancellationToken cancellationToken = default)
    {
        var up = _userProfiles.Values.FirstOrDefault(u => u.IdentityOid == identityId && u.ArchivedAtUtc == null);
        if (up != null)
        {
            return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(up)));
        }

        return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.NotFound, new ApiResponse<UserProfileDto>(new ApiError("NOT_FOUND", "User profile not found"))));
    }

    public Task<ApiResult<UserProfileDto>> GetUserProfileByXtremeIdiotsId(string xtremeIdiotsId, CancellationToken cancellationToken = default)
    {
        var up = _userProfiles.Values.FirstOrDefault(u => u.XtremeIdiotsForumId == xtremeIdiotsId && u.ArchivedAtUtc == null);
        if (up != null)
        {
            return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(up)));
        }

        return Task.FromResult(new ApiResult<UserProfileDto>(HttpStatusCode.NotFound, new ApiResponse<UserProfileDto>(new ApiError("NOT_FOUND", "User profile not found"))));
    }

    public Task<ApiResult<UserProfileDto>> GetUserProfileByDemoAuthKey(string demoAuthKey, CancellationToken cancellationToken = default)
    {
        var up = _userProfiles.Values.FirstOrDefault(u => u.DemoAuthKey == demoAuthKey && u.ArchivedAtUtc == null);
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
        IEnumerable<UserProfileDto> query = _userProfiles.Values.Where(up => up.ArchivedAtUtc == null);

        var totalCount = query.Count();

        if (!string.IsNullOrWhiteSpace(filterString))
        {
            var textFilter = filterString.Trim().ToLower();
            query = query.Where(up => (up.IdentityOid != null && up.IdentityOid.ToLower().Contains(textFilter)) ||
                                       (up.XtremeIdiotsForumId != null && up.XtremeIdiotsForumId.ToLower().Contains(textFilter)) ||
                                       (up.DemoAuthKey != null && up.DemoAuthKey.ToLower().Contains(textFilter)) ||
                                       (up.DisplayName != null && up.DisplayName.ToLower().Contains(textFilter)) ||
                                       (up.Email != null && up.Email.ToLower().Contains(textFilter)));
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

    public Task<ApiResult<UserProfileDto>> GetUserProfileIncludingArchived(Guid userProfileId, CancellationToken cancellationToken = default)
        => Task.FromResult(_userProfiles.TryGetValue(userProfileId, out var profile)
            ? new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(profile))
            : new ApiResult<UserProfileDto>(HttpStatusCode.NotFound));

    public Task<ApiResult> ArchiveUserProfile(ArchiveUserProfileDto archiveUserProfileDto, CancellationToken cancellationToken = default)
    {
        var dto = archiveUserProfileDto;
        lock (_userProfiles)
        {
            if (_errorResponses.TryGetValue(nameof(ArchiveUserProfile), out var error))
            {
                return Task.FromResult(new ApiResult(error.StatusCode, new ApiResponse(error.Error)));
            }
            if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length > 500)
            {
                return Result(HttpStatusCode.BadRequest);
            }
            if (dto.UserProfileId == dto.ArchivedByUserProfileId)
            {
                return Result(HttpStatusCode.Conflict);
            }
            if (!_userProfiles.TryGetValue(dto.ArchivedByUserProfileId, out var actor) || actor.ArchivedAtUtc != null)
            {
                return Result(HttpStatusCode.BadRequest);
            }
            if (!_userProfiles.TryGetValue(dto.UserProfileId, out var profile))
            {
                return Result(HttpStatusCode.NotFound);
            }
            if (profile.ArchivedAtUtc == null)
            {
                _userProfiles[dto.UserProfileId] = profile with
                {
                    ArchivedAtUtc = DateTime.UtcNow,
                    ArchivedByUserProfileId = dto.ArchivedByUserProfileId,
                    ArchiveReason = dto.Reason.Trim(),
                    DemoAuthKey = null
                };
                return Result(HttpStatusCode.OK);
            }
            return Result(HttpStatusCode.NoContent);
        }
    }

    private static Task<ApiResult> Result(HttpStatusCode status) => Task.FromResult(new ApiResult(status, new ApiResponse()));
    private bool IsArchived(Guid id) => _userProfiles.TryGetValue(id, out var profile) && profile.ArchivedAtUtc != null;
    private bool IsReserved(string? identity, string? forumId, Guid? except = null) =>
        _userProfiles.Values.Any(up => up.UserProfileId != except &&
            (UserProfileIdentifierComparison.Equals(up.IdentityOid, identity) || UserProfileIdentifierComparison.Equals(up.XtremeIdiotsForumId, forumId)));

    public Task<ApiResult> CreateUserProfile(CreateUserProfileDto createUserProfileDto, CancellationToken cancellationToken = default)
        => Result(IsReserved(createUserProfileDto.IdentityOid, createUserProfileDto.XtremeIdiotsForumId) ? HttpStatusCode.Conflict : HttpStatusCode.OK);
    public Task<ApiResult> CreateUserProfiles(List<CreateUserProfileDto> createUserProfileDtos, CancellationToken cancellationToken = default)
        => Result(UserProfileIdentifierComparison.HasDuplicates(createUserProfileDtos.Select(dto => dto.IdentityOid)) ||
            UserProfileIdentifierComparison.HasDuplicates(createUserProfileDtos.Select(dto => dto.XtremeIdiotsForumId)) ||
            createUserProfileDtos.Any(dto => IsReserved(dto.IdentityOid, dto.XtremeIdiotsForumId)) ? HttpStatusCode.Conflict : HttpStatusCode.OK);
    public Task<ApiResult> UpdateUserProfile(EditUserProfileDto editUserProfileDto, CancellationToken cancellationToken = default)
        => Result(IsArchived(editUserProfileDto.UserProfileId) ? HttpStatusCode.NotFound : IsReserved(editUserProfileDto.IdentityOid, editUserProfileDto.XtremeIdiotsForumId, editUserProfileDto.UserProfileId) ? HttpStatusCode.Conflict : HttpStatusCode.OK);
    public Task<ApiResult> UpdateUserProfiles(List<EditUserProfileDto> editUserProfileDtos, CancellationToken cancellationToken = default)
        => Result(UserProfileIdentifierComparison.HasDuplicates(editUserProfileDtos.Select(dto => dto.IdentityOid)) ||
            UserProfileIdentifierComparison.HasDuplicates(editUserProfileDtos.Select(dto => dto.XtremeIdiotsForumId)) ? HttpStatusCode.Conflict :
            editUserProfileDtos.Any(dto => IsArchived(dto.UserProfileId)) ? HttpStatusCode.NotFound :
            editUserProfileDtos.Any(dto => IsReserved(dto.IdentityOid, dto.XtremeIdiotsForumId, dto.UserProfileId)) ? HttpStatusCode.Conflict : HttpStatusCode.OK);
    public Task<ApiResult> CreateUserProfileClaim(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto, CancellationToken cancellationToken = default)
        => Result(IsArchived(userProfileId) ? HttpStatusCode.NotFound : HttpStatusCode.OK);
    public Task<ApiResult> SetUserProfileClaims(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto, CancellationToken cancellationToken = default)
        => Result(IsArchived(userProfileId) ? HttpStatusCode.NotFound : HttpStatusCode.OK);
    public Task<ApiResult> DeleteUserProfileClaim(Guid userProfileId, Guid userProfileClaimId, CancellationToken cancellationToken = default)
        => Result(IsArchived(userProfileId) ? HttpStatusCode.NotFound : HttpStatusCode.OK);
    public Task<ApiResult<CollectionModel<PermissionReportEntryDto>>> GetPermissionsReport(GameType? gameType, string? claimType, CancellationToken cancellationToken = default)
    {
        var collection = new CollectionModel<PermissionReportEntryDto>(new List<PermissionReportEntryDto>());
        return Task.FromResult(new ApiResult<CollectionModel<PermissionReportEntryDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<PermissionReportEntryDto>>(collection)));
    }
}
