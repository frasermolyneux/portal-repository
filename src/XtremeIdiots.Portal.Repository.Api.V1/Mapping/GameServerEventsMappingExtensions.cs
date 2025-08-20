using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for GameServerEvent entities and DTOs.
    /// </summary>
    public static class GameServerEventsMappingExtensions
    {
        /// <summary>
        /// Maps a CreateGameServerEventDto to a GameServerEvent entity.
        /// </summary>
        /// <param name="dto">The CreateGameServerEventDto to map from.</param>
        /// <returns>The mapped GameServerEvent entity.</returns>
        public static GameServerEvent ToEntity(this CreateGameServerEventDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new GameServerEvent
            {
                GameServerId = dto.GameServerId,
                EventType = dto.EventType,
                EventData = dto.EventData,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}