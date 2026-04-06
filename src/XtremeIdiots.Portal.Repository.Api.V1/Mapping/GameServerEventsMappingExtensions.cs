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
        /// Maps a GameServerEvent entity to a GameServerEventDto.
        /// </summary>
        /// <param name="entity">The GameServerEvent entity to map from.</param>
        /// <param name="expand">Whether to include related entities.</param>
        /// <returns>The mapped GameServerEventDto.</returns>
        public static GameServerEventDto ToDto(this GameServerEvent entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new GameServerEventDto
            {
                GameServerEventId = entity.GameServerEventId,
                GameServerId = entity.GameServerId,
                Timestamp = entity.Timestamp,
                EventType = entity.EventType,
                EventData = entity.EventData,
                GameServer = expand && entity.GameServer is not null ? entity.GameServer.ToDto(false) : null!
            };
        }

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