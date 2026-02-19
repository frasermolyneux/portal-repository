using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameTracker;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeGameTrackerBannerApi : IGameTrackerBannerApi
{
    private readonly ConcurrentDictionary<string, GameTrackerBannerDto> _banners = new(StringComparer.OrdinalIgnoreCase);

    public FakeGameTrackerBannerApi AddBanner(string key, GameTrackerBannerDto banner) { _banners[key] = banner; return this; }
    public FakeGameTrackerBannerApi Reset() { _banners.Clear(); return this; }

    public Task<ApiResult<GameTrackerBannerDto>> GetGameTrackerBanner(string ipAddress, string queryPort, string imageName, CancellationToken cancellationToken = default)
    {
        var key = $"{ipAddress}:{queryPort}:{imageName}";
        if (_banners.TryGetValue(key, out var banner))
            return Task.FromResult(new ApiResult<GameTrackerBannerDto>(HttpStatusCode.OK, new ApiResponse<GameTrackerBannerDto>(banner)));
        var defaultBanner = new GameTrackerBannerDto();
        return Task.FromResult(new ApiResult<GameTrackerBannerDto>(HttpStatusCode.OK, new ApiResponse<GameTrackerBannerDto>(defaultBanner)));
    }
}
