using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameTracker;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces
{
    public interface IGameTrackerBannerApi
    {
        Task<ApiResponseDto<GameTrackerBannerDto>> GetGameTrackerBanner(string ipAddress, string queryPort, string imageName);
    }
}
