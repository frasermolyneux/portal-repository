using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1
{
    public interface IGameServersStatsApi
    {
        Task<ApiResponseDto> CreateGameServerStats(List<CreateGameServerStatDto> createGameServerStatDtos);
        Task<ApiResponseDto<GameServerStatCollectionDto>> GetGameServerStatusStats(Guid gameServerId, DateTime cutoff);
    }
}
