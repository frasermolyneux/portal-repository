using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IGameServersStatsApi
    {
        Task<ApiResult> CreateGameServerStats(List<CreateGameServerStatDto> createGameServerStatDtos, CancellationToken cancellationToken = default);
        Task<ApiResult<CollectionModel<GameServerStatDto>>> GetGameServerStatusStats(Guid gameServerId, DateTime cutoff, CancellationToken cancellationToken = default);
    }
}
