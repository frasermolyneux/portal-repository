using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.RecentPlayers;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1
{
    public interface IRecentPlayersApi
    {
        Task<ApiResponseDto<RecentPlayersCollectionDto>> GetRecentPlayers(GameType? gameType, Guid? gameServerId, DateTime? cutoff, RecentPlayersFilter? filter, int skipEntries, int takeEntries, RecentPlayersOrder? order);

        Task<ApiResponseDto> CreateRecentPlayers(List<CreateRecentPlayerDto> createRecentPlayerDtos);
    }
}