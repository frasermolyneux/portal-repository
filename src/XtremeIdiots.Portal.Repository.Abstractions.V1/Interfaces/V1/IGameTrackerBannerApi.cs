using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameTracker;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IGameTrackerBannerApi
    {
        Task<ApiResponseDto<GameTrackerBannerDto>> GetGameTrackerBanner(string ipAddress, string queryPort, string imageName);
    }
}
