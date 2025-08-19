using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    public static class GameServersMappingExtensions
    {
        public static GameServerDto ToDto(this GameServer entity)
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
                BannerServerListPosition = entity.BannerServerListPosition,
                LiveTitle = entity.LiveTitle,
                LiveMod = entity.LiveMod,
                LiveMap = entity.LiveMap,
                LiveMaxPlayers = entity.LiveMaxPlayers,
                LiveCurrentPlayers = entity.LiveCurrentPlayers,
                LiveLastUpdated = entity.LiveLastUpdated,
                LiveQueryFailed = entity.LiveQueryFailed
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

        public static GameServerStatDto ToDto(this GameServerStat entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            return new GameServerStatDto
            {
                GameServerStatId = entity.GameServerStatId,
                GameServerId = entity.GameServerId,
                Timestamp = entity.Timestamp,
                PlayerCount = entity.PlayerCount,
                MapName = entity.MapName ?? string.Empty,
                ModName = entity.ModName ?? string.Empty
            };
        }

        public static GameServerStat ToEntity(this CreateGameServerStatDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return new GameServerStat
            {
                GameServerId = dto.GameServerId,
                Timestamp = dto.Timestamp,
                PlayerCount = dto.PlayerCount,
                MapName = dto.MapName,
                ModName = dto.ModName
            };
        }
    }
}