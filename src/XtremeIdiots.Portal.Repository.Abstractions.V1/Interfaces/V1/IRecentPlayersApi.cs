using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RecentPlayers;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IRecentPlayersApi
    {
        Task<ApiResult<CollectionModel<RecentPlayerDto>>> GetRecentPlayers(GameType? gameType, Guid? gameServerId, DateTime? cutoff, RecentPlayersFilter? filter, int skipEntries, int takeEntries, RecentPlayersOrder? order, CancellationToken cancellationToken = default);

        Task<ApiResult> CreateRecentPlayers(List<CreateRecentPlayerDto> createRecentPlayerDtos, CancellationToken cancellationToken = default);
    }
}