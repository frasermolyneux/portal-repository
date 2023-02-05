using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameTracker;

namespace XtremeIdiots.Portal.RepositoryApiClient.Api
{
    public class GameTrackerBannerApi : BaseApi, IGameTrackerBannerApi
    {
        public GameTrackerBannerApi(ILogger<GameTrackerBannerApi> logger, IApiTokenProvider apiTokenProvider, IRestClientSingleton restClientSingleton, IOptions<RepositoryApiClientOptions> options) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<GameTrackerBannerDto>> GetGameTrackerBanner(string ipAddress, string queryPort, string imageName)
        {
            var request = await CreateRequest($"gametracker/{ipAddress}:{queryPort}/{imageName}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<GameTrackerBannerDto>();
        }
    }
}
