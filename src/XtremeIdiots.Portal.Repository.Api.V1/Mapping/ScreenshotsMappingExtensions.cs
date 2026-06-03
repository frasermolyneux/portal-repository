using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    public static class ScreenshotsMappingExtensions
    {
        public static ScreenshotDto ToDto(this Screenshot entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new ScreenshotDto
            {
                ScreenshotId = entity.ScreenshotId,
                GameServerId = entity.GameServerId,
                GameType = entity.GameType.ToGameType().ToString(),
                PlayerIdentifier = entity.PlayerIdentifier,
                PlayerName = entity.PlayerName,
                LinkSource = entity.LinkSource,
                LinkConfidence = entity.LinkConfidence,
                LinkDiagnostics = entity.LinkDiagnostics,
                CapturedUtc = entity.CapturedUtc,
                BlobContainer = entity.BlobContainer,
                BlobName = entity.BlobName,
                BlobUri = entity.BlobUri,
                ContentType = entity.ContentType,
                SizeBytes = entity.SizeBytes,
                ETag = entity.Etag,
                Source = entity.Source,
                Fingerprint = entity.Fingerprint,
                SourceFileName = entity.SourceFileName,
                SourceSizeBytes = entity.SourceSizeBytes,
                SourceLastWriteUtc = entity.SourceLastWriteUtc,
                Deleted = entity.Deleted,
                DeletedUtc = entity.DeletedUtc,
                CreatedUtc = entity.CreatedUtc,
                LastUpdatedUtc = entity.LastUpdatedUtc,
                GameServer = expand && entity.GameServer is not null ? entity.GameServer.ToDto(false) : null
            };
        }

        public static PendingScreenshotRequestDto ToDto(this ScreenshotPendingRequest entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new PendingScreenshotRequestDto
            {
                ScreenshotPendingRequestId = entity.ScreenshotPendingRequestId,
                GameServerId = entity.GameServerId,
                PlayerIdentifier = entity.PlayerIdentifier,
                PlayerName = entity.PlayerName,
                CorrelationKey = entity.CorrelationKey,
                RequestedAtUtc = entity.RequestedAtUtc,
                ExpiresAtUtc = entity.ExpiresAtUtc,
                ConsumedAtUtc = entity.ConsumedAtUtc,
                CreatedBy = entity.CreatedBy,
                CreatedUtc = entity.CreatedUtc,
                LastUpdatedUtc = entity.LastUpdatedUtc
            };
        }
    }
}
