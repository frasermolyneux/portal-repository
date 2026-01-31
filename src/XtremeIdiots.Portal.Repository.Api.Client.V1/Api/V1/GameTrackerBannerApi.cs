using Microsoft.Extensions.Logging;



using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameTracker;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class GameTrackerBannerApi : BaseApi<RepositoryApiClientOptions>, IGameTrackerBannerApi
    {
        public GameTrackerBannerApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<GameTrackerBannerDto>> GetGameTrackerBanner(string ipAddress, string queryPort, string imageName, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/gametracker/{ipAddress}:{queryPort}/{imageName}", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<GameTrackerBannerDto>();
        }
    }
}


