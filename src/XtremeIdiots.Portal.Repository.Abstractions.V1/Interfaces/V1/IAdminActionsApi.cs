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
        /// Returns aggregate counts of currently-active bans per game type, used by
        /// the ban file monitor dashboard. When <paramref name="gameType"/> is
        /// supplied, the collection contains exactly one entry; otherwise one entry
        /// per game type with at least one ban is returned.
        /// </summary>
        Task<ApiResult<CollectionModel<ActiveBanCountsDto>>> GetActiveBanCounts(GameType? gameType, CancellationToken cancellationToken = default);

        Task<ApiResult> CreateAdminAction(CreateAdminActionDto createAdminActionDto, CancellationToken cancellationToken = default);

        Task<ApiResult> UpdateAdminAction(EditAdminActionDto editAdminActionDto, CancellationToken cancellationToken = default);

        Task<ApiResult> DeleteAdminAction(Guid adminActionId, CancellationToken cancellationToken = default);
    }
}
