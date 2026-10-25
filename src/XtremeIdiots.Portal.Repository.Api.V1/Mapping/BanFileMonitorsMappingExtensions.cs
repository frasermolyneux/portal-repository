using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for BanFileMonitor entities and DTOs. Monitors are
    /// owned by the server agent; only status upserts are supported now.
    /// </summary>
    public static class BanFileMonitorsMappingExtensions
    {
        /// <summary>
        /// Maps a BanFileMonitor entity to a BanFileMonitorDto.
        /// NOTE: GameServer navigation property will be null when expand=false to avoid
        /// circular dependencies.
        /// </summary>
        public static BanFileMonitorDto ToDto(this BanFileMonitor entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new BanFileMonitorDto
            {
                BanFileMonitorId = entity.BanFileMonitorId,
                GameServerId = entity.GameServerId,
                RemoteFileSize = entity.RemoteFileSize,
                LastCheckUtc = entity.LastCheckUtc,
                LastCheckResult = entity.LastCheckResult,
                LastCheckErrorMessage = entity.LastCheckErrorMessage,
                RemoteFilePath = entity.RemoteFilePath,
                ResolvedForMod = entity.ResolvedForMod,
                LastImportUtc = entity.LastImportUtc,
                LastImportBanCount = entity.LastImportBanCount,
                LastImportSampleNames = entity.LastImportSampleNames,
                LastPushUtc = entity.LastPushUtc,
                LastPushedETag = entity.LastPushedEtag,
                LastPushedSize = entity.LastPushedSize,
                LastCentralBlobETag = entity.LastCentralBlobEtag,
                LastCentralBlobUtc = entity.LastCentralBlobUtc,
                ConsecutiveFailureCount = entity.ConsecutiveFailureCount,
                RemoteTotalLineCount = entity.RemoteTotalLineCount,
                RemoteUntaggedCount = entity.RemoteUntaggedCount,
                RemoteBanSyncCount = entity.RemoteBanSyncCount,
                RemoteExternalCount = entity.RemoteExternalCount,
                GameServer = expand && entity.GameServer is not null ? entity.GameServer.ToDto(false) : null!
            };
        }

        /// <summary>
        /// Applies the values from an UpsertBanFileMonitorStatusDto to an existing
        /// BanFileMonitor entity. A timestamp, result and path identify a complete
        /// check snapshot; nullable observations in that snapshot replace old values.
        /// Push and import fields remain partial updates.
        /// </summary>
        public static void ApplyStatus(this UpsertBanFileMonitorStatusDto dto, BanFileMonitor entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            var completeCheck = dto.LastCheckUtc.HasValue
                && dto.LastCheckResult is not null
                && dto.RemoteFilePath is not null;

            if (dto.LastCheckUtc.HasValue)
            {
                entity.LastCheckUtc = dto.LastCheckUtc.Value;
            }

            if (dto.LastCheckResult is not null)
            {
                entity.LastCheckResult = dto.LastCheckResult;
            }

            if (completeCheck || dto.LastCheckErrorMessage is not null
                || (dto.LastCheckUtc.HasValue && string.Equals(dto.LastCheckResult, "Success", StringComparison.OrdinalIgnoreCase)))
            {
                entity.LastCheckErrorMessage = dto.LastCheckErrorMessage;
            }

            if (dto.RemoteFilePath is not null)
            {
                entity.RemoteFilePath = dto.RemoteFilePath;
            }

            if (completeCheck || dto.ResolvedForMod is not null)
            {
                entity.ResolvedForMod = dto.ResolvedForMod;
            }

            if (completeCheck || dto.RemoteFileSize.HasValue)
            {
                entity.RemoteFileSize = dto.RemoteFileSize;
            }

            if (dto.LastImportUtc.HasValue)
            {
                entity.LastImportUtc = dto.LastImportUtc.Value;
            }

            if (dto.LastImportBanCount.HasValue)
            {
                entity.LastImportBanCount = dto.LastImportBanCount.Value;
            }

            if (dto.LastImportSampleNames is not null)
            {
                entity.LastImportSampleNames = dto.LastImportSampleNames;
            }

            if (dto.LastPushUtc.HasValue)
            {
                entity.LastPushUtc = dto.LastPushUtc.Value;
            }

            if (dto.LastPushedETag is not null)
            {
                entity.LastPushedEtag = dto.LastPushedETag;
            }

            if (dto.LastPushedSize.HasValue)
            {
                entity.LastPushedSize = dto.LastPushedSize.Value;
            }

            if (dto.LastCentralBlobETag is not null)
            {
                entity.LastCentralBlobEtag = dto.LastCentralBlobETag;
            }

            if (dto.LastCentralBlobUtc.HasValue)
            {
                entity.LastCentralBlobUtc = dto.LastCentralBlobUtc.Value;
            }

            if (dto.ConsecutiveFailureCount.HasValue)
            {
                entity.ConsecutiveFailureCount = dto.ConsecutiveFailureCount.Value;
            }

            if (completeCheck || dto.RemoteTotalLineCount.HasValue)
            {
                entity.RemoteTotalLineCount = dto.RemoteTotalLineCount;
            }

            if (completeCheck || dto.RemoteUntaggedCount.HasValue)
            {
                entity.RemoteUntaggedCount = dto.RemoteUntaggedCount;
            }

            if (completeCheck || dto.RemoteBanSyncCount.HasValue)
            {
                entity.RemoteBanSyncCount = dto.RemoteBanSyncCount;
            }

            if (completeCheck || dto.RemoteExternalCount.HasValue)
            {
                entity.RemoteExternalCount = dto.RemoteExternalCount;
            }
        }
    }
}
