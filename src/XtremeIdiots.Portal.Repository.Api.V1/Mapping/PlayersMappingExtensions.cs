using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for Player entities and DTOs.
    /// </summary>
    public static class PlayersMappingExtensions
    {
        /// <summary>
        /// Maps a Player entity to a PlayerDto (without navigation properties).
        /// Navigation properties will need to be populated separately after mapping.
        /// </summary>
        /// <param name="entity">The Player entity to map from.</param>
        /// <returns>The mapped PlayerDto without navigation properties.</returns>
        public static PlayerDto ToDto(this Player entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new PlayerDto
            {
                PlayerId = entity.PlayerId,
                GameType = entity.GameType.ToGameType(),
                Username = entity.Username ?? string.Empty,
                Guid = entity.Guid ?? string.Empty,
                FirstSeen = entity.FirstSeen,
                LastSeen = entity.LastSeen,
                IpAddress = entity.IpAddress ?? string.Empty,
                PlayerAliases = new List<AliasDto>(),
                PlayerIpAddresses = new List<IpAddressDto>(),
                AdminActions = new List<AdminActionDto>(),
                Reports = new List<ReportDto>(),
                RelatedPlayers = new List<RelatedPlayerDto>(),
                ProtectedNames = new List<ProtectedNameDto>(),
                Tags = new List<PlayerTagDto>()
            };
        }

        /// <summary>
        /// Maps a CreatePlayerDto to a Player entity.
        /// </summary>
        /// <param name="dto">The CreatePlayerDto to map from.</param>
        /// <returns>The mapped Player entity.</returns>
        public static Player ToEntity(this CreatePlayerDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var now = DateTime.UtcNow;

            return new Player
            {
                GameType = dto.GameType.ToGameTypeInt(),
                Username = dto.Username,
                Guid = dto.Guid,
                IpAddress = dto.IpAddress,
                FirstSeen = now,
                LastSeen = now
            };
        }

        /// <summary>
        /// Maps a PlayerAlias entity to an AliasDto.
        /// </summary>
        /// <param name="entity">The PlayerAlias entity to map from.</param>
        /// <returns>The mapped AliasDto.</returns>
        public static AliasDto ToAliasDto(this PlayerAlias entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new AliasDto
            {
                Name = entity.Name ?? string.Empty,
                Added = entity.Added,
                LastUsed = entity.LastUsed,
                ConfidenceScore = entity.ConfidenceScore
            };
        }

        /// <summary>
        /// Maps a PlayerAlias entity to a PlayerAliasDto.
        /// </summary>
        /// <param name="entity">The PlayerAlias entity to map from.</param>
        /// <returns>The mapped PlayerAliasDto.</returns>
        public static PlayerAliasDto ToPlayerAliasDto(this PlayerAlias entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new PlayerAliasDto
            {
                PlayerAliasId = entity.PlayerAliasId,
                PlayerId = entity.PlayerId,
                Name = entity.Name ?? string.Empty,
                Added = entity.Added
            };
        }

        /// <summary>
        /// Maps a PlayerIpAddress entity to an IpAddressDto.
        /// </summary>
        /// <param name="entity">The PlayerIpAddress entity to map from.</param>
        /// <returns>The mapped IpAddressDto.</returns>
        public static IpAddressDto ToIpAddressDto(this PlayerIpAddress entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new IpAddressDto
            {
                Address = entity.Address ?? string.Empty,
                Added = entity.Added,
                LastUsed = entity.LastUsed,
                ConfidenceScore = entity.ConfidenceScore
            };
        }

        /// <summary>
        /// Maps a PlayerIpAddress entity to a RelatedPlayerDto.
        /// </summary>
        /// <param name="entity">The PlayerIpAddress entity to map from.</param>
        /// <returns>The mapped RelatedPlayerDto.</returns>
        public static RelatedPlayerDto ToRelatedPlayerDto(this PlayerIpAddress entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            if (entity.Player == null)
                throw new InvalidOperationException("Player navigation property is required for RelatedPlayerDto mapping");

            return new RelatedPlayerDto
            {
                GameType = entity.Player.GameType.ToGameType(),
                Username = entity.Player.Username ?? string.Empty,
                PlayerId = entity.Player.PlayerId,
                IpAddress = entity.Address ?? string.Empty
            };
        }

        /// <summary>
        /// Maps a ProtectedName entity to a ProtectedNameDto.
        /// </summary>
        /// <param name="entity">The ProtectedName entity to map from.</param>
        /// <returns>The mapped ProtectedNameDto.</returns>
        public static ProtectedNameDto ToProtectedNameDto(this ProtectedName entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new ProtectedNameDto
            {
                ProtectedNameId = entity.ProtectedNameId,
                PlayerId = entity.PlayerId ?? Guid.Empty,
                Name = entity.Name ?? string.Empty,
                CreatedOn = entity.CreatedOn,
                CreatedByUserProfileId = entity.CreatedByUserProfileId ?? Guid.Empty
                // Note: CreatedByUserProfile will be null to avoid circular dependency
            };
        }

        /// <summary>
        /// Maps a CreateProtectedNameDto to a ProtectedName entity.
        /// </summary>
        /// <param name="dto">The CreateProtectedNameDto to map from.</param>
        /// <returns>The mapped ProtectedName entity.</returns>
        public static ProtectedName ToEntity(this CreateProtectedNameDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ProtectedName
            {
                PlayerId = dto.PlayerId,
                Name = dto.Name,
                CreatedOn = DateTime.UtcNow
                // Note: CreatedByUserProfileId will need to be set separately based on AdminId lookup
            };
        }
    }
}