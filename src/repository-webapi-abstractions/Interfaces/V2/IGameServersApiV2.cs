using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameServers;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V2
{
    /// <summary>
    /// Version 2 of the Game Servers API interface
    /// </summary>
    public interface IGameServersApiV2
    {
        /// <summary>
        /// Get a game server by ID
        /// </summary>
        /// <param name="gameServerId">The game server ID</param>
        /// <returns>The game server details</returns>
        Task<ApiResponseDto<GameServerDto>> GetGameServer(Guid gameServerId);
    }
}
