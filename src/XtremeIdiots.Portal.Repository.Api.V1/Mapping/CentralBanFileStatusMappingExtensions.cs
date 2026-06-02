using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.CentralBanFileStatus;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XiCentralBanFileStatusEntity = XtremeIdiots.Portal.Repository.DataLib.CentralBanFileStatus;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    public static class CentralBanFileStatusMappingExtensions
    {
        public static CentralBanFileStatusDto ToDto(this XiCentralBanFileStatusEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new CentralBanFileStatusDto
            {
                GameType = entity.GameType.ToGameType(),
                BlobLastRegeneratedUtc = entity.BlobLastRegeneratedUtc,
                BlobETag = entity.BlobEtag,
                BlobSizeBytes = entity.BlobSizeBytes,
                TotalLineCount = entity.TotalLineCount,
                BanSyncLineCount = entity.BanSyncLineCount,
                ExternalLineCount = entity.ExternalLineCount,
                ExternalSourceLastModifiedUtc = entity.ExternalSourceLastModifiedUtc,
                LastRegenerationDurationMs = entity.LastRegenerationDurationMs,
                LastRegenerationError = entity.LastRegenerationError,
                ActiveBanSetHash = entity.ActiveBanSetHash,
                LegacyBlobLastRegeneratedUtc = entity.LegacyBlobLastRegeneratedUtc,
                LegacyBlobETag = entity.LegacyBlobEtag,
                LegacyBlobSizeBytes = entity.LegacyBlobSizeBytes,
                LegacyTotalLineCount = entity.LegacyTotalLineCount,
                LegacyBanSyncLineCount = entity.LegacyBanSyncLineCount,
                LegacyExternalLineCount = entity.LegacyExternalLineCount,
                LegacyExternalSourceLastModifiedUtc = entity.LegacyExternalSourceLastModifiedUtc,
                LegacyLastRegenerationDurationMs = entity.LegacyLastRegenerationDurationMs,
                LegacyLastRegenerationError = entity.LegacyLastRegenerationError,
                LegacyActiveBanSetHash = entity.LegacyActiveBanSetHash,
                LastUpdatedUtc = entity.LastUpdatedUtc
            };
        }

        /// <summary>
        /// Applies non-null fields from an upsert DTO to an existing entity.
        /// </summary>
        public static void ApplyTo(this UpsertCentralBanFileStatusDto dto, XiCentralBanFileStatusEntity entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            if (dto.BlobLastRegeneratedUtc.HasValue) entity.BlobLastRegeneratedUtc = dto.BlobLastRegeneratedUtc.Value;
            if (dto.BlobETag is not null) entity.BlobEtag = dto.BlobETag;
            if (dto.BlobSizeBytes.HasValue) entity.BlobSizeBytes = dto.BlobSizeBytes.Value;
            if (dto.TotalLineCount.HasValue) entity.TotalLineCount = dto.TotalLineCount.Value;
            if (dto.BanSyncLineCount.HasValue) entity.BanSyncLineCount = dto.BanSyncLineCount.Value;
            if (dto.ExternalLineCount.HasValue) entity.ExternalLineCount = dto.ExternalLineCount.Value;
            if (dto.ExternalSourceLastModifiedUtc.HasValue) entity.ExternalSourceLastModifiedUtc = dto.ExternalSourceLastModifiedUtc.Value;
            if (dto.LastRegenerationDurationMs.HasValue) entity.LastRegenerationDurationMs = dto.LastRegenerationDurationMs.Value;
            // LastRegenerationError is allowed to be cleared by passing an empty string;
            // null on the DTO means "leave unchanged" so callers always set this explicitly
            // when finishing a regeneration.
            if (dto.LastRegenerationError is not null) entity.LastRegenerationError = dto.LastRegenerationError.Length == 0 ? null : dto.LastRegenerationError;
            if (dto.ActiveBanSetHash is not null) entity.ActiveBanSetHash = dto.ActiveBanSetHash;

            if (dto.LegacyBlobLastRegeneratedUtc.HasValue) entity.LegacyBlobLastRegeneratedUtc = dto.LegacyBlobLastRegeneratedUtc.Value;
            if (dto.LegacyBlobETag is not null) entity.LegacyBlobEtag = dto.LegacyBlobETag;
            if (dto.LegacyBlobSizeBytes.HasValue) entity.LegacyBlobSizeBytes = dto.LegacyBlobSizeBytes.Value;
            if (dto.LegacyTotalLineCount.HasValue) entity.LegacyTotalLineCount = dto.LegacyTotalLineCount.Value;
            if (dto.LegacyBanSyncLineCount.HasValue) entity.LegacyBanSyncLineCount = dto.LegacyBanSyncLineCount.Value;
            if (dto.LegacyExternalLineCount.HasValue) entity.LegacyExternalLineCount = dto.LegacyExternalLineCount.Value;
            if (dto.LegacyExternalSourceLastModifiedUtc.HasValue) entity.LegacyExternalSourceLastModifiedUtc = dto.LegacyExternalSourceLastModifiedUtc.Value;
            if (dto.LegacyLastRegenerationDurationMs.HasValue) entity.LegacyLastRegenerationDurationMs = dto.LegacyLastRegenerationDurationMs.Value;
            if (dto.LegacyLastRegenerationError is not null) entity.LegacyLastRegenerationError = dto.LegacyLastRegenerationError.Length == 0 ? null : dto.LegacyLastRegenerationError;
            if (dto.LegacyActiveBanSetHash is not null) entity.LegacyActiveBanSetHash = dto.LegacyActiveBanSetHash;

            entity.LastUpdatedUtc = DateTime.UtcNow;
        }
    }
}
