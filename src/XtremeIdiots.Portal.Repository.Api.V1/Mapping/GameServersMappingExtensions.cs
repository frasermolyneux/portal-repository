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
                GameType = entity.GameType.ToGameType(),
                ServerListPosition = entity.ServerListPosition,
                AgentEnabled = entity.AgentEnabled,
                FtpEnabled = entity.FtpEnabled,
                RconEnabled = entity.RconEnabled,
                BanFileSyncEnabled = entity.BanFileSyncEnabled,
                BanFileRootPath = string.IsNullOrWhiteSpace(entity.BanFileRootPath) ? "/" : entity.BanFileRootPath,
                ServerListEnabled = entity.ServerListEnabled,
                Deleted = entity.Deleted,
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
                ServerListPosition = dto.ServerListPosition,
                AgentEnabled = dto.AgentEnabled,
                FtpEnabled = dto.FtpEnabled,
                RconEnabled = dto.RconEnabled,
                BanFileSyncEnabled = dto.BanFileSyncEnabled,
                BanFileRootPath = string.IsNullOrWhiteSpace(dto.BanFileRootPath) ? "/" : dto.BanFileRootPath,
                ServerListEnabled = dto.ServerListEnabled
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
            if (dto.ServerListPosition is not null) entity.ServerListPosition = dto.ServerListPosition.Value;
            if (dto.AgentEnabled is not null) entity.AgentEnabled = dto.AgentEnabled.Value;
            if (dto.FtpEnabled is not null) entity.FtpEnabled = dto.FtpEnabled.Value;
            if (dto.RconEnabled is not null) entity.RconEnabled = dto.RconEnabled.Value;
            if (dto.BanFileSyncEnabled is not null) entity.BanFileSyncEnabled = dto.BanFileSyncEnabled.Value;
            if (dto.BanFileRootPath is not null) entity.BanFileRootPath = string.IsNullOrWhiteSpace(dto.BanFileRootPath) ? "/" : dto.BanFileRootPath;
            if (dto.ServerListEnabled is not null) entity.ServerListEnabled = dto.ServerListEnabled.Value;
            if (dto.Deleted is not null) entity.Deleted = dto.Deleted.Value;
        }
    }
}