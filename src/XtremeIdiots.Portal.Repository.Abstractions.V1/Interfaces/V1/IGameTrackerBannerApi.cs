using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameTracker;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IGameTrackerBannerApi
    {
        Task<ApiResult<GameTrackerBannerDto>> GetGameTrackerBanner(string ipAddress, string queryPort, string imageName, CancellationToken cancellationToken = default);
    }
}
