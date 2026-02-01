using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for Map entities and DTOs.
    /// </summary>
    public static class MapsMappingExtensions
    {
        /// <summary>
        /// Maps a Map entity to a MapDto.
        /// </summary>
        /// <param name="entity">The Map entity to map from.</param>
        /// <returns>The mapped MapDto.</returns>
        public static MapDto ToDto(this Map entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            List<MapFileDto> mapFiles;
            try
            {
                mapFiles = string.IsNullOrEmpty(entity.MapFiles)
                    ? []
                    : JsonConvert.DeserializeObject<List<MapFileDto>>(entity.MapFiles) ?? [];
            }
            catch (JsonException)
            {
                // Fallback to empty list if deserialization fails
                mapFiles = [];
            }

            return new MapDto
            {
                MapId = entity.MapId,
                GameType = entity.GameType.ToGameType(),
                MapName = entity.MapName ?? string.Empty,
                MapImageUri = entity.MapImageUri ?? string.Empty,
                TotalLikes = entity.TotalLikes,
                TotalDislikes = entity.TotalDislikes,
                TotalVotes = entity.TotalVotes,
                LikePercentage = entity.LikePercentage,
                DislikePercentage = entity.DislikePercentage,
                MapFiles = mapFiles
            };
        }

        /// <summary>
        /// Maps a CreateMapDto to a Map entity.
        /// </summary>
        /// <param name="dto">The CreateMapDto to map from.</param>
        /// <returns>The mapped Map entity.</returns>
        public static Map ToEntity(this CreateMapDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new Map
            {
                GameType = dto.GameType.ToGameTypeInt(),
                MapName = dto.MapName,
                MapFiles = dto.MapFiles?.Count > 0 ? JsonConvert.SerializeObject(dto.MapFiles) : null,
                TotalLikes = 0,
                TotalDislikes = 0,
                TotalVotes = 0,
                LikePercentage = 0.0,
                DislikePercentage = 0.0
            };
        }

        /// <summary>
        /// Applies the values from an EditMapDto to an existing Map entity,
        /// preserving null-handling behavior (only updates non-null values).
        /// </summary>
        /// <param name="dto">The EditMapDto containing the updates.</param>
        /// <param name="entity">The existing Map entity to update.</param>
        public static void ApplyTo(this EditMapDto dto, Map entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            // EditMapDto only has MapFiles, so only update that
            if (dto.MapFiles?.Count > 0)
            {
                entity.MapFiles = JsonConvert.SerializeObject(dto.MapFiles);
            }
        }

        /// <summary>
        /// Maps a MapVote entity to a MapVoteDto.
        /// NOTE: This method assumes navigation properties (Map, Player, GameServer) are already loaded if needed.
        /// </summary>
        /// <param name="entity">The MapVote entity to map from.</param>
        /// <returns>The mapped MapVoteDto.</returns>
        public static MapVoteDto ToDto(this MapVote entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new MapVoteDto
            {
                MapVoteId = entity.MapVoteId,
                MapId = entity.MapId,
                PlayerId = entity.PlayerId,
                GameServerId = entity.GameServerId,
                Like = entity.Like,
                Timestamp = entity.Timestamp,
                Map = expand && entity.Map is not null ? entity.Map.ToDto(false) : null,
                Player = expand && entity.Player is not null ? entity.Player.ToDto(false) : null,
                GameServer = expand && entity.GameServer is not null ? entity.GameServer.ToDto(false) : null
            };
        }

        /// <summary>
        /// Maps an UpsertMapVoteDto to a MapVote entity.
        /// </summary>
        /// <param name="dto">The UpsertMapVoteDto to map from.</param>
        /// <returns>The mapped MapVote entity.</returns>
        public static MapVote ToEntity(this UpsertMapVoteDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new MapVote
            {
                MapId = dto.MapId,
                PlayerId = dto.PlayerId,
                GameServerId = dto.GameServerId,
                Like = dto.Like,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}