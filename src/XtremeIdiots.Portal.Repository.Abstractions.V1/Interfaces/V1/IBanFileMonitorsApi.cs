using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IBanFileMonitorsApi
    {
        Task<ApiResult<BanFileMonitorDto>> GetBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default);
        Task<ApiResult<CollectionModel<BanFileMonitorDto>>> GetBanFileMonitors(GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId, int skipEntries, int takeEntries, BanFileMonitorOrder? order, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upserts a ban file monitor status snapshot for a game server. If no row
        /// exists for the game server, one is created. Called by portal-server-agent
        /// after each check cycle. This is the supported way to write monitor status.
        /// </summary>
        Task<ApiResult<BanFileMonitorDto>> UpsertBanFileMonitorStatus(UpsertBanFileMonitorStatusDto upsertDto, CancellationToken cancellationToken = default);

        [Obsolete("Ban file monitors are no longer manually created — they are provisioned automatically by the agent when GameServer.BanFileSyncEnabled is true. This endpoint will be removed in a future release.")]
        Task<ApiResult> CreateBanFileMonitor(CreateBanFileMonitorDto createBanFileMonitorDto, CancellationToken cancellationToken = default);

        [Obsolete("Use UpsertBanFileMonitorStatus instead. Manual FilePath edits are no longer supported — the agent resolves the path from GameServer.BanFileRootPath plus the live mod. This endpoint will be removed in a future release.")]
        Task<ApiResult> UpdateBanFileMonitor(EditBanFileMonitorDto editBanFileMonitorDto, CancellationToken cancellationToken = default);

        [Obsolete("Ban file monitors are no longer manually deletable — they follow the lifecycle of GameServer.BanFileSyncEnabled. This endpoint will be removed in a future release.")]
        Task<ApiResult> DeleteBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default);
    }
}

