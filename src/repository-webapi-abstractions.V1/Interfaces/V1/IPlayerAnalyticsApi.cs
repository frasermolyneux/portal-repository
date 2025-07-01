using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1
{
    public interface IPlayerAnalyticsApi
    {
        Task<ApiResponseDto<PlayerAnalyticEntryCollectionDto>> GetCumulativeDailyPlayers(DateTime cutoff);
        Task<ApiResponseDto<PlayerAnalyticPerGameEntryCollectionDto>> GetNewDailyPlayersPerGame(DateTime cutoff);
        Task<ApiResponseDto<PlayerAnalyticPerGameEntryCollectionDto>> GetPlayersDropOffPerGameJson(DateTime cutoff);
    }
}
