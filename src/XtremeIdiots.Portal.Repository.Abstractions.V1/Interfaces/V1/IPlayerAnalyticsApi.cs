using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IPlayerAnalyticsApi
    {
        Task<ApiResult<CollectionModel<PlayerAnalyticEntryDto>>> GetCumulativeDailyPlayers(DateTime cutoff, CancellationToken cancellationToken = default);
        Task<ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>> GetNewDailyPlayersPerGame(DateTime cutoff, CancellationToken cancellationToken = default);
        Task<ApiResult<CollectionModel<PlayerAnalyticPerGameEntryDto>>> GetPlayersDropOffPerGameJson(DateTime cutoff, CancellationToken cancellationToken = default);
    }
}
