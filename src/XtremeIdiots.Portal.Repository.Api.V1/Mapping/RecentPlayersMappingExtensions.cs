using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RecentPlayers;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for RecentPlayer entities and DTOs.
    /// </summary>
    public static class RecentPlayersMappingExtensions
    {
        /// <summary>
        /// Maps a RecentPlayer entity to a RecentPlayerDto.
        /// NOTE: Player navigation property will be null to avoid circular dependencies.
        /// </summary>
        /// <param name="entity">The RecentPlayer entity to map from.</param>
        /// <returns>The mapped RecentPlayerDto.</returns>
        public static RecentPlayerDto ToDto(this RecentPlayer entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new RecentPlayerDto
            {
                RecentPlayerId = entity.RecentPlayerId,
                Name = entity.Name,
                IpAddress = entity.IpAddress,
                Lat = entity.Lat,
                Long = entity.Long,
                CountryCode = entity.CountryCode,
                GameType = entity.GameType.ToGameType(),
                PlayerId = entity.PlayerId,
                GameServerId = entity.GameServerId,
                Timestamp = entity.Timestamp,
                Player = expand && entity.Player != null ? entity.Player.ToDto(false) : null
            };
        }

        /// <summary>
        /// Maps a CreateRecentPlayerDto to a RecentPlayer entity.
        /// </summary>
        /// <param name="dto">The CreateRecentPlayerDto to map from.</param>
        /// <returns>The mapped RecentPlayer entity.</returns>
        public static RecentPlayer ToEntity(this CreateRecentPlayerDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new RecentPlayer
            {
                Name = dto.Name,
                IpAddress = dto.IpAddress,
                Lat = dto.Lat,
                Long = dto.Long,
                CountryCode = dto.CountryCode,
                GameType = dto.GameType.ToGameTypeInt(),
                PlayerId = dto.PlayerId,
                GameServerId = dto.GameServerId,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Applies the values from a CreateRecentPlayerDto to an existing RecentPlayer entity,
        /// preserving null-handling behavior (only updates non-null values).
        /// </summary>
        /// <param name="dto">The CreateRecentPlayerDto containing the updates.</param>
        /// <param name="entity">The existing RecentPlayer entity to update.</param>
        public static void ApplyTo(this CreateRecentPlayerDto dto, RecentPlayer entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            entity.Name = dto.Name;
            entity.GameType = dto.GameType.ToGameTypeInt();
            entity.PlayerId = dto.PlayerId;
            if (dto.IpAddress is not null) entity.IpAddress = dto.IpAddress;
            if (dto.Lat.HasValue) entity.Lat = dto.Lat.Value;
            if (dto.Long.HasValue) entity.Long = dto.Long.Value;
            if (dto.CountryCode is not null) entity.CountryCode = dto.CountryCode;
            if (dto.GameServerId.HasValue) entity.GameServerId = dto.GameServerId.Value;
            entity.Timestamp = DateTime.UtcNow;
        }
    }
}