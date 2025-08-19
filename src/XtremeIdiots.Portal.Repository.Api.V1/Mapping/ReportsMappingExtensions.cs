using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Reports;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    public static class ReportsMappingExtensions
    {
        public static ReportDto ToDto(this Report entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return new ReportDto
            {
                ReportId = entity.ReportId,
                PlayerId = entity.PlayerId ?? Guid.Empty,
                UserProfileId = entity.UserProfileId ?? Guid.Empty,
                GameServerId = entity.GameServerId ?? Guid.Empty,
                GameType = entity.GameType.ToGameType(),
                Comments = entity.Comments ?? string.Empty,
                Timestamp = entity.Timestamp,
                AdminUserProfileId = entity.AdminUserProfileId ?? Guid.Empty,
                AdminClosingComments = entity.AdminClosingComments,
                Closed = entity.Closed,
                ClosedTimestamp = entity.ClosedTimestamp ?? DateTime.MinValue,
                UserProfile = null,
                AdminUserProfile = null
            };
        }

        public static Report ToEntity(this CreateReportDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return new Report
            {
                PlayerId = dto.PlayerId,
                UserProfileId = dto.UserProfileId,
                GameServerId = dto.GameServerId,
                Comments = dto.Comments,
                Timestamp = DateTime.UtcNow,
                Closed = false
            };
        }

        public static void ApplyTo(this CloseReportDto dto, Report entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);
            entity.AdminClosingComments = dto.AdminClosingComments;
            entity.Closed = true;
            entity.ClosedTimestamp = DateTime.UtcNow;
        }
    }
}