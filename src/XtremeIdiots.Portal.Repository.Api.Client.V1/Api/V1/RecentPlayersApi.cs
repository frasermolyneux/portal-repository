using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RecentPlayers;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class RecentPlayersApi : BaseApi, IRecentPlayersApi
    {
        public RecentPlayersApi(ILogger<RecentPlayersApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<RecentPlayersCollectionDto>> GetRecentPlayers(GameType? gameType, Guid? gameServerId, DateTime? cutoff, RecentPlayersFilter? filter, int skipEntries, int takeEntries, RecentPlayersOrder? order)
        {
            var request = await CreateRequestAsync("v1/recent-players", Method.Get);

            if (gameType.HasValue)
                request.AddQueryParameter("gameType", gameType.ToString());

            if (gameServerId.HasValue)
                request.AddQueryParameter("gameServerId", gameServerId.ToString());

            if (cutoff.HasValue)
                request.AddQueryParameter("cutoff", cutoff.Value.ToString("MM/dd/yyyy HH:mm:ss"));

            if (filter.HasValue)
                request.AddQueryParameter("filter", filter.ToString());

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<RecentPlayersCollectionDto>();
        }

        public async Task<ApiResponseDto> CreateRecentPlayers(List<CreateRecentPlayerDto> createRecentPlayerDtos)
        {
            var request = await CreateRequestAsync("v1/recent-players", Method.Post);
            request.AddJsonBody(createRecentPlayerDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
