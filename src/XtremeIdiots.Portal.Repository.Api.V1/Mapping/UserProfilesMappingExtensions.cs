using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for UserProfile entities and DTOs.
    /// </summary>
    public static class UserProfilesMappingExtensions
    {
        /// <summary>
        /// Maps a UserProfile entity to a UserProfileDto.
        /// </summary>
        /// <param name="entity">The UserProfile entity to map from.</param>
        /// <returns>The mapped UserProfileDto.</returns>
        public static UserProfileDto ToDto(this UserProfile entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new UserProfileDto
            {
                UserProfileId = entity.UserProfileId,
                IdentityOid = entity.IdentityOid,
                XtremeIdiotsForumId = entity.XtremeIdiotsForumId,
                DemoAuthKey = entity.DemoAuthKey,
                DisplayName = entity.DisplayName,
                FormattedName = entity.FormattedName,
                PrimaryGroup = entity.PrimaryGroup,
                Email = entity.Email,
                PhotoUrl = entity.PhotoUrl,
                ProfileUrl = entity.ProfileUrl,
                TimeZone = entity.TimeZone,
                UserProfileClaims = entity.UserProfileClaims?.Select(c => c.ToDto(false)).ToList() ?? new List<UserProfileClaimDto>()
            };
        }

        /// <summary>
        /// Maps a CreateUserProfileDto to a UserProfile entity.
        /// </summary>
        /// <param name="dto">The CreateUserProfileDto to map from.</param>
        /// <returns>The mapped UserProfile entity.</returns>
        public static UserProfile ToEntity(this CreateUserProfileDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new UserProfile
            {
                IdentityOid = dto.IdentityOid,
                XtremeIdiotsForumId = dto.XtremeIdiotsForumId,
                DemoAuthKey = dto.DemoAuthKey,
                DisplayName = dto.DisplayName,
                FormattedName = dto.FormattedName,
                PrimaryGroup = dto.PrimaryGroup,
                Email = dto.Email,
                PhotoUrl = dto.PhotoUrl,
                ProfileUrl = dto.ProfileUrl,
                TimeZone = dto.TimeZone
            };
        }

        /// <summary>
        /// Applies the values from an EditUserProfileDto to an existing UserProfile entity,
        /// preserving null-handling behavior (only updates non-null values).
        /// </summary>
        /// <param name="dto">The EditUserProfileDto containing the updates.</param>
        /// <param name="entity">The existing UserProfile entity to update.</param>
        public static void ApplyTo(this EditUserProfileDto dto, UserProfile entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            if (dto.IdentityOid is not null) entity.IdentityOid = dto.IdentityOid;
            if (dto.XtremeIdiotsForumId is not null) entity.XtremeIdiotsForumId = dto.XtremeIdiotsForumId;
            if (dto.DemoAuthKey is not null) entity.DemoAuthKey = dto.DemoAuthKey;
            if (dto.DisplayName is not null) entity.DisplayName = dto.DisplayName;
            if (dto.FormattedName is not null) entity.FormattedName = dto.FormattedName;
            if (dto.PrimaryGroup is not null) entity.PrimaryGroup = dto.PrimaryGroup;
            if (dto.Email is not null) entity.Email = dto.Email;
            if (dto.PhotoUrl is not null) entity.PhotoUrl = dto.PhotoUrl;
            if (dto.ProfileUrl is not null) entity.ProfileUrl = dto.ProfileUrl;
            if (dto.TimeZone is not null) entity.TimeZone = dto.TimeZone;
        }

        /// <summary>
        /// Maps a UserProfileClaim entity to a UserProfileClaimDto.
        /// </summary>
        /// <param name="entity">The UserProfileClaim entity to map from.</param>
        /// <returns>The mapped UserProfileClaimDto.</returns>
        public static UserProfileClaimDto ToDto(this UserProfileClaim entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new UserProfileClaimDto
            {
                UserProfileClaimId = entity.UserProfileClaimId,
                UserProfileId = entity.UserProfileId,
                SystemGenerated = entity.SystemGenerated,
                ClaimType = entity.ClaimType,
                ClaimValue = entity.ClaimValue
            };
        }

        /// <summary>
        /// Maps a CreateUserProfileClaimDto to a UserProfileClaim entity.
        /// </summary>
        /// <param name="dto">The CreateUserProfileClaimDto to map from.</param>
        /// <returns>The mapped UserProfileClaim entity.</returns>
        public static UserProfileClaim ToEntity(this CreateUserProfileClaimDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new UserProfileClaim
            {
                UserProfileId = dto.UserProfileId,
                SystemGenerated = dto.SystemGenerated,
                ClaimType = dto.ClaimType,
                ClaimValue = dto.ClaimValue
            };
        }
    }
}