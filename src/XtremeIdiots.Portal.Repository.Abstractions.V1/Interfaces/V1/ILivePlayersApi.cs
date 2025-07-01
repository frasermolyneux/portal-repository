using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface ILivePlayersApi
    {
        Task<ApiResponseDto<LivePlayersCollectionDto>> GetLivePlayers(GameType? gameType, Guid? gameServerId, LivePlayerFilter? filter, int skipEntries, int takeEntries, LivePlayersOrder? order);

        Task<ApiResponseDto> SetLivePlayersForGameServer(Guid gameServerId, List<CreateLivePlayerDto> createLivePlayerDtos);
    }
}
