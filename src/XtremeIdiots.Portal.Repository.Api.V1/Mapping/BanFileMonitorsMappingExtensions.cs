using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for BanFileMonitor entities and DTOs.
    /// </summary>
    public static class BanFileMonitorsMappingExtensions
    {
        /// <summary>
        /// Maps a BanFileMonitor entity to a BanFileMonitorDto.
        /// NOTE: GameServer navigation property will be null to avoid circular dependencies.
        /// </summary>
        /// <param name="entity">The BanFileMonitor entity to map from.</param>
        /// <returns>The mapped BanFileMonitorDto.</returns>
        public static BanFileMonitorDto ToDto(this BanFileMonitor entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new BanFileMonitorDto
            {
                BanFileMonitorId = entity.BanFileMonitorId,
                GameServerId = entity.GameServerId,
                FilePath = entity.FilePath ?? string.Empty,
                RemoteFileSize = entity.RemoteFileSize,
                LastSync = entity.LastSync,
                GameServer = expand && entity.GameServer != null ? entity.GameServer.ToDto(false) : null!
            };
        }

        /// <summary>
        /// Maps a CreateBanFileMonitorDto to a BanFileMonitor entity.
        /// </summary>
        /// <param name="dto">The CreateBanFileMonitorDto to map from.</param>
        /// <returns>The mapped BanFileMonitor entity.</returns>
        public static BanFileMonitor ToEntity(this CreateBanFileMonitorDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new BanFileMonitor
            {
                GameServerId = dto.GameServerId,
                FilePath = dto.FilePath,
                RemoteFileSize = null,
                LastSync = null
            };
        }

        /// <summary>
        /// Applies the values from an EditBanFileMonitorDto to an existing BanFileMonitor entity,
        /// preserving null-handling behavior (only updates non-null values).
        /// </summary>
        /// <param name="dto">The EditBanFileMonitorDto containing the updates.</param>
        /// <param name="entity">The existing BanFileMonitor entity to update.</param>
        public static void ApplyTo(this EditBanFileMonitorDto dto, BanFileMonitor entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            if (dto.FilePath is not null) entity.FilePath = dto.FilePath;
            if (dto.RemoteFileSize.HasValue) entity.RemoteFileSize = dto.RemoteFileSize.Value;
            if (dto.LastSync.HasValue) entity.LastSync = dto.LastSync.Value;
        }
    }
}