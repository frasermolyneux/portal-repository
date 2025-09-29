using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.DataLib;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using Asp.Versioning;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class AdminActionsController : ControllerBase, IAdminActionsApi
    {
        private readonly PortalDbContext context;

        public AdminActionsController(PortalDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves a specific admin action by its unique identifier.
        /// </summary>
        /// <param name="adminActionId">The unique identifier of the admin action to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The admin action details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("admin-actions/{adminActionId:guid}")]
        [ProducesResponseType<AdminActionDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAdminAction(Guid adminActionId, CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).GetAdminAction(adminActionId, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a specific admin action by its unique identifier.
        /// </summary>
        /// <param name="adminActionId">The unique identifier of the admin action to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the admin action details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<AdminActionDto>> IAdminActionsApi.GetAdminAction(Guid adminActionId, CancellationToken cancellationToken)
        {
            var adminAction = await context.AdminActions
                .Include(a => a.Player)
                .Include(a => a.UserProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AdminActionId == adminActionId, cancellationToken);

            if (adminAction == null)
                return new ApiResult<AdminActionDto>(HttpStatusCode.NotFound);

            var result = adminAction.ToDto();
            return new ApiResponse<AdminActionDto>(result).ToApiResult();
        }

        /// <summary>
        /// Retrieves a paginated list of admin actions with optional filtering and sorting.
        /// </summary>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="playerId">Optional filter by player identifier.</param>
        /// <param name="adminId">Optional filter by admin identifier (supports Identity OID, XtremeIdiots Forum ID, or Demo Auth Key).</param>
        /// <param name="filter">Optional filter criteria for admin actions.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A paginated collection of admin actions.</returns>
        [HttpGet("admin-actions")]
        [ProducesResponseType<CollectionModel<AdminActionDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdminActions(
            [FromQuery] GameType? gameType = null,
            [FromQuery] Guid? playerId = null,
            [FromQuery] string? adminId = null,
            [FromQuery] AdminActionFilter? filter = null,
            [FromQuery] int skipEntries = 0,
            [FromQuery] int takeEntries = 20,
            [FromQuery] AdminActionOrder? order = null,
            CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).GetAdminActions(gameType, playerId, adminId, filter, skipEntries, takeEntries, order, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of admin actions with optional filtering and sorting.
        /// </summary>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="playerId">Optional filter by player identifier.</param>
        /// <param name="adminId">Optional filter by admin identifier.</param>
        /// <param name="filter">Optional filter criteria for admin actions.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of admin actions.</returns>
        async Task<ApiResult<CollectionModel<AdminActionDto>>> IAdminActionsApi.GetAdminActions(
            GameType? gameType,
            Guid? playerId,
            string? adminId,
            AdminActionFilter? filter,
            int skipEntries,
            int takeEntries,
            AdminActionOrder? order,
            CancellationToken cancellationToken)
        {
            var baseQuery = context.AdminActions
                .Include(a => a.Player)
                .Include(a => a.UserProfile)
                .AsNoTracking()
                .AsQueryable();

            // Calculate total count before applying filters
            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Apply filters
            var filteredQuery = ApplyFilters(baseQuery, gameType, playerId, adminId, filter);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken);

            // Apply ordering and pagination
            var orderedQuery = ApplyOrderingAndPagination(filteredQuery, skipEntries, takeEntries, order);
            var adminActions = await orderedQuery.ToListAsync(cancellationToken);

            var entries = adminActions.Select(aa => aa.ToDto()).ToList();

            var data = new CollectionModel<AdminActionDto>(entries);

            return new ApiResponse<CollectionModel<AdminActionDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        private IQueryable<AdminAction> ApplyFilters(IQueryable<AdminAction> query, GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter)
        {
            if (gameType.HasValue)
                query = query.Where(a => a.Player.GameType == (int)gameType.Value);

            if (playerId.HasValue)
                query = query.Where(a => a.PlayerId == playerId.Value);

            if (!string.IsNullOrEmpty(adminId))
                query = query.Where(a => a.UserProfile != null &&
                    (a.UserProfile.IdentityOid == adminId ||
                     a.UserProfile.XtremeIdiotsForumId == adminId ||
                     a.UserProfile.DemoAuthKey == adminId));

            if (filter.HasValue)
            {
                query = filter.Value switch
                {
                    AdminActionFilter.ActiveBans => query.Where(a => (a.Type == (int)AdminActionType.Ban || a.Type == (int)AdminActionType.TempBan) && (a.Expires == null || a.Expires > DateTime.UtcNow)),
                    AdminActionFilter.UnclaimedBans => query.Where(a => a.Type == (int)AdminActionType.Ban && a.UserProfile == null),
                    AdminActionFilter.Observations => query.Where(a => a.Type == (int)AdminActionType.Observation),
                    AdminActionFilter.Warnings => query.Where(a => a.Type == (int)AdminActionType.Warning),
                    AdminActionFilter.Kicks => query.Where(a => a.Type == (int)AdminActionType.Kick),
                    AdminActionFilter.TempBans => query.Where(a => a.Type == (int)AdminActionType.TempBan),
                    AdminActionFilter.PermanentBans => query.Where(a => a.Type == (int)AdminActionType.Ban),
                    AdminActionFilter.AllBans => query.Where(a => a.Type == (int)AdminActionType.Ban || a.Type == (int)AdminActionType.TempBan),
                    AdminActionFilter.Disciplinary => query.Where(a => a.Type == (int)AdminActionType.Warning || a.Type == (int)AdminActionType.Kick || a.Type == (int)AdminActionType.Ban || a.Type == (int)AdminActionType.TempBan),
                    AdminActionFilter.NonBan => query.Where(a => a.Type == (int)AdminActionType.Observation || a.Type == (int)AdminActionType.Warning || a.Type == (int)AdminActionType.Kick),
                    _ => query
                };
            }

            return query;
        }

        private IQueryable<AdminAction> ApplyOrderingAndPagination(IQueryable<AdminAction> query, int skipEntries, int takeEntries, AdminActionOrder? order)
        {
            // Apply ordering
            var orderedQuery = order switch
            {
                AdminActionOrder.CreatedAsc => query.OrderBy(a => a.Created),
                AdminActionOrder.CreatedDesc => query.OrderByDescending(a => a.Created),
                _ => query.OrderByDescending(a => a.Created)
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }

        /// <summary>
        /// Creates a new admin action.
        /// </summary>
        /// <param name="createAdminActionDto">The admin action data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A success response indicating the admin action was created.</returns>
        [HttpPost("admin-actions")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAdminAction([FromBody] CreateAdminActionDto createAdminActionDto, CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).CreateAdminAction(createAdminActionDto, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates a new admin action.
        /// </summary>
        /// <param name="createAdminActionDto">The admin action data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the admin action was created.</returns>
        async Task<ApiResult> IAdminActionsApi.CreateAdminAction(CreateAdminActionDto createAdminActionDto, CancellationToken cancellationToken)
        {
            var adminAction = createAdminActionDto.ToEntity();
            // Match provided admin identifier (could be Identity OID or Forum Id) to a UserProfile
            if (!string.IsNullOrEmpty(createAdminActionDto.AdminId))
            {
                var adminId = createAdminActionDto.AdminId;
                var userProfileId = await context.UserProfiles
                    .Where(up => up.IdentityOid == adminId || up.XtremeIdiotsForumId == adminId)
                    .Select(up => up.UserProfileId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (userProfileId != Guid.Empty)
                    adminAction.UserProfileId = userProfileId;
            }
            context.AdminActions.Add(adminAction);
            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Partially updates an existing admin action. Uses PATCH semantics: only non-null properties in the payload are applied.
        /// </summary>
        /// <param name="adminActionId">The unique identifier of the admin action to update.</param>
        /// <param name="editAdminActionDto">The partial admin action data to apply.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A success response if the admin action was updated; otherwise, a 404 Not Found response.</returns>
        [HttpPatch("admin-actions/{adminActionId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAdminAction([FromRoute] Guid adminActionId, [FromBody] EditAdminActionDto editAdminActionDto, CancellationToken cancellationToken = default)
        {
            if (editAdminActionDto == null)
            {
                var err = new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage));
                return err.ToBadRequestResult().ToHttpResult();
            }

            if (editAdminActionDto.AdminActionId == Guid.Empty || editAdminActionDto.AdminActionId != adminActionId)
            {
                var err = new ApiResponse(new ApiError(ApiErrorCodes.EntityIdMismatch, ApiErrorMessages.RequestEntityMismatchMessage));
                return err.ToBadRequestResult().ToHttpResult();
            }

            var response = await ((IAdminActionsApi)this).UpdateAdminAction(editAdminActionDto, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates an existing admin action.
        /// </summary>
        /// <param name="editAdminActionDto">The admin action data to update.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the admin action was updated if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IAdminActionsApi.UpdateAdminAction(EditAdminActionDto editAdminActionDto, CancellationToken cancellationToken)
        {
            var adminAction = await context.AdminActions
                .FirstOrDefaultAsync(a => a.AdminActionId == editAdminActionDto.AdminActionId, cancellationToken);

            if (adminAction == null)
                return new ApiResult(HttpStatusCode.NotFound);

            editAdminActionDto.ApplyTo(adminAction);

            // Match provided admin identifier (could be Identity OID or Forum Id) to a UserProfile
            if (!string.IsNullOrEmpty(editAdminActionDto.AdminId))
            {
                var adminId = editAdminActionDto.AdminId;
                var userProfileId = await context.UserProfiles
                    .Where(up => up.IdentityOid == adminId || up.XtremeIdiotsForumId == adminId)
                    .Select(up => up.UserProfileId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (userProfileId != Guid.Empty)
                    adminAction.UserProfileId = userProfileId;
            }

            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Deletes an admin action by its unique identifier.
        /// </summary>
        /// <param name="adminActionId">The unique identifier of the admin action to delete.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A success response if the admin action was deleted; otherwise, a 404 Not Found response.</returns>
        [HttpDelete("admin-actions/{adminActionId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAdminAction(Guid adminActionId, CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).DeleteAdminAction(adminActionId, cancellationToken);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Deletes an admin action by its unique identifier.
        /// </summary>
        /// <param name="adminActionId">The unique identifier of the admin action to delete.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the admin action was deleted if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> IAdminActionsApi.DeleteAdminAction(Guid adminActionId, CancellationToken cancellationToken)
        {
            var adminAction = await context.AdminActions
                .FirstOrDefaultAsync(a => a.AdminActionId == adminActionId, cancellationToken);

            if (adminAction == null)
                return new ApiResult(HttpStatusCode.NotFound);

            context.AdminActions.Remove(adminAction);
            await context.SaveChangesAsync(cancellationToken);
            return new ApiResponse().ToApiResult();
        }
    }
}
