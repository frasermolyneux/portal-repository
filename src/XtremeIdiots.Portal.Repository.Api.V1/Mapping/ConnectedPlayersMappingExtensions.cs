using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    public static class ConnectedPlayersMappingExtensions
    {
        public static ConnectedPlayerDto ToDto(this ConnectedPlayerProfile entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new ConnectedPlayerDto
            {
                ConnectedPlayerProfileId = entity.ConnectedPlayerProfileId,
                PlayerId = entity.PlayerId,
                UserProfileId = entity.UserProfileId,
                GameType = entity.Player?.GameType.ToGameType() ?? Abstractions.Constants.V1.GameType.Unknown,
                Username = entity.Player?.Username ?? string.Empty,
                LinkMethod = ParseLinkMethod(entity.LinkMethod),
                LinkedAtUtc = entity.LinkedAtUtc,
                LinkedByUserProfileId = entity.LinkedByUserProfileId,
                UnlinkedAtUtc = entity.UnlinkedAtUtc,
                UnlinkedByUserProfileId = entity.UnlinkedByUserProfileId,
                IsActive = entity.IsActive
            };
        }

        public static ConnectedPlayerProfile ToEntity(this CreateConnectedPlayerLinkDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ConnectedPlayerProfile
            {
                PlayerId = dto.PlayerId,
                UserProfileId = dto.UserProfileId,
                LinkMethod = dto.LinkMethod.ToString(),
                LinkedAtUtc = DateTime.UtcNow,
                LinkedByUserProfileId = dto.LinkedByUserProfileId,
                IsActive = true
            };
        }

        public static ConnectedPlayerActivationCodeDto ToDto(this ConnectedPlayerActivationCode entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new ConnectedPlayerActivationCodeDto
            {
                ConnectedPlayerActivationCodeId = entity.ConnectedPlayerActivationCodeId,
                UserProfileId = entity.UserProfileId,
                Code = entity.Code,
                ExpiresAtUtc = entity.ExpiresAtUtc,
                AttemptCount = entity.AttemptCount,
                MaxAttempts = entity.MaxAttempts,
                IsActive = entity.IsActive,
                ActivatedAtUtc = entity.ActivatedAtUtc
            };
        }

        private static ConnectedPlayerLinkMethod ParseLinkMethod(string value)
        {
            if (Enum.TryParse<ConnectedPlayerLinkMethod>(value, true, out var result))
                return result;

            // Keep backward-read compatibility for historical rows while TokenVerified is phased out.
            if (string.Equals(value, "TokenVerified", StringComparison.OrdinalIgnoreCase))
                return ConnectedPlayerLinkMethod.ActivationCode;

            return ConnectedPlayerLinkMethod.TrustedWebsite;
        }
    }
}
