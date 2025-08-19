using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IBanFileMonitorsApi
    {
        Task<ApiResult<BanFileMonitorDto>> GetBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default);
        Task<ApiResult<CollectionModel<BanFileMonitorDto>>> GetBanFileMonitors(GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId, int skipEntries, int takeEntries, BanFileMonitorOrder? order, CancellationToken cancellationToken = default);

        Task<ApiResult> CreateBanFileMonitor(CreateBanFileMonitorDto createBanFileMonitorDto, CancellationToken cancellationToken = default);

        Task<ApiResult> UpdateBanFileMonitor(EditBanFileMonitorDto editBanFileMonitorDto, CancellationToken cancellationToken = default);

        Task<ApiResult> DeleteBanFileMonitor(Guid banFileMonitorId, CancellationToken cancellationToken = default);
    }
}
