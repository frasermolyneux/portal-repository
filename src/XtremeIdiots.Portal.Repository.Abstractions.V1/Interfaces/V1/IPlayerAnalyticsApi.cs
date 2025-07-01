using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IPlayerAnalyticsApi
    {
        Task<ApiResponseDto<PlayerAnalyticEntryCollectionDto>> GetCumulativeDailyPlayers(DateTime cutoff);
        Task<ApiResponseDto<PlayerAnalyticPerGameEntryCollectionDto>> GetNewDailyPlayersPerGame(DateTime cutoff);
        Task<ApiResponseDto<PlayerAnalyticPerGameEntryCollectionDto>> GetPlayersDropOffPerGameJson(DateTime cutoff);
    }
}
