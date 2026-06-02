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
                LegacyLastCheckUtc = entity.LegacyLastCheckUtc,
                LegacyLastCheckResult = entity.LegacyLastCheckResult,
                LegacyLastCheckErrorMessage = entity.LegacyLastCheckErrorMessage,
                LegacyRemoteFilePath = entity.LegacyRemoteFilePath,
                LegacyResolvedForMod = entity.LegacyResolvedForMod,
                LegacyRemoteFileSize = entity.LegacyRemoteFileSize,
                LegacyLastPushUtc = entity.LegacyLastPushUtc,
                LegacyLastPushedETag = entity.LegacyLastPushedEtag,
                LegacyLastPushedSize = entity.LegacyLastPushedSize,
                LegacyLastCentralBlobETag = entity.LegacyLastCentralBlobEtag,
                LegacyLastCentralBlobUtc = entity.LegacyLastCentralBlobUtc,
                LegacyRemoteTotalLineCount = entity.LegacyRemoteTotalLineCount,
                LegacyRemoteUntaggedCount = entity.LegacyRemoteUntaggedCount,
                LegacyRemoteBanSyncCount = entity.LegacyRemoteBanSyncCount,
                LegacyRemoteExternalCount = entity.LegacyRemoteExternalCount,
                GameServer = expand && entity.GameServer is not null ? entity.GameServer.ToDto(false) : null!
            };
        }

        /// <summary>
        /// Applies the values from an UpsertBanFileMonitorStatusDto to an existing
        /// BanFileMonitor entity. Only non-null properties on the DTO are applied so a
        /// partial cycle (check-only, no push) does not blank out unrelated fields.
        /// </summary>
        public static void ApplyStatus(this UpsertBanFileMonitorStatusDto dto, BanFileMonitor entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            if (dto.LastCheckUtc.HasValue) entity.LastCheckUtc = dto.LastCheckUtc.Value;
            if (dto.LastCheckResult is not null) entity.LastCheckResult = dto.LastCheckResult;
            if (dto.LastCheckErrorMessage is not null) entity.LastCheckErrorMessage = dto.LastCheckErrorMessage;
            if (dto.RemoteFilePath is not null) entity.RemoteFilePath = dto.RemoteFilePath;
            if (dto.ResolvedForMod is not null) entity.ResolvedForMod = dto.ResolvedForMod;
            if (dto.RemoteFileSize.HasValue) entity.RemoteFileSize = dto.RemoteFileSize.Value;

            if (dto.LastImportUtc.HasValue) entity.LastImportUtc = dto.LastImportUtc.Value;
            if (dto.LastImportBanCount.HasValue) entity.LastImportBanCount = dto.LastImportBanCount.Value;
            if (dto.LastImportSampleNames is not null) entity.LastImportSampleNames = dto.LastImportSampleNames;

            if (dto.LastPushUtc.HasValue) entity.LastPushUtc = dto.LastPushUtc.Value;
            if (dto.LastPushedETag is not null) entity.LastPushedEtag = dto.LastPushedETag;
            if (dto.LastPushedSize.HasValue) entity.LastPushedSize = dto.LastPushedSize.Value;

            if (dto.LastCentralBlobETag is not null) entity.LastCentralBlobEtag = dto.LastCentralBlobETag;
            if (dto.LastCentralBlobUtc.HasValue) entity.LastCentralBlobUtc = dto.LastCentralBlobUtc.Value;

            if (dto.ConsecutiveFailureCount.HasValue) entity.ConsecutiveFailureCount = dto.ConsecutiveFailureCount.Value;

            if (dto.RemoteTotalLineCount.HasValue) entity.RemoteTotalLineCount = dto.RemoteTotalLineCount.Value;
            if (dto.RemoteUntaggedCount.HasValue) entity.RemoteUntaggedCount = dto.RemoteUntaggedCount.Value;
            if (dto.RemoteBanSyncCount.HasValue) entity.RemoteBanSyncCount = dto.RemoteBanSyncCount.Value;
            if (dto.RemoteExternalCount.HasValue) entity.RemoteExternalCount = dto.RemoteExternalCount.Value;

            if (dto.LegacyLastCheckUtc.HasValue) entity.LegacyLastCheckUtc = dto.LegacyLastCheckUtc.Value;
            if (dto.LegacyLastCheckResult is not null) entity.LegacyLastCheckResult = dto.LegacyLastCheckResult;
            if (dto.LegacyLastCheckErrorMessage is not null) entity.LegacyLastCheckErrorMessage = dto.LegacyLastCheckErrorMessage;
            if (dto.LegacyRemoteFilePath is not null) entity.LegacyRemoteFilePath = dto.LegacyRemoteFilePath;
            if (dto.LegacyResolvedForMod is not null) entity.LegacyResolvedForMod = dto.LegacyResolvedForMod;
            if (dto.LegacyRemoteFileSize.HasValue) entity.LegacyRemoteFileSize = dto.LegacyRemoteFileSize.Value;

            if (dto.LegacyLastPushUtc.HasValue) entity.LegacyLastPushUtc = dto.LegacyLastPushUtc.Value;
            if (dto.LegacyLastPushedETag is not null) entity.LegacyLastPushedEtag = dto.LegacyLastPushedETag;
            if (dto.LegacyLastPushedSize.HasValue) entity.LegacyLastPushedSize = dto.LegacyLastPushedSize.Value;

            if (dto.LegacyLastCentralBlobETag is not null) entity.LegacyLastCentralBlobEtag = dto.LegacyLastCentralBlobETag;
            if (dto.LegacyLastCentralBlobUtc.HasValue) entity.LegacyLastCentralBlobUtc = dto.LegacyLastCentralBlobUtc.Value;

            if (dto.LegacyRemoteTotalLineCount.HasValue) entity.LegacyRemoteTotalLineCount = dto.LegacyRemoteTotalLineCount.Value;
            if (dto.LegacyRemoteUntaggedCount.HasValue) entity.LegacyRemoteUntaggedCount = dto.LegacyRemoteUntaggedCount.Value;
            if (dto.LegacyRemoteBanSyncCount.HasValue) entity.LegacyRemoteBanSyncCount = dto.LegacyRemoteBanSyncCount.Value;
            if (dto.LegacyRemoteExternalCount.HasValue) entity.LegacyRemoteExternalCount = dto.LegacyRemoteExternalCount.Value;
        }
    }
}

