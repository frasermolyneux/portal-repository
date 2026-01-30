using System.Net;
using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class UserProfileController : ControllerBase, IUserProfileApi
    {
        private readonly PortalDbContext context;

        public UserProfileController(
            PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        /// <summary>
        /// Retrieves a user profile by its unique identifier.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The user profile details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("user-profile/{userProfileId:guid}")]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserProfile(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).GetUserProfile(userProfileId, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a user profile by its unique identifier.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the user profile details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<UserProfileDto>> IUserProfileApi.GetUserProfile(Guid userProfileId, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .AsNoTracking()
                .FirstOrDefaultAsync(up => up.UserProfileId == userProfileId, cancellationToken);

            if (userProfile == null)
                return new ApiResult<UserProfileDto>(HttpStatusCode.NotFound);

            var result = userProfile.ToDto();
            return new ApiResponse<UserProfileDto>(result).ToApiResult();
        }

        /// <summary>
        /// Retrieves a user profile by its identity identifier.
        /// </summary>
        /// <param name="identityId">The identity identifier of the user profile to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The user profile details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("user-profile/by-identity-id/{identityId}")]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserProfileByIdentityId(string identityId, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).GetUserProfileByIdentityId(identityId, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a user profile by its identity identifier.
        /// </summary>
        /// <param name="identityId">The identity identifier of the user profile to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the user profile details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<UserProfileDto>> IUserProfileApi.GetUserProfileByIdentityId(string identityId, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .AsNoTracking()
                .FirstOrDefaultAsync(up => up.IdentityOid == identityId, cancellationToken);

            if (userProfile == null)
                return new ApiResult<UserProfileDto>(HttpStatusCode.NotFound);

            var result = userProfile.ToDto();
            return new ApiResponse<UserProfileDto>(result).ToApiResult();
        }

        /// <summary>
        /// Retrieves a user profile by its XtremeIdiots forum identifier.
        /// </summary>
        /// <param name="xtremeIdiotsId">The XtremeIdiots forum identifier of the user profile to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The user profile details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("user-profile/by-xtremeidiots-id/{xtremeIdiotsId}")]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserProfileByXtremeIdiotsId(string xtremeIdiotsId, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).GetUserProfileByXtremeIdiotsId(xtremeIdiotsId, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a user profile by its XtremeIdiots forum identifier.
        /// </summary>
        /// <param name="xtremeIdiotsId">The XtremeIdiots forum identifier of the user profile to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the user profile details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<UserProfileDto>> IUserProfileApi.GetUserProfileByXtremeIdiotsId(string xtremeIdiotsId, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .AsNoTracking()
                .FirstOrDefaultAsync(up => up.XtremeIdiotsForumId == xtremeIdiotsId, cancellationToken);

            if (userProfile == null)
                return new ApiResult<UserProfileDto>(HttpStatusCode.NotFound);

            var result = userProfile.ToDto();
            return new ApiResponse<UserProfileDto>(result).ToApiResult();
        }

        /// <summary>
        /// Retrieves a user profile by its demo authentication key.
        /// </summary>
        /// <param name="demoAuthKey">The demo authentication key of the user profile to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The user profile details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("user-profile/by-demo-auth-key/{demoAuthKey}")]
        [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserProfileByDemoAuthKey(string demoAuthKey, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).GetUserProfileByDemoAuthKey(demoAuthKey, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a user profile by its demo authentication key.
        /// </summary>
        /// <param name="demoAuthKey">The demo authentication key of the user profile to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the user profile details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<UserProfileDto>> IUserProfileApi.GetUserProfileByDemoAuthKey(string demoAuthKey, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .AsNoTracking()
                .FirstOrDefaultAsync(up => up.DemoAuthKey == demoAuthKey, cancellationToken);

            if (userProfile == null)
                return new ApiResult<UserProfileDto>(HttpStatusCode.NotFound);

            var result = userProfile.ToDto();
            return new ApiResponse<UserProfileDto>(result).ToApiResult();
        }

        /// <summary>
        /// Retrieves a paginated list of user profiles with optional filtering and sorting.
        /// </summary>
        /// <param name="filterString">Optional filter string to search for user profiles.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 50).</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A paginated collection of user profiles.</returns>
        [HttpGet("user-profiles")]
        [ProducesResponseType<CollectionModel<UserProfileDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserProfiles([FromQuery] string? filterString, [FromQuery] UserProfileFilter? filter = null, [FromQuery] int skipEntries = 0, [FromQuery] int takeEntries = 50, [FromQuery] UserProfilesOrder? order = null, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).GetUserProfiles(filterString, filter, skipEntries, takeEntries, order, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of user profiles with optional filtering and sorting.
        /// </summary>
        /// <param name="filterString">Optional filter string to search for user profiles.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of user profiles.</returns>
        async Task<ApiResult<CollectionModel<UserProfileDto>>> IUserProfileApi.GetUserProfiles(string? filterString, UserProfileFilter? filter, int skipEntries, int takeEntries, UserProfilesOrder? order, CancellationToken cancellationToken)
        {
            var baseQuery = context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .AsNoTracking()
                .AsQueryable();

            // Calculate total count before applying filters
            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Apply filtering
            var filteredQuery = ApplyFilters(baseQuery, filterString, filter);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken);

            // Apply ordering and pagination
            var orderedQuery = ApplyOrderingAndPagination(filteredQuery, skipEntries, takeEntries, order);
            var results = await orderedQuery.ToListAsync(cancellationToken);

            var entries = results.Select(up => up.ToDto()).ToList();

            var data = new CollectionModel<UserProfileDto>(entries);

            return new ApiResponse<CollectionModel<UserProfileDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        private static IQueryable<UserProfile> ApplyFilters(IQueryable<UserProfile> query, string? filterString, UserProfileFilter? filter)
        {
            if (!string.IsNullOrWhiteSpace(filterString))
            {
                var textFilter = filterString.Trim().ToLower();
                query = query.Where(up => (up.IdentityOid != null && up.IdentityOid.ToLower().Contains(textFilter)) ||
                                         (up.XtremeIdiotsForumId != null && up.XtremeIdiotsForumId.ToLower().Contains(textFilter)) ||
                                         (up.DemoAuthKey != null && up.DemoAuthKey.ToLower().Contains(textFilter)) ||
                                         (up.DisplayName != null && up.DisplayName.ToLower().Contains(textFilter)) ||
                                         (up.Email != null && up.Email.ToLower().Contains(textFilter)));
            }

            if (filter.HasValue)
            {
                query = filter.Value switch
                {
                    UserProfileFilter.SeniorAdmins => query.Where(up => up.UserProfileClaims.Any(c => c.ClaimType == UserProfileClaimType.SeniorAdmin)),
                    UserProfileFilter.HeadAdmins => query.Where(up => up.UserProfileClaims.Any(c => c.ClaimType == UserProfileClaimType.HeadAdmin)),
                    UserProfileFilter.GameAdmins => query.Where(up => up.UserProfileClaims.Any(c => c.ClaimType == UserProfileClaimType.GameAdmin)),
                    UserProfileFilter.Moderators => query.Where(up => up.UserProfileClaims.Any(c => c.ClaimType == UserProfileClaimType.Moderator)),
                    UserProfileFilter.AnyAdmin => query.Where(up => up.UserProfileClaims.Any(c => c.ClaimType == UserProfileClaimType.SeniorAdmin || c.ClaimType == UserProfileClaimType.HeadAdmin || c.ClaimType == UserProfileClaimType.GameAdmin || c.ClaimType == UserProfileClaimType.Moderator)),
                    UserProfileFilter.HasCustomPermissions => query.Where(up => up.UserProfileClaims.Any(c => c.ClaimType == UserProfileClaimType.FtpCredentials || c.ClaimType == UserProfileClaimType.RconCredentials || c.ClaimType == UserProfileClaimType.GameServer || c.ClaimType == UserProfileClaimType.BanFileMonitor || c.ClaimType == UserProfileClaimType.RconMonitor || c.ClaimType == UserProfileClaimType.ServerAdmin || c.ClaimType == UserProfileClaimType.LiveRcon)),
                    _ => query
                };
            }

            return query;
        }

        private static IQueryable<UserProfile> ApplyOrderingAndPagination(IQueryable<UserProfile> query, int skipEntries, int takeEntries, UserProfilesOrder? order)
        {
            var orderedQuery = order switch
            {
                UserProfilesOrder.DisplayNameAsc => query.OrderBy(up => up.DisplayName),
                UserProfilesOrder.DisplayNameDesc => query.OrderByDescending(up => up.DisplayName),
                _ => query.OrderBy(up => up.DisplayName)
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }

        /// <summary>
        /// Creates a new user profile.
        /// </summary>
        /// <param name="createUserProfileDto">The user profile data to create.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response indicating the user profile was created.</returns>
        [HttpPost("user-profile")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateUserProfile([FromBody] CreateUserProfileDto createUserProfileDto, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).CreateUserProfile(createUserProfileDto, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates a new user profile.
        /// </summary>
        /// <param name="createUserProfileDto">The user profile data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the user profile was created.</returns>
        async Task<ApiResult> IUserProfileApi.CreateUserProfile(CreateUserProfileDto createUserProfileDto, CancellationToken cancellationToken)
        {
            if (await context.UserProfiles.AsNoTracking().AnyAsync(up => up.IdentityOid == createUserProfileDto.IdentityOid, cancellationToken))
                return new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.UserProfileConflictMessage)).ToConflictResult();

            var userProfile = createUserProfileDto.ToEntity();
            await context.UserProfiles.AddAsync(userProfile, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Creates multiple user profiles.
        /// </summary>
        /// <param name="createUserProfileDtos">The user profile data to create.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response indicating the user profiles were created.</returns>
        [HttpPost("user-profiles")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateUserProfiles([FromBody] List<CreateUserProfileDto> createUserProfileDtos, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).CreateUserProfiles(createUserProfileDtos, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates multiple user profiles.
        /// </summary>
        /// <param name="createUserProfileDtos">The user profile data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the user profiles were created.</returns>
        async Task<ApiResult> IUserProfileApi.CreateUserProfiles(List<CreateUserProfileDto> createUserProfileDtos, CancellationToken cancellationToken)
        {
            foreach (var createUserProfileDto in createUserProfileDtos)
            {
                if (await context.UserProfiles.AsNoTracking().AnyAsync(up => up.IdentityOid == createUserProfileDto.IdentityOid, cancellationToken))
                    return new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.UserProfileConflictMessage)).ToConflictResult();

                var userProfile = createUserProfileDto.ToEntity();
                await context.UserProfiles.AddAsync(userProfile, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Updates an existing user profile.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile to update.</param>
        /// <param name="editUserProfileDto">The user profile data to update.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the user profile was updated; otherwise, a 404 Not Found response.</returns>
        [HttpPatch("user-profile/{userProfileId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUserProfile(Guid userProfileId, [FromBody] EditUserProfileDto editUserProfileDto, CancellationToken cancellationToken = default)
        {
            if (editUserProfileDto.UserProfileId != userProfileId)
                return new ApiResponse(new ApiError(ApiErrorCodes.EntityIdMismatch, ApiErrorMessages.UserProfileIdMismatchMessage)).ToBadRequestResult().ToHttpResult();

            var response = await ((IUserProfileApi)this).UpdateUserProfile(editUserProfileDto, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates an existing user profile.
        /// </summary>
        /// <param name="editUserProfileDto">The user profile data to update.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the user profile was updated if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IUserProfileApi.UpdateUserProfile(EditUserProfileDto editUserProfileDto, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .FirstOrDefaultAsync(up => up.UserProfileId == editUserProfileDto.UserProfileId, cancellationToken);

            if (userProfile == null)
                return new ApiResult(HttpStatusCode.NotFound);

            editUserProfileDto.ApplyTo(userProfile);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Updates multiple user profiles.
        /// </summary>
        /// <param name="editUserProfileDtos">The user profile data to update.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if all user profiles were updated; otherwise, a 404 Not Found response.</returns>
        [HttpPatch("user-profiles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUserProfiles([FromBody] List<EditUserProfileDto> editUserProfileDtos, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).UpdateUserProfiles(editUserProfileDtos, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates multiple user profiles.
        /// </summary>
        /// <param name="editUserProfileDtos">The user profile data to update.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the user profiles were updated if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IUserProfileApi.UpdateUserProfiles(List<EditUserProfileDto> editUserProfileDtos, CancellationToken cancellationToken)
        {
            foreach (var editUserProfileDto in editUserProfileDtos)
            {
                var userProfile = await context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserProfileId == editUserProfileDto.UserProfileId, cancellationToken);

                if (userProfile == null)
                    return new ApiResult(HttpStatusCode.NotFound);

                editUserProfileDto.ApplyTo(userProfile);
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Creates user profile claims for a specific user profile.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile to add claims to.</param>
        /// <param name="createUserProfileClaimDtos">The claim data to create.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the claims were created; otherwise, a 404 Not Found response.</returns>
        [HttpPost("user-profile/{userProfileId:guid}/claims")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateUserProfileClaim(Guid userProfileId, [FromBody] List<CreateUserProfileClaimDto> createUserProfileClaimDtos, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).CreateUserProfileClaim(userProfileId, createUserProfileClaimDtos, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates user profile claims for a specific user profile.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile to add claims to.</param>
        /// <param name="createUserProfileClaimDtos">The claim data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the claims were created if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IUserProfileApi.CreateUserProfileClaim(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDtos, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .AsNoTracking()
                .FirstOrDefaultAsync(up => up.UserProfileId == userProfileId, cancellationToken);

            if (userProfile == null)
                return new ApiResult(HttpStatusCode.NotFound);

            foreach (var createUserProfileClaimDto in createUserProfileClaimDtos)
            {
                if (userProfile.UserProfileClaims.Any(upc => upc.ClaimType == createUserProfileClaimDto.ClaimType))
                    continue;

                var userProfileClaim = createUserProfileClaimDto.ToEntity();
                userProfileClaim.UserProfileId = userProfileId;
                await context.UserProfileClaims.AddAsync(userProfileClaim, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Sets (replaces) all user profile claims for a specific user profile.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile to set claims for.</param>
        /// <param name="createUserProfileClaimDtos">The claim data to set.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the claims were set; otherwise, a 404 Not Found response.</returns>
        [HttpPut("user-profile/{userProfileId:guid}/claims")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetUserProfileClaims(Guid userProfileId, [FromBody] List<CreateUserProfileClaimDto> createUserProfileClaimDtos, CancellationToken cancellationToken = default)
        {
            if (createUserProfileClaimDtos == null)
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage)).ToBadRequestResult().ToHttpResult();

            // Reject duplicate claim types in request
            var duplicateClaimTypes = createUserProfileClaimDtos
                .GroupBy(c => c.ClaimType, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateClaimTypes.Any())
            {
                var err = new ApiResponse(new ApiError(ApiErrorCodes.RequestEntityMismatch, $"Duplicate claim types: {string.Join(", ", duplicateClaimTypes)}"));
                return err.ToBadRequestResult().ToHttpResult();
            }
            var response = await ((IUserProfileApi)this).SetUserProfileClaims(userProfileId, createUserProfileClaimDtos, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Sets (replaces) all user profile claims for a specific user profile.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile to set claims for.</param>
        /// <param name="createUserProfileClaimDtos">The claim data to set.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the claims were set if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IUserProfileApi.SetUserProfileClaims(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDtos, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .FirstOrDefaultAsync(up => up.UserProfileId == userProfileId, cancellationToken);

            if (userProfile == null)
                return new ApiResult(HttpStatusCode.NotFound);

            // Remove existing claims
            context.UserProfileClaims.RemoveRange(userProfile.UserProfileClaims);

            // Add new claims
            foreach (var createUserProfileClaimDto in createUserProfileClaimDtos)
            {
                var userProfileClaim = createUserProfileClaimDto.ToEntity();
                userProfileClaim.UserProfileId = userProfileId;
                await context.UserProfileClaims.AddAsync(userProfileClaim, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Deletes a user profile claim by its unique identifier.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile.</param>
        /// <param name="userProfileClaimId">The unique identifier of the user profile claim to delete.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the claim was deleted; otherwise, a 404 Not Found response.</returns>
        [HttpDelete("user-profile/{userProfileId:guid}/claims/{userProfileClaimId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUserProfileClaim(Guid userProfileId, Guid userProfileClaimId, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).DeleteUserProfileClaim(userProfileId, userProfileClaimId, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Deletes a user profile claim by its unique identifier.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile.</param>
        /// <param name="userProfileClaimId">The unique identifier of the user profile claim to delete.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the claim was deleted if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IUserProfileApi.DeleteUserProfileClaim(Guid userProfileId, Guid userProfileClaimId, CancellationToken cancellationToken)
        {
            var userProfileClaim = await context.UserProfileClaims
                .FirstOrDefaultAsync(upc => upc.UserProfileId == userProfileId && upc.UserProfileClaimId == userProfileClaimId, cancellationToken);

            if (userProfileClaim == null)
                return new ApiResult(HttpStatusCode.NotFound);

            context.UserProfileClaims.Remove(userProfileClaim);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult();
        }
    }
}

