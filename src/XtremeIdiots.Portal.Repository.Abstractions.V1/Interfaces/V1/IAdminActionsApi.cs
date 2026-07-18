using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IAdminActionsApi
    {
        Task<ApiResult<AdminActionDto>> GetAdminAction(Guid adminActionId, CancellationToken cancellationToken = default);
        Task<ApiResult<CollectionModel<AdminActionDto>>> GetAdminActions(GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, int skipEntries, int takeEntries, AdminActionOrder? order, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a paginated list of admin actions with automation provenance filters.
        /// </summary>
        Task<ApiResult<CollectionModel<AdminActionDto>>> GetAdminActions(GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, int skipEntries, int takeEntries, AdminActionOrder? order, ActionSource? source, AutomationFeature? automationFeature, string? automationRuleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns aggregate counts of currently-active bans per game type, used by
        /// the ban file monitor dashboard. When <paramref name="gameType"/> is
        /// supplied, the collection contains exactly one entry; otherwise one entry
        /// per game type with at least one ban is returned.
        /// </summary>
        Task<ApiResult<CollectionModel<ActiveBanCountsDto>>> GetActiveBanCounts(GameType? gameType, CancellationToken cancellationToken = default);

        Task<ApiResult> CreateAdminAction(CreateAdminActionDto createAdminActionDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an automated action only when an equal or stronger action for the automation rule does not already exist.
        /// </summary>
        Task<ApiResult<EnsureAutomatedActionResultDto>> EnsureAutomatedAction(EnsureAutomatedActionDto ensureAutomatedActionDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically claims the right to create a forum topic for an action. A claim is never automatically replayed after an ambiguous external failure.
        /// </summary>
        Task<ApiResult<ForumTopicPublicationClaimResultDto>> ClaimForumTopicPublication(Guid adminActionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Links a forum topic to an action after the caller successfully completes its publication claim.
        /// </summary>
        Task<ApiResult> CompleteForumTopicPublication(Guid adminActionId, CompleteForumTopicPublicationDto dto, CancellationToken cancellationToken = default);

        Task<ApiResult> UpdateAdminAction(EditAdminActionDto editAdminActionDto, CancellationToken cancellationToken = default);

        Task<ApiResult> DeleteAdminAction(Guid adminActionId, CancellationToken cancellationToken = default);
    }
}
