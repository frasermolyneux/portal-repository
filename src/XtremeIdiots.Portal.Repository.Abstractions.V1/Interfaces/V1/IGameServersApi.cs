using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IGameServersApi
    {
        Task<ApiResult<GameServerDto>> GetGameServer(Guid gameServerId, CancellationToken cancellationToken = default);
        Task<ApiResult<CollectionModel<GameServerDto>>> GetGameServers(GameType[]? gameTypes, Guid[]? gameServerIds, GameServerFilter? filter, int skipEntries, int takeEntries, GameServerOrder? order, CancellationToken cancellationToken = default);

        Task<ApiResult> CreateGameServer(CreateGameServerDto createGameServerDto, CancellationToken cancellationToken = default);
        Task<ApiResult> CreateGameServers(List<CreateGameServerDto> createGameServerDtos, CancellationToken cancellationToken = default);

        Task<ApiResult> UpdateGameServer(EditGameServerDto editGameServerDto, CancellationToken cancellationToken = default);

        Task<ApiResult> DeleteGameServer(Guid gameServerId, CancellationToken cancellationToken = default);
    }
}