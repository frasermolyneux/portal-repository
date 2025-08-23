using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for Tag and PlayerTag entities and DTOs.
    /// </summary>
    public static class TagsMappingExtensions
    {
        /// <summary>
        /// Maps a Tag entity to a TagDto.
        /// </summary>
        /// <param name="entity">The Tag entity to map from.</param>
        /// <returns>The mapped TagDto.</returns>
        public static TagDto ToDto(this Tag entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new TagDto
            {
                TagId = entity.TagId,
                Name = entity.Name,
                Description = entity.Description,
                UserDefined = entity.UserDefined,
                TagHtml = entity.TagHtml
            };
        }

        /// <summary>
        /// Maps a TagDto to a Tag entity.
        /// </summary>
        /// <param name="dto">The TagDto to map from.</param>
        /// <returns>The mapped Tag entity.</returns>
        public static Tag ToEntity(this TagDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new Tag
            {
                TagId = dto.TagId,
                Name = dto.Name,
                Description = dto.Description,
                UserDefined = dto.UserDefined,
                TagHtml = dto.TagHtml
            };
        }

        /// <summary>
        /// Applies the values from a TagDto to an existing Tag entity,
        /// preserving null-handling behavior (only updates non-null values).
        /// </summary>
        /// <param name="dto">The TagDto containing the updates.</param>
        /// <param name="entity">The existing Tag entity to update.</param>
        public static void ApplyTo(this TagDto dto, Tag entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            if (dto.Name is not null) entity.Name = dto.Name;
            if (dto.Description is not null) entity.Description = dto.Description;
            entity.UserDefined = dto.UserDefined; // bool is not nullable, always update
            if (dto.TagHtml is not null) entity.TagHtml = dto.TagHtml;
        }

        /// <summary>
        /// Maps a PlayerTag entity to a PlayerTagDto.
        /// NOTE: This method assumes navigation properties (Player, Tag, UserProfile) are already loaded if needed.
        /// </summary>
        /// <param name="entity">The PlayerTag entity to map from.</param>
        /// <returns>The mapped PlayerTagDto.</returns>
        public static PlayerTagDto ToDto(this PlayerTag entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new PlayerTagDto
            {
                PlayerTagId = entity.PlayerTagId,
                PlayerId = entity.PlayerId,
                TagId = entity.TagId,
                UserProfileId = entity.UserProfileId,
                Assigned = entity.Assigned,
                Player = expand && entity.Player != null ? entity.Player.ToDto(false) : null,
                Tag = expand && entity.Tag != null ? entity.Tag.ToDto() : null,
                UserProfile = expand && entity.UserProfile != null ? entity.UserProfile.ToDto() : null
            };
        }

        /// <summary>
        /// Maps a PlayerTagDto to a PlayerTag entity.
        /// </summary>
        /// <param name="dto">The PlayerTagDto to map from.</param>
        /// <returns>The mapped PlayerTag entity.</returns>
        public static PlayerTag ToEntity(this PlayerTagDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new PlayerTag
            {
                PlayerTagId = dto.PlayerTagId,
                PlayerId = dto.PlayerId,
                TagId = dto.TagId,
                UserProfileId = dto.UserProfileId,
                Assigned = dto.Assigned
            };
        }

        /// <summary>
        /// Applies the values from a PlayerTagDto to an existing PlayerTag entity,
        /// preserving null-handling behavior (only updates non-null values).
        /// </summary>
        /// <param name="dto">The PlayerTagDto containing the updates.</param>
        /// <param name="entity">The existing PlayerTag entity to update.</param>
        public static void ApplyTo(this PlayerTagDto dto, PlayerTag entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            if (dto.PlayerId.HasValue) entity.PlayerId = dto.PlayerId.Value;
            if (dto.TagId.HasValue) entity.TagId = dto.TagId.Value;
            if (dto.UserProfileId.HasValue) entity.UserProfileId = dto.UserProfileId.Value;
            entity.Assigned = dto.Assigned; // DateTime is not nullable, always update
        }
    }
}