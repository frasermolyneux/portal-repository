using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IGameServersApi
    {
        Task<ApiResponseDto<GameServerDto>> GetGameServer(Guid gameServerId);
        Task<ApiResponseDto<GameServersCollectionDto>> GetGameServers(GameType[]? gameTypes, Guid[]? gameServerIds, GameServerFilter? filter, int skipEntries, int takeEntries, GameServerOrder? order);

        Task<ApiResponseDto> CreateGameServer(CreateGameServerDto createGameServerDto);
        Task<ApiResponseDto> CreateGameServers(List<CreateGameServerDto> createGameServerDtos);

        Task<ApiResponseDto> UpdateGameServer(EditGameServerDto editGameServerDto);

        Task<ApiResponseDto> DeleteGameServer(Guid gameServerId);
    }
}