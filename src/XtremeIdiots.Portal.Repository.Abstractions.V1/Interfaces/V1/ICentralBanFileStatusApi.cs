using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.CentralBanFileStatus;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    /// <summary>
    /// Read/write API over the per-game-type central ban file status row.
    /// Written by portal-sync; consumed by the ban file monitor dashboard.
    /// </summary>
    public interface ICentralBanFileStatusApi
    {
        /// <summary>
        /// Returns the central ban file status for a single game type, or 404 if
        /// portal-sync has not yet written a row for it.
        /// </summary>
        Task<ApiResult<CentralBanFileStatusDto>> GetCentralBanFileStatus(GameType gameType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns central ban file status for all game types that have rows. Empty
        /// collection on first run.
        /// </summary>
        Task<ApiResult<CollectionModel<CentralBanFileStatusDto>>> GetCentralBanFileStatuses(CancellationToken cancellationToken = default);

        /// <summary>
        /// Upserts the central ban file status row for a game type. Only non-null
        /// properties on the DTO are applied — passing a partial update preserves
        /// fields not included in this call.
        /// </summary>
        Task<ApiResult<CentralBanFileStatusDto>> UpsertCentralBanFileStatus(UpsertCentralBanFileStatusDto upsertDto, CancellationToken cancellationToken = default);
    }
}
