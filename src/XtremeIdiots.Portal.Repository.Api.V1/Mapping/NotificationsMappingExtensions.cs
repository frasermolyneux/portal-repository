using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for Notification, NotificationType, and NotificationPreference entities and DTOs.
    /// </summary>
    public static class NotificationsMappingExtensions
    {
        /// <summary>
        /// Maps a NotificationType entity to a NotificationTypeDto.
        /// </summary>
        /// <param name="entity">The NotificationType entity to map from.</param>
        /// <returns>The mapped NotificationTypeDto.</returns>
        public static NotificationTypeDto ToDto(this NotificationType entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new NotificationTypeDto
            {
                NotificationTypeId = entity.NotificationTypeId,
                DisplayName = entity.DisplayName,
                Description = entity.Description,
                Category = entity.Category,
                SupportsInSite = entity.SupportsInSite,
                SupportsEmail = entity.SupportsEmail,
                DefaultChannels = entity.DefaultChannels,
                IsEnabled = entity.IsEnabled,
                SortOrder = entity.SortOrder
            };
        }

        /// <summary>
        /// Maps a NotificationPreference entity to a NotificationPreferenceDto.
        /// </summary>
        /// <param name="entity">The NotificationPreference entity to map from.</param>
        /// <returns>The mapped NotificationPreferenceDto.</returns>
        public static NotificationPreferenceDto ToDto(this NotificationPreference entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new NotificationPreferenceDto
            {
                NotificationPreferenceId = entity.NotificationPreferenceId,
                UserProfileId = entity.UserProfileId,
                NotificationTypeId = entity.NotificationTypeId,
                InSiteEnabled = entity.InSiteEnabled,
                EmailEnabled = entity.EmailEnabled,
                Created = entity.Created,
                LastModified = entity.LastModified
            };
        }

        /// <summary>
        /// Maps a Notification entity to a NotificationDto.
        /// </summary>
        /// <param name="entity">The Notification entity to map from.</param>
        /// <returns>The mapped NotificationDto.</returns>
        public static NotificationDto ToDto(this Notification entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new NotificationDto
            {
                NotificationId = entity.NotificationId,
                UserProfileId = entity.UserProfileId,
                NotificationTypeId = entity.NotificationTypeId,
                Title = entity.Title,
                Message = entity.Message,
                ActionUrl = entity.ActionUrl,
                MetadataJson = entity.MetadataJson,
                IsRead = entity.IsRead,
                ReadAt = entity.ReadAt,
                CreatedAt = entity.CreatedAt,
                EmailSent = entity.EmailSent,
                EmailSentAt = entity.EmailSentAt
            };
        }

        /// <summary>
        /// Maps a CreateNotificationDto to a Notification entity.
        /// </summary>
        /// <param name="dto">The CreateNotificationDto to map from.</param>
        /// <returns>The mapped Notification entity.</returns>
        public static Notification ToEntity(this CreateNotificationDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new Notification
            {
                UserProfileId = dto.UserProfileId,
                NotificationTypeId = dto.NotificationTypeId,
                Title = dto.Title,
                Message = dto.Message,
                ActionUrl = dto.ActionUrl,
                MetadataJson = dto.MetadataJson,
                EmailSent = dto.EmailSent,
                EmailSentAt = dto.EmailSentAt,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Applies the values from an EditNotificationPreferenceDto to an existing NotificationPreference entity,
        /// preserving null-handling behavior (only updates non-null values).
        /// </summary>
        /// <param name="dto">The EditNotificationPreferenceDto containing the updates.</param>
        /// <param name="entity">The existing NotificationPreference entity to update.</param>
        public static void ApplyTo(this EditNotificationPreferenceDto dto, NotificationPreference entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            if (dto.InSiteEnabled.HasValue) entity.InSiteEnabled = dto.InSiteEnabled.Value;
            if (dto.EmailEnabled.HasValue) entity.EmailEnabled = dto.EmailEnabled.Value;
            entity.LastModified = DateTime.UtcNow;
        }
    }
}
