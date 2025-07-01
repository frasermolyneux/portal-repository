using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.GameTracker;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1
{
    public interface IGameTrackerBannerApi
    {
        Task<ApiResponseDto<GameTrackerBannerDto>> GetGameTrackerBanner(string ipAddress, string queryPort, string imageName);
    }
}
