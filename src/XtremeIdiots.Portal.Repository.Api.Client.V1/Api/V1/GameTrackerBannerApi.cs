using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameTracker;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class GameTrackerBannerApi : BaseApi, IGameTrackerBannerApi
    {
        public GameTrackerBannerApi(ILogger<GameTrackerBannerApi> logger, IApiTokenProvider apiTokenProvider, IRestClientSingleton restClientSingleton, IOptions<RepositoryApiClientOptions> options) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<GameTrackerBannerDto>> GetGameTrackerBanner(string ipAddress, string queryPort, string imageName)
        {
            var request = await CreateRequestAsync($"v1/gametracker/{ipAddress}:{queryPort}/{imageName}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<GameTrackerBannerDto>();
        }
    }
}
