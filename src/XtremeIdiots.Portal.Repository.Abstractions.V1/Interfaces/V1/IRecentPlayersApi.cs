using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RecentPlayers;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IRecentPlayersApi
    {
        Task<ApiResponseDto<RecentPlayersCollectionDto>> GetRecentPlayers(GameType? gameType, Guid? gameServerId, DateTime? cutoff, RecentPlayersFilter? filter, int skipEntries, int takeEntries, RecentPlayersOrder? order);

        Task<ApiResponseDto> CreateRecentPlayers(List<CreateRecentPlayerDto> createRecentPlayerDtos);
    }
}