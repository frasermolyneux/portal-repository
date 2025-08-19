using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for LivePlayer entities and DTOs.
    /// </summary>
    public static class LivePlayersMappingExtensions
    {
        /// <summary>
        /// Maps a LivePlayer entity to a LivePlayerDto.
        /// NOTE: Player navigation property will be null to avoid circular dependencies.
        /// </summary>
        /// <param name="entity">The LivePlayer entity to map from.</param>
        /// <returns>The mapped LivePlayerDto.</returns>
        public static LivePlayerDto ToDto(this LivePlayer entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new LivePlayerDto
            {
                LivePlayerId = entity.LivePlayerId,
                Name = entity.Name,
                Score = entity.Score,
                Ping = entity.Ping,
                Num = entity.Num,
                Rate = entity.Rate,
                Team = entity.Team,
                Time = entity.Time,
                IpAddress = entity.IpAddress,
                Lat = entity.Lat,
                Long = entity.Long,
                CountryCode = entity.CountryCode,
                GameType = entity.GameType.ToGameType(),
                PlayerId = entity.PlayerId,
                GameServerServerId = entity.GameServerId,
                Player = null // Set separately to avoid circular dependencies
            };
        }

        /// <summary>
        /// Maps a CreateLivePlayerDto to a LivePlayer entity.
        /// </summary>
        /// <param name="dto">The CreateLivePlayerDto to map from.</param>
        /// <returns>The mapped LivePlayer entity.</returns>
        public static LivePlayer ToEntity(this CreateLivePlayerDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new LivePlayer
            {
                PlayerId = dto.PlayerId,
                GameServerId = dto.GameServerId,
                Name = dto.Name,
                Score = dto.Score,
                Ping = dto.Ping,
                Num = dto.Num,
                Rate = dto.Rate,
                Team = dto.Team,
                Time = dto.Time,
                IpAddress = dto.IpAddress,
                Lat = dto.Lat,
                Long = dto.Long,
                CountryCode = dto.CountryCode,
                GameType = dto.GameType.ToGameTypeInt()
            };
        }
    }
}