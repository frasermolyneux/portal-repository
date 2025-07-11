using System.Net;
using Asp.Versioning;
using AutoMapper;

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

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class UserProfileController : ControllerBase, IUserProfileApi
    {
        private readonly PortalDbContext context;
        private readonly IMapper mapper;

        public UserProfileController(
            PortalDbContext context,
            IMapper mapper)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [Route("user-profile/{userProfileId}")]
        public async Task<IActionResult> GetUserProfile(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).GetUserProfile(userProfileId, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<UserProfileDto>> IUserProfileApi.GetUserProfile(Guid userProfileId, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .SingleOrDefaultAsync(up => up.UserProfileId == userProfileId, cancellationToken);

            if (userProfile == null)
                return new ApiResult<UserProfileDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<UserProfileDto>(userProfile);
            return new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(result));
        }

        [HttpGet]
        [Route("user-profile/by-identity-id/{identityId}")]
        public async Task<IActionResult> GetUserProfileByIdentityId(string identityId, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).GetUserProfileByIdentityId(identityId, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<UserProfileDto>> IUserProfileApi.GetUserProfileByIdentityId(string identityId, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .SingleOrDefaultAsync(up => up.IdentityOid == identityId, cancellationToken);

            if (userProfile == null)
                return new ApiResult<UserProfileDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<UserProfileDto>(userProfile);
            return new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(result));
        }

        [HttpGet]
        [Route("user-profile/by-xtremeidiots-id/{xtremeIdiotsId}")]
        public async Task<IActionResult> GetUserProfileByXtremeIdiotsId(string xtremeIdiotsId, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).GetUserProfileByXtremeIdiotsId(xtremeIdiotsId, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<UserProfileDto>> IUserProfileApi.GetUserProfileByXtremeIdiotsId(string xtremeIdiotsId, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .SingleOrDefaultAsync(up => up.XtremeIdiotsForumId == xtremeIdiotsId, cancellationToken);

            if (userProfile == null)
                return new ApiResult<UserProfileDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<UserProfileDto>(userProfile);
            return new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(result));
        }

        [HttpGet]
        [Route("user-profile/by-demo-auth-key/{demoAuthKey}")]
        public async Task<IActionResult> GetUserProfileByDemoAuthKey(string demoAuthKey, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).GetUserProfileByDemoAuthKey(demoAuthKey, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<UserProfileDto>> IUserProfileApi.GetUserProfileByDemoAuthKey(string demoAuthKey, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .SingleOrDefaultAsync(up => up.DemoAuthKey == demoAuthKey, cancellationToken);

            if (userProfile == null)
                return new ApiResult<UserProfileDto>(HttpStatusCode.NotFound);

            var result = mapper.Map<UserProfileDto>(userProfile);
            return new ApiResult<UserProfileDto>(HttpStatusCode.OK, new ApiResponse<UserProfileDto>(result));
        }

        [HttpGet]
        [Route("user-profiles")]
        public async Task<IActionResult> GetUserProfiles([FromQuery] string? filterString, [FromQuery] int skipEntries = 0, [FromQuery] int takeEntries = 50, [FromQuery] UserProfilesOrder? order = null, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).GetUserProfiles(filterString, skipEntries, takeEntries, order, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<UserProfileDto>>> IUserProfileApi.GetUserProfiles(string? filterString, int skipEntries, int takeEntries, UserProfilesOrder? order, CancellationToken cancellationToken)
        {
            var query = context.UserProfiles.Include(up => up.UserProfileClaims).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filterString))
            {
                var filter = filterString.Trim().ToLower();
                query = query.Where(up => (up.IdentityOid != null && up.IdentityOid.ToLower().Contains(filter)) ||
                                         (up.XtremeIdiotsForumId != null && up.XtremeIdiotsForumId.ToLower().Contains(filter)) ||
                                         (up.DemoAuthKey != null && up.DemoAuthKey.ToLower().Contains(filter)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            switch (order)
            {
                case UserProfilesOrder.DisplayNameAsc:
                    query = query.OrderBy(up => up.IdentityOid);
                    break;
                case UserProfilesOrder.DisplayNameDesc:
                    query = query.OrderByDescending(up => up.IdentityOid);
                    break;
                default:
                    query = query.OrderBy(up => up.IdentityOid);
                    break;
            }

            var filteredCount = await query.CountAsync(cancellationToken);
            query = query.Skip(skipEntries).Take(takeEntries);
            var results = await query.ToListAsync(cancellationToken);

            var items = results.Select(up => mapper.Map<UserProfileDto>(up)).ToList();

            var result = new CollectionModel<UserProfileDto>
            {
                Items = items,
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };

            return new ApiResult<CollectionModel<UserProfileDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<UserProfileDto>>(result));
        }

        [HttpPost]
        [Route("user-profile")]
        public async Task<IActionResult> CreateUserProfile(CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

            CreateUserProfileDto? createUserProfileDto;
            try
            {
                createUserProfileDto = JsonConvert.DeserializeObject<CreateUserProfileDto>(requestBody);
            }
            catch
            {
                return BadRequest();
            }

            if (createUserProfileDto == null)
                return BadRequest();

            var response = await ((IUserProfileApi)this).CreateUserProfile(createUserProfileDto, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IUserProfileApi.CreateUserProfile(CreateUserProfileDto createUserProfileDto, CancellationToken cancellationToken)
        {
            if (await context.UserProfiles.AnyAsync(up => up.IdentityOid == createUserProfileDto.IdentityOid, cancellationToken))
                return new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.UserProfileConflictMessage)).ToConflictResult();

            var userProfile = mapper.Map<UserProfile>(createUserProfileDto);
            await context.UserProfiles.AddAsync(userProfile, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResult(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("user-profiles")]
        public async Task<IActionResult> CreateUserProfiles(CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

            List<CreateUserProfileDto>? createUserProfileDtos;
            try
            {
                createUserProfileDtos = JsonConvert.DeserializeObject<List<CreateUserProfileDto>>(requestBody);
            }
            catch
            {
                return BadRequest();
            }

            if (createUserProfileDtos == null || !createUserProfileDtos.Any())
                return BadRequest();

            var response = await ((IUserProfileApi)this).CreateUserProfiles(createUserProfileDtos, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IUserProfileApi.CreateUserProfiles(List<CreateUserProfileDto> createUserProfileDtos, CancellationToken cancellationToken)
        {
            foreach (var createUserProfileDto in createUserProfileDtos)
            {
                if (await context.UserProfiles.AnyAsync(up => up.IdentityOid == createUserProfileDto.IdentityOid, cancellationToken))
                    return new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, ApiErrorMessages.UserProfileConflictMessage)).ToConflictResult();

                var userProfile = mapper.Map<UserProfile>(createUserProfileDto);
                await context.UserProfiles.AddAsync(userProfile, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResult(HttpStatusCode.OK);
        }

        [HttpPatch]
        [Route("user-profile/{userProfileId}")]
        public async Task<IActionResult> UpdateUserProfile(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

            EditUserProfileDto? editUserProfileDto;
            try
            {
                editUserProfileDto = JsonConvert.DeserializeObject<EditUserProfileDto>(requestBody);
            }
            catch
            {
                return BadRequest();
            }

            if (editUserProfileDto == null)
                return BadRequest();

            if (editUserProfileDto.UserProfileId != userProfileId)
                return new ApiResponse(new ApiError(ApiErrorCodes.EntityIdMismatch, ApiErrorMessages.UserProfileIdMismatchMessage)).ToBadRequestResult().ToHttpResult();

            var response = await ((IUserProfileApi)this).UpdateUserProfile(editUserProfileDto, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IUserProfileApi.UpdateUserProfile(EditUserProfileDto editUserProfileDto, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .SingleOrDefaultAsync(up => up.UserProfileId == editUserProfileDto.UserProfileId, cancellationToken);

            if (userProfile == null)
                return new ApiResult(HttpStatusCode.NotFound);

            mapper.Map(editUserProfileDto, userProfile);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResult(HttpStatusCode.OK);
        }

        [HttpPatch]
        [Route("user-profiles")]
        public async Task<IActionResult> UpdateUserProfiles(CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

            List<EditUserProfileDto>? editUserProfileDtos;
            try
            {
                editUserProfileDtos = JsonConvert.DeserializeObject<List<EditUserProfileDto>>(requestBody);
            }
            catch
            {
                return BadRequest();
            }

            if (editUserProfileDtos == null || !editUserProfileDtos.Any())
                return BadRequest();

            var response = await ((IUserProfileApi)this).UpdateUserProfiles(editUserProfileDtos, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IUserProfileApi.UpdateUserProfiles(List<EditUserProfileDto> editUserProfileDtos, CancellationToken cancellationToken)
        {
            foreach (var editUserProfileDto in editUserProfileDtos)
            {
                var userProfile = await context.UserProfiles
                    .SingleOrDefaultAsync(up => up.UserProfileId == editUserProfileDto.UserProfileId, cancellationToken);

                if (userProfile == null)
                    return new ApiResult(HttpStatusCode.NotFound);

                mapper.Map(editUserProfileDto, userProfile);
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResult(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("user-profile/{userProfileId}/claims")]
        public async Task<IActionResult> CreateUserProfileClaim(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

            List<CreateUserProfileClaimDto>? createUserProfileClaimDtos;
            try
            {
                createUserProfileClaimDtos = JsonConvert.DeserializeObject<List<CreateUserProfileClaimDto>>(requestBody);
            }
            catch
            {
                return BadRequest();
            }

            if (createUserProfileClaimDtos == null || !createUserProfileClaimDtos.Any())
                return BadRequest();

            var response = await ((IUserProfileApi)this).CreateUserProfileClaim(userProfileId, createUserProfileClaimDtos, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IUserProfileApi.CreateUserProfileClaim(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDtos, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .SingleOrDefaultAsync(up => up.UserProfileId == userProfileId, cancellationToken);

            if (userProfile == null)
                return new ApiResult(HttpStatusCode.NotFound);

            foreach (var createUserProfileClaimDto in createUserProfileClaimDtos)
            {
                if (userProfile.UserProfileClaims.Any(upc => upc.ClaimType == createUserProfileClaimDto.ClaimType))
                    continue;

                var userProfileClaim = mapper.Map<UserProfileClaim>(createUserProfileClaimDto);
                userProfileClaim.UserProfileId = userProfileId;
                await context.UserProfileClaims.AddAsync(userProfileClaim, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResult(HttpStatusCode.OK);
        }

        [HttpPut]
        [Route("user-profile/{userProfileId}/claims")]
        public async Task<IActionResult> SetUserProfileClaims(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

            List<CreateUserProfileClaimDto>? createUserProfileClaimDtos;
            try
            {
                createUserProfileClaimDtos = JsonConvert.DeserializeObject<List<CreateUserProfileClaimDto>>(requestBody);
            }
            catch
            {
                return BadRequest();
            }

            if (createUserProfileClaimDtos == null)
                return BadRequest();

            var response = await ((IUserProfileApi)this).SetUserProfileClaims(userProfileId, createUserProfileClaimDtos, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IUserProfileApi.SetUserProfileClaims(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDtos, CancellationToken cancellationToken)
        {
            var userProfile = await context.UserProfiles
                .Include(up => up.UserProfileClaims)
                .SingleOrDefaultAsync(up => up.UserProfileId == userProfileId, cancellationToken);

            if (userProfile == null)
                return new ApiResult(HttpStatusCode.NotFound);

            // Remove existing claims
            context.UserProfileClaims.RemoveRange(userProfile.UserProfileClaims);

            // Add new claims
            foreach (var createUserProfileClaimDto in createUserProfileClaimDtos)
            {
                var userProfileClaim = mapper.Map<UserProfileClaim>(createUserProfileClaimDto);
                userProfileClaim.UserProfileId = userProfileId;
                await context.UserProfileClaims.AddAsync(userProfileClaim, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResult(HttpStatusCode.OK);
        }

        [HttpDelete]
        [Route("user-profile/{userProfileId}/claims/{userProfileClaimId}")]
        public async Task<IActionResult> DeleteUserProfileClaim(Guid userProfileId, Guid userProfileClaimId, CancellationToken cancellationToken = default)
        {
            var response = await ((IUserProfileApi)this).DeleteUserProfileClaim(userProfileId, userProfileClaimId, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IUserProfileApi.DeleteUserProfileClaim(Guid userProfileId, Guid userProfileClaimId, CancellationToken cancellationToken)
        {
            var userProfileClaim = await context.UserProfileClaims
                .SingleOrDefaultAsync(upc => upc.UserProfileId == userProfileId && upc.UserProfileClaimId == userProfileClaimId, cancellationToken);

            if (userProfileClaim == null)
                return new ApiResponse(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)).ToNotFoundResult();

            context.UserProfileClaims.Remove(userProfileClaim);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResult(HttpStatusCode.OK);
        }
    }
}

