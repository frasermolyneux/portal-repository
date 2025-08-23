using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    public static class GameServersMappingExtensions
    {
        public static GameServerDto ToDto(this GameServer entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return new GameServerDto
            {
                GameServerId = entity.GameServerId,
                Title = entity.Title ?? string.Empty,
                Hostname = entity.Hostname ?? string.Empty,
                QueryPort = entity.QueryPort,
                FtpHostname = entity.FtpHostname,
                FtpPort = entity.FtpPort,
                FtpUsername = entity.FtpUsername,
                FtpPassword = entity.FtpPassword,
                RconPassword = entity.RconPassword,
                GameType = entity.GameType.ToGameType(),
                HtmlBanner = entity.HtmlBanner,
                ServerListPosition = entity.ServerListPosition,
                BotEnabled = entity.BotEnabled,
                BannerServerListEnabled = entity.BannerServerListEnabled,
                PortalServerListEnabled = entity.PortalServerListEnabled,
                ChatLogEnabled = entity.ChatLogEnabled,
                LiveTrackingEnabled = entity.LiveTrackingEnabled,
                LiveTitle = entity.LiveTitle,
                LiveMod = entity.LiveMod,
                LiveMap = entity.LiveMap,
                LiveLogFile = entity.LiveLogFile,
                LiveMaxPlayers = entity.LiveMaxPlayers,
                LiveCurrentPlayers = entity.LiveCurrentPlayers,
                LiveLastUpdated = entity.LiveLastUpdated,
                Deleted = entity.Deleted
            };
        }

        public static GameServer ToEntity(this CreateGameServerDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return new GameServer
            {
                Title = dto.Title,
                Hostname = dto.Hostname,
                QueryPort = dto.QueryPort,
                GameType = dto.GameType.ToGameTypeInt(),
                FtpHostname = dto.FtpHostname,
                FtpPort = dto.FtpPort,
                FtpUsername = dto.FtpUsername,
                FtpPassword = dto.FtpPassword,
                RconPassword = dto.RconPassword
            };
        }

        public static GameServerStatDto ToDto(this GameServerStat entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return new GameServerStatDto
            {
                GameServerStatId = entity.GameServerStatId,
                GameServerId = entity.GameServerId ?? Guid.Empty,
                Timestamp = entity.Timestamp,
                PlayerCount = entity.PlayerCount,
                MapName = entity.MapName ?? string.Empty
            };
        }

        public static GameServerStat ToEntity(this CreateGameServerStatDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return new GameServerStat
            {
                GameServerId = dto.GameServerId,
                PlayerCount = dto.PlayerCount,
                MapName = dto.MapName,
                Timestamp = DateTime.UtcNow
            };
        }

        public static void ApplyTo(this EditGameServerDto dto, GameServer entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            if (dto.Title is not null) entity.Title = dto.Title;
            if (dto.Hostname is not null) entity.Hostname = dto.Hostname;
            if (dto.QueryPort is not null) entity.QueryPort = dto.QueryPort.Value;
            if (dto.FtpHostname is not null) entity.FtpHostname = dto.FtpHostname;
            if (dto.FtpPort is not null) entity.FtpPort = dto.FtpPort;
            if (dto.FtpUsername is not null) entity.FtpUsername = dto.FtpUsername;
            if (dto.FtpPassword is not null) entity.FtpPassword = dto.FtpPassword;
            if (dto.RconPassword is not null) entity.RconPassword = dto.RconPassword;
            if (dto.ServerListPosition is not null) entity.ServerListPosition = dto.ServerListPosition.Value;
            if (dto.HtmlBanner is not null) entity.HtmlBanner = dto.HtmlBanner;
            if (dto.BotEnabled is not null) entity.BotEnabled = dto.BotEnabled.Value;
            if (dto.BannerServerListEnabled is not null) entity.BannerServerListEnabled = dto.BannerServerListEnabled.Value;
            if (dto.PortalServerListEnabled is not null) entity.PortalServerListEnabled = dto.PortalServerListEnabled.Value;
            if (dto.ChatLogEnabled is not null) entity.ChatLogEnabled = dto.ChatLogEnabled.Value;
            if (dto.LiveTrackingEnabled is not null) entity.LiveTrackingEnabled = dto.LiveTrackingEnabled.Value;
            if (dto.LiveTitle is not null) entity.LiveTitle = dto.LiveTitle;
            if (dto.LiveMap is not null) entity.LiveMap = dto.LiveMap;
            if (dto.LiveMod is not null) entity.LiveMod = dto.LiveMod;
            if (dto.LiveLogFile is not null) entity.LiveLogFile = dto.LiveLogFile;
            if (dto.LiveMaxPlayers is not null) entity.LiveMaxPlayers = dto.LiveMaxPlayers;
            if (dto.LiveCurrentPlayers is not null) entity.LiveCurrentPlayers = dto.LiveCurrentPlayers;
            if (dto.LiveLastUpdated is not null) entity.LiveLastUpdated = dto.LiveLastUpdated;
            if (dto.Deleted is not null) entity.Deleted = dto.Deleted.Value;
        }
    }
}