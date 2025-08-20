using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapPacks;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for MapPack entities and DTOs.
    /// </summary>
    public static class MapPacksMappingExtensions
    {
        /// <summary>
        /// Maps a MapPack entity to a MapPackDto.
        /// </summary>
        /// <param name="entity">The MapPack entity to map from.</param>
        /// <returns>The mapped MapPackDto.</returns>
        public static MapPackDto ToDto(this MapPack entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new MapPackDto(
                entity.MapPackId,
                entity.GameServerId ?? Guid.Empty,
                entity.Title ?? string.Empty,
                entity.Description ?? string.Empty,
                entity.GameMode ?? string.Empty,
                entity.SyncToGameServer,
                entity.SyncCompleted,
                entity.Deleted,
                entity.MapPackMaps?.Select(mpm => mpm.ToDto()).ToList() ?? new List<MapPackMapDto>()
            );
        }

        /// <summary>
        /// Maps a CreateMapPackDto to a MapPack entity.
        /// </summary>
        /// <param name="dto">The CreateMapPackDto to map from.</param>
        /// <returns>The mapped MapPack entity.</returns>
        public static MapPack ToEntity(this CreateMapPackDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new MapPack
            {
                GameServerId = dto.GameServerId,
                Title = dto.Title,
                Description = dto.Description,
                GameMode = dto.GameMode,
                SyncToGameServer = dto.SyncToGameServer,
                SyncCompleted = false,
                Deleted = false
            };
        }

        /// <summary>
        /// Applies the values from an UpdateMapPackDto to an existing MapPack entity,
        /// preserving null-handling behavior (only updates non-null values).
        /// </summary>
        /// <param name="dto">The UpdateMapPackDto containing the updates.</param>
        /// <param name="entity">The existing MapPack entity to update.</param>
        public static void ApplyTo(this UpdateMapPackDto dto, MapPack entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            if (dto.GameServerId.HasValue) entity.GameServerId = dto.GameServerId.Value;
            if (dto.Title is not null) entity.Title = dto.Title;
            if (dto.Description is not null) entity.Description = dto.Description;
            if (dto.GameMode is not null) entity.GameMode = dto.GameMode;
            if (dto.SyncToGameServer.HasValue) entity.SyncToGameServer = dto.SyncToGameServer.Value;
            if (dto.SyncCompleted.HasValue) entity.SyncCompleted = dto.SyncCompleted.Value;
            if (dto.Deleted.HasValue) entity.Deleted = dto.Deleted.Value;
        }

        /// <summary>
        /// Maps a MapPackMap entity to a MapPackMapDto.
        /// </summary>
        /// <param name="entity">The MapPackMap entity to map from.</param>
        /// <returns>The mapped MapPackMapDto.</returns>
        public static MapPackMapDto ToDto(this MapPackMap entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new MapPackMapDto
            {
                MapPackMapId = entity.MapPackMapId,
                MapId = entity.MapId
            };
        }
    }
}