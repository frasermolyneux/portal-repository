using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameServers;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces
{
    public interface IGameServersStatsApi
    {
        Task<ApiResponseDto> CreateGameServerStats(List<CreateGameServerStatDto> createGameServerStatDtos);
        Task<ApiResponseDto<GameServerStatCollectionDto>> GetGameServerStatusStats(Guid gameServerId, DateTime cutoff);
    }
}
