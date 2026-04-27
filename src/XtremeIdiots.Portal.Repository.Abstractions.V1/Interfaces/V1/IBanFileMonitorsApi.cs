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
        /// after each check cycle. This is the only supported way to write monitor status.
        /// </summary>
        Task<ApiResult<BanFileMonitorDto>> UpsertBanFileMonitorStatus(UpsertBanFileMonitorStatusDto upsertDto, CancellationToken cancellationToken = default);
    }
}


