using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.BanFileMonitors;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1
{
    public interface IBanFileMonitorsApi
    {
        Task<ApiResponseDto<BanFileMonitorDto>> GetBanFileMonitor(Guid banFileMonitorId);
        Task<ApiResponseDto<BanFileMonitorCollectionDto>> GetBanFileMonitors(GameType[]? gameTypes, Guid[]? banFileMonitorIds, Guid? gameServerId, int skipEntries, int takeEntries, BanFileMonitorOrder? order);

        Task<ApiResponseDto> CreateBanFileMonitor(CreateBanFileMonitorDto createBanFileMonitorDto);

        Task<ApiResponseDto> UpdateBanFileMonitor(EditBanFileMonitorDto editBanFileMonitorDto);

        Task<ApiResponseDto> DeleteBanFileMonitor(Guid banFileMonitorId);
    }
}
