using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Data;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.DataLib;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using Asp.Versioning;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("v{version:apiVersion}")]
    public class AdminActionsController : ControllerBase, IAdminActionsApi
    {
        private readonly PortalDbContext context;

        public AdminActionsController(PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
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
            var response = await ((IAdminActionsApi)this).GetAdminAction(adminActionId, cancellationToken).ConfigureAwait(false);
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
                .FirstOrDefaultAsync(a => a.AdminActionId == adminActionId, cancellationToken).ConfigureAwait(false);

            if (adminAction == null)
            {
                return new ApiResult<AdminActionDto>(HttpStatusCode.NotFound);
            }

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
            CancellationToken cancellationToken = default,
            [FromQuery] ActionSource? source = null,
            [FromQuery] AutomationFeature? automationFeature = null,
            [FromQuery] string? automationRuleId = null)
        {
            var response = await ((IAdminActionsApi)this).GetAdminActions(gameType, playerId, adminId, filter, skipEntries, takeEntries, order, source, automationFeature, automationRuleId, cancellationToken).ConfigureAwait(false);
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
        Task<ApiResult<CollectionModel<AdminActionDto>>> IAdminActionsApi.GetAdminActions(
            GameType? gameType,
            Guid? playerId,
            string? adminId,
            AdminActionFilter? filter,
            int skipEntries,
            int takeEntries,
            AdminActionOrder? order,
            CancellationToken cancellationToken)
            => ((IAdminActionsApi)this).GetAdminActions(gameType, playerId, adminId, filter, skipEntries, takeEntries, order, null, null, null, cancellationToken);

        async Task<ApiResult<CollectionModel<AdminActionDto>>> IAdminActionsApi.GetAdminActions(
            GameType? gameType,
            Guid? playerId,
            string? adminId,
            AdminActionFilter? filter,
            int skipEntries,
            int takeEntries,
            AdminActionOrder? order,
            ActionSource? source,
            AutomationFeature? automationFeature,
            string? automationRuleId,
            CancellationToken cancellationToken)
        {
            // Use lightweight query without includes for counting
            var countQuery = context.AdminActions.AsNoTracking().AsQueryable();
            var totalCount = await countQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            var filteredCountQuery = ApplyFilters(countQuery, gameType, playerId, adminId, filter, source, automationFeature, automationRuleId);
            var filteredCount = await filteredCountQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            // Build data query with includes only for the paginated results
            var dataQuery = context.AdminActions
                .Include(a => a.Player)
                .Include(a => a.UserProfile)
                .AsNoTracking()
                .AsQueryable();

            var filteredDataQuery = ApplyFilters(dataQuery, gameType, playerId, adminId, filter, source, automationFeature, automationRuleId);
            var orderedQuery = ApplyOrderingAndPagination(filteredDataQuery, skipEntries, takeEntries, order);
            var adminActions = await orderedQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = adminActions.Select(aa => aa.ToDto()).ToList();

            var data = new CollectionModel<AdminActionDto>(entries);

            return new ApiResponse<CollectionModel<AdminActionDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        private IQueryable<AdminAction> ApplyFilters(IQueryable<AdminAction> query, GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, ActionSource? source, AutomationFeature? automationFeature, string? automationRuleId)
        {
            if (gameType.HasValue)
            {
                query = query.Where(a => a.Player.GameType == (int)gameType.Value);
            }

            if (playerId.HasValue)
            {
                query = query.Where(a => a.PlayerId == playerId.Value);
            }

            if (!string.IsNullOrEmpty(adminId))
            {
                query = query.Where(a => a.UserProfile != null &&
                    (a.UserProfile.IdentityOid == adminId ||
                     a.UserProfile.XtremeIdiotsForumId == adminId ||
                     a.UserProfile.DemoAuthKey == adminId));
            }

            if (source.HasValue)
            {
                query = query.Where(a => a.Source == (byte)source.Value);
            }

            if (automationFeature.HasValue)
            {
                query = query.Where(a => a.AutomationFeature == (int)automationFeature.Value);
            }

            if (!string.IsNullOrWhiteSpace(automationRuleId))
            {
                query = query.Where(a => a.AutomationRuleId == automationRuleId);
            }

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
                    AdminActionFilter.UnclaimedActions => query.Where(a => a.UserProfile == null),
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
        /// Returns aggregate counts of currently-active bans per game type, used by
        /// the ban file monitor dashboard. When <paramref name="gameType"/> is
        /// supplied, the collection contains exactly one entry; otherwise one entry
        /// per game type with at least one ban is returned.
        /// </summary>
        [HttpGet("admin-actions/active-ban-counts")]
        [ProducesResponseType<CollectionModel<ActiveBanCountsDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveBanCounts([FromQuery] GameType? gameType = null, CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).GetActiveBanCounts(gameType, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<ActiveBanCountsDto>>> IAdminActionsApi.GetActiveBanCounts(GameType? gameType, CancellationToken cancellationToken)
        {
            var nowUtc = DateTime.UtcNow;
            var soonUtc = nowUtc.AddHours(24);

            var banQuery = context.AdminActions.AsNoTracking()
                .Where(a => a.Type == (int)AdminActionType.Ban || a.Type == (int)AdminActionType.TempBan);

            if (gameType.HasValue)
            {
                var gtInt = (int)gameType.Value;
                banQuery = banQuery.Where(a => a.Player.GameType == gtInt);
            }

            // Aggregate per-game-type via a single grouped query.
            var grouped = await banQuery
                .Where(a => a.Type == (int)AdminActionType.Ban
                    ? (a.Expires == null || a.Expires > nowUtc)
                    : (a.Expires != null && a.Expires > nowUtc))
                .GroupBy(a => a.Player.GameType)
                .Select(g => new
                {
                    GameTypeInt = g.Key,
                    ActivePermanentBanCount = g.Count(a => a.Type == (int)AdminActionType.Ban),
                    ActiveTempBanCount = g.Count(a => a.Type == (int)AdminActionType.TempBan),
                    ExpiringTempBansNext24h = g.Count(a => a.Type == (int)AdminActionType.TempBan && a.Expires != null && a.Expires <= soonUtc)
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var items = grouped
                .Select(g => new ActiveBanCountsDto
                {
                    GameType = (GameType)g.GameTypeInt,
                    ActivePermanentBanCount = g.ActivePermanentBanCount,
                    ActiveTempBanCount = g.ActiveTempBanCount,
                    ExpiringTempBansNext24h = g.ExpiringTempBansNext24h
                })
                .OrderBy(c => c.GameType)
                .ToList();

            // When a specific game type was requested but has zero bans, still return
            // an entry so the caller can render a "0 bans" card without a 404.
            if (gameType.HasValue && items.Count == 0)
            {
                items.Add(new ActiveBanCountsDto
                {
                    GameType = gameType.Value,
                    ActivePermanentBanCount = 0,
                    ActiveTempBanCount = 0,
                    ExpiringTempBansNext24h = 0
                });
            }

            var data = new CollectionModel<ActiveBanCountsDto>(items);
            return new ApiResponse<CollectionModel<ActiveBanCountsDto>>(data)
            {
                Pagination = new ApiPagination(items.Count, items.Count, 0, items.Count)
            }.ToApiResult();
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
            var response = await ((IAdminActionsApi)this).CreateAdminAction(createAdminActionDto, cancellationToken).ConfigureAwait(false);
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
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

                if (userProfileId != Guid.Empty)
                {
                    adminAction.UserProfileId = userProfileId;
                }
            }
            context.AdminActions.Add(adminAction);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Creates an action for an automation rule only when an equal or stronger action does not already exist.
        /// </summary>
        /// <param name="ensureAutomatedActionDto">The automation action to ensure.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The created or existing action.</returns>
        [HttpPost("admin-actions/ensure-automated")]
        [ProducesResponseType<EnsureAutomatedActionResultDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<EnsureAutomatedActionResultDto>(StatusCodes.Status201Created)]
        public async Task<IActionResult> EnsureAutomatedAction([FromBody] EnsureAutomatedActionDto ensureAutomatedActionDto, CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).EnsureAutomatedAction(ensureAutomatedActionDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        async Task<ApiResult<EnsureAutomatedActionResultDto>> IAdminActionsApi.EnsureAutomatedAction(EnsureAutomatedActionDto ensureAutomatedActionDto, CancellationToken cancellationToken)
        {
            if (!context.Database.IsRelational())
            {
                return await EnsureAutomatedActionCoreAsync(ensureAutomatedActionDto, null, cancellationToken).ConfigureAwait(false);
            }

            for (var attempt = 0; attempt < 2; attempt++)
            {
                await using var transaction = await context.Database
                    .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                    .ConfigureAwait(false);

                try
                {
                    var state = await GetOrCreateAutomationActionStateAsync(ensureAutomatedActionDto, cancellationToken).ConfigureAwait(false);
                    var result = await EnsureAutomatedActionCoreAsync(ensureAutomatedActionDto, state, cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return result;
                }
                catch (DbUpdateException) when (attempt == 0)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    context.ChangeTracker.Clear();
                }
            }

            throw new InvalidOperationException("Unable to serialize the automated action decision.");
        }

        /// <summary>
        /// Claims the right to create a forum topic for an admin action exactly once.
        /// </summary>
        [HttpPost("admin-actions/{adminActionId:guid}/forum-topic-publication/claim")]
        [ProducesResponseType<ForumTopicPublicationClaimResultDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ClaimForumTopicPublication(Guid adminActionId, CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).ClaimForumTopicPublication(adminActionId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        async Task<ApiResult<ForumTopicPublicationClaimResultDto>> IAdminActionsApi.ClaimForumTopicPublication(Guid adminActionId, CancellationToken cancellationToken)
        {
            if (!context.Database.IsRelational())
            {
                return await ClaimForumTopicPublicationCoreAsync(adminActionId, cancellationToken).ConfigureAwait(false);
            }

            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
            var response = await ClaimForumTopicPublicationCoreAsync(adminActionId, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }

        /// <summary>
        /// Completes a claimed forum-topic publication and stores the resulting topic identifier.
        /// </summary>
        [HttpPost("admin-actions/{adminActionId:guid}/forum-topic-publication/complete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CompleteForumTopicPublication(Guid adminActionId, [FromBody] CompleteForumTopicPublicationDto dto, CancellationToken cancellationToken = default)
        {
            var response = await ((IAdminActionsApi)this).CompleteForumTopicPublication(adminActionId, dto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        async Task<ApiResult> IAdminActionsApi.CompleteForumTopicPublication(Guid adminActionId, CompleteForumTopicPublicationDto dto, CancellationToken cancellationToken)
        {
            if (!context.Database.IsRelational())
            {
                return await CompleteForumTopicPublicationCoreAsync(adminActionId, dto, cancellationToken).ConfigureAwait(false);
            }

            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
            var response = await CompleteForumTopicPublicationCoreAsync(adminActionId, dto, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }

        private async Task<ApiResult<ForumTopicPublicationClaimResultDto>> ClaimForumTopicPublicationCoreAsync(Guid adminActionId, CancellationToken cancellationToken)
        {
            var action = await context.AdminActions
                .SingleOrDefaultAsync(existing => existing.AdminActionId == adminActionId, cancellationToken)
                .ConfigureAwait(false);

            if (action is null)
            {
                return new ApiResult<ForumTopicPublicationClaimResultDto>(HttpStatusCode.NotFound);
            }

            if (action.ForumTopicId.HasValue)
            {
                return ToForumTopicPublicationClaimResult(action, null, requiresManualRecovery: false);
            }

            if (action.ForumTopicPublicationClaimId.HasValue)
            {
                return ToForumTopicPublicationClaimResult(action, null, requiresManualRecovery: true);
            }

            var claimId = Guid.NewGuid();
            action.ForumTopicPublicationClaimId = claimId;
            action.ForumTopicPublicationClaimedUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ToForumTopicPublicationClaimResult(action, claimId, requiresManualRecovery: false);
        }

        private async Task<ApiResult> CompleteForumTopicPublicationCoreAsync(Guid adminActionId, CompleteForumTopicPublicationDto dto, CancellationToken cancellationToken)
        {
            var action = await context.AdminActions
                .SingleOrDefaultAsync(existing => existing.AdminActionId == adminActionId, cancellationToken)
                .ConfigureAwait(false);

            if (action is null)
            {
                return new ApiResult(HttpStatusCode.NotFound);
            }

            if (action.ForumTopicId == dto.ForumTopicId)
            {
                return new ApiResponse().ToApiResult();
            }

            if (action.ForumTopicId.HasValue || action.ForumTopicPublicationClaimId != dto.ClaimId)
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.EntityConflict, "The forum topic publication claim is not valid for this action."))
                    .ToApiResult(HttpStatusCode.Conflict);
            }

            action.ForumTopicId = dto.ForumTopicId;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult();
        }

        private static ApiResult<ForumTopicPublicationClaimResultDto> ToForumTopicPublicationClaimResult(
            AdminAction action,
            Guid? claimId,
            bool requiresManualRecovery)
        {
            var result = new ForumTopicPublicationClaimResultDto
            {
                AdminActionId = action.AdminActionId,
                ForumTopicId = action.ForumTopicId,
                ClaimId = claimId,
                RequiresManualRecovery = requiresManualRecovery
            };

            return new ApiResponse<ForumTopicPublicationClaimResultDto>(result).ToApiResult();
        }

        private async Task<ApiResult<EnsureAutomatedActionResultDto>> EnsureAutomatedActionCoreAsync(
            EnsureAutomatedActionDto ensureAutomatedActionDto,
            AutomationActionState? state,
            CancellationToken cancellationToken)
        {
            var nowUtc = DateTime.UtcNow;
            var existingActions = await GetAutomationActionsAsync(ensureAutomatedActionDto, cancellationToken).ConfigureAwait(false);

            if (DeactivateExpiredAutomationBans(existingActions, nowUtc))
            {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            var existingAction = FindEqualOrStrongerAction(existingActions, ensureAutomatedActionDto.Type, nowUtc);
            if (existingAction is not null)
            {
                if (state is not null)
                {
                    state.LastUpdatedUtc = nowUtc;
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return ToEnsureResult(existingAction, created: false, HttpStatusCode.OK);
            }

            DeactivateLowerActiveBans(existingActions, ensureAutomatedActionDto.Type, nowUtc);

            var adminAction = new AdminAction
            {
                PlayerId = ensureAutomatedActionDto.PlayerId,
                Type = ensureAutomatedActionDto.Type.ToAdminActionTypeInt(),
                Text = ensureAutomatedActionDto.Text,
                Expires = ensureAutomatedActionDto.Expires,
                Created = nowUtc,
                Source = (byte)ActionSource.Automation,
                AutomationFeature = (int)ensureAutomatedActionDto.AutomationFeature,
                AutomationRuleId = ensureAutomatedActionDto.AutomationRuleId,
                AutomationIsActive = IsBan(ensureAutomatedActionDto.Type)
            };

            if (!string.IsNullOrEmpty(ensureAutomatedActionDto.AdminId))
            {
                adminAction.UserProfileId = await ResolveUserProfileIdAsync(ensureAutomatedActionDto.AdminId, cancellationToken).ConfigureAwait(false);
            }

            context.AdminActions.Add(adminAction);

            state?.LastUpdatedUtc = nowUtc;

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ToEnsureResult(adminAction, created: true, HttpStatusCode.Created);
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

            var response = await ((IAdminActionsApi)this).UpdateAdminAction(editAdminActionDto, cancellationToken).ConfigureAwait(false);
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
                .FirstOrDefaultAsync(a => a.AdminActionId == editAdminActionDto.AdminActionId, cancellationToken).ConfigureAwait(false);

            if (adminAction == null)
            {
                return new ApiResult(HttpStatusCode.NotFound);
            }

            if (editAdminActionDto.ForumTopicId.HasValue &&
                adminAction.Source == (byte)ActionSource.Automation &&
                adminAction.AutomationFeature == (int)AutomationFeature.RconBanImport)
            {
                return new ApiResponse(new ApiError(
                    ApiErrorCodes.EntityConflict,
                    "RCON-import forum topics must be linked through the publication claim workflow."))
                    .ToApiResult(HttpStatusCode.Conflict);
            }

            editAdminActionDto.ApplyTo(adminAction);

            if (adminAction.Source == (byte)ActionSource.Automation
                && IsBan(adminAction.Type.ToAdminActionType())
                && adminAction.Expires <= DateTime.UtcNow)
            {
                adminAction.AutomationIsActive = false;
            }

            // Match provided admin identifier (could be Identity OID or Forum Id) to a UserProfile
            if (!string.IsNullOrEmpty(editAdminActionDto.AdminId))
            {
                var adminId = editAdminActionDto.AdminId;
                var userProfileId = await context.UserProfiles
                    .Where(up => up.IdentityOid == adminId || up.XtremeIdiotsForumId == adminId)
                    .Select(up => up.UserProfileId)
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

                if (userProfileId != Guid.Empty)
                {
                    adminAction.UserProfileId = userProfileId;
                }
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult();
        }

        private async Task<List<AdminAction>> GetAutomationActionsAsync(EnsureAutomatedActionDto dto, CancellationToken cancellationToken)
        {
            return await context.AdminActions
                .Where(action => action.PlayerId == dto.PlayerId
                    && action.Source == (byte)ActionSource.Automation
                    && action.AutomationFeature == (int)dto.AutomationFeature
                    && action.AutomationRuleId == dto.AutomationRuleId)
                .OrderByDescending(action => action.Type)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<AutomationActionState> GetOrCreateAutomationActionStateAsync(EnsureAutomatedActionDto dto, CancellationToken cancellationToken)
        {
            var state = await context.AutomationActionStates
                .SingleOrDefaultAsync(existing => existing.PlayerId == dto.PlayerId
                    && existing.AutomationFeature == (int)dto.AutomationFeature
                    && existing.AutomationRuleId == dto.AutomationRuleId, cancellationToken)
                .ConfigureAwait(false);

            if (state is not null)
            {
                return state;
            }

            state = new AutomationActionState
            {
                PlayerId = dto.PlayerId,
                AutomationFeature = (int)dto.AutomationFeature,
                AutomationRuleId = dto.AutomationRuleId,
                LastUpdatedUtc = DateTime.UtcNow
            };
            context.AutomationActionStates.Add(state);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return state;
        }

        private static bool DeactivateExpiredAutomationBans(IEnumerable<AdminAction> actions, DateTime nowUtc)
        {
            var changed = false;

            foreach (var action in actions.Where(action => action.AutomationIsActive
                && IsBan(action.Type.ToAdminActionType())
                && action.Expires.HasValue
                && action.Expires <= nowUtc))
            {
                action.AutomationIsActive = false;
                changed = true;
            }

            return changed;
        }

        private static AdminAction? FindEqualOrStrongerAction(IEnumerable<AdminAction> actions, AdminActionType requestedType, DateTime nowUtc)
        {
            return actions
                .Where(action => IsActionRelevant(action, nowUtc))
                .FirstOrDefault(action => GetSeverity(action.Type.ToAdminActionType()) >= GetSeverity(requestedType));
        }

        private static void DeactivateLowerActiveBans(IEnumerable<AdminAction> actions, AdminActionType requestedType, DateTime nowUtc)
        {
            if (!IsBan(requestedType))
            {
                return;
            }

            foreach (var action in actions.Where(action => action.AutomationIsActive
                && IsBan(action.Type.ToAdminActionType())
                && GetSeverity(action.Type.ToAdminActionType()) < GetSeverity(requestedType)))
            {
                action.AutomationIsActive = false;
                action.Expires = nowUtc;
            }
        }

        private static bool IsActionRelevant(AdminAction action, DateTime nowUtc)
        {
            if (!IsBan(action.Type.ToAdminActionType()))
            {
                return true;
            }

            return action.AutomationIsActive
                && (!action.Expires.HasValue || action.Expires > nowUtc);
        }

        private static bool IsBan(AdminActionType type) => type is AdminActionType.Ban or AdminActionType.TempBan;

        private static int GetSeverity(AdminActionType type) => type switch
        {
            AdminActionType.Observation => 0,
            AdminActionType.Warning => 1,
            AdminActionType.Kick => 2,
            AdminActionType.TempBan => 3,
            AdminActionType.Ban => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported admin action type.")
        };

        private static ApiResult<EnsureAutomatedActionResultDto> ToEnsureResult(AdminAction action, bool created, HttpStatusCode statusCode)
        {
            var result = new EnsureAutomatedActionResultDto
            {
                Created = created,
                AdminAction = action.ToDto(expand: false)
            };

            return new ApiResponse<EnsureAutomatedActionResultDto>(result).ToApiResult(statusCode);
        }

        private async Task<Guid?> ResolveUserProfileIdAsync(string adminId, CancellationToken cancellationToken)
        {
            var userProfileId = await context.UserProfiles
                .Where(profile => profile.IdentityOid == adminId || profile.XtremeIdiotsForumId == adminId)
                .Select(profile => (Guid?)profile.UserProfileId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return userProfileId;
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
            var response = await ((IAdminActionsApi)this).DeleteAdminAction(adminActionId, cancellationToken).ConfigureAwait(false);
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
                .FirstOrDefaultAsync(a => a.AdminActionId == adminActionId, cancellationToken).ConfigureAwait(false);

            if (adminAction == null)
            {
                return new ApiResult(HttpStatusCode.NotFound);
            }

            context.AdminActions.Remove(adminAction);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult();
        }
    }
}
