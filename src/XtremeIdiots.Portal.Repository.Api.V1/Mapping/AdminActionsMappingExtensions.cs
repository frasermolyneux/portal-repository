using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for AdminAction entities and DTOs.
    /// </summary>
    public static class AdminActionsMappingExtensions
    {
        /// <summary>
        /// Maps an AdminAction entity to an AdminActionDto.
        /// NOTE: This method assumes navigation properties (Player, UserProfile) are already loaded.
        /// </summary>
        /// <param name="entity">The AdminAction entity to map from.</param>
        /// <returns>The mapped AdminActionDto.</returns>
        public static AdminActionDto ToDto(this AdminAction entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new AdminActionDto
            {
                AdminActionId = entity.AdminActionId,
                PlayerId = entity.PlayerId,
                UserProfileId = entity.UserProfileId,
                ForumTopicId = entity.ForumTopicId,
                Type = entity.Type.ToAdminActionType(),
                Text = entity.Text,
                Created = entity.Created,
                Expires = entity.Expires,
                Player = expand && entity.Player is not null ? entity.Player.ToDto(false) : null!,
                UserProfile = expand && entity.UserProfile is not null ? entity.UserProfile.ToDto(false) : null
            };
        }

        /// <summary>
        /// Maps a CreateAdminActionDto to an AdminAction entity.
        /// </summary>
        /// <param name="dto">The CreateAdminActionDto to map from.</param>
        /// <returns>The mapped AdminAction entity.</returns>
        public static AdminAction ToEntity(this CreateAdminActionDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new AdminAction
            {
                PlayerId = dto.PlayerId,
                Type = dto.Type.ToAdminActionTypeInt(),
                Text = dto.Text,
                Expires = dto.Expires,
                ForumTopicId = dto.ForumTopicId,
                Created = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Applies the values from an EditAdminActionDto to an existing AdminAction entity,
        /// preserving null-handling behavior (only updates non-null values).
        /// </summary>
        /// <param name="dto">The EditAdminActionDto containing the updates.</param>
        /// <param name="entity">The existing AdminAction entity to update.</param>
        public static void ApplyTo(this EditAdminActionDto dto, AdminAction entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            if (dto.Text is not null) entity.Text = dto.Text;
            if (dto.Expires.HasValue) entity.Expires = dto.Expires.Value;
            if (dto.ForumTopicId.HasValue) entity.ForumTopicId = dto.ForumTopicId.Value;
        }
    }
}