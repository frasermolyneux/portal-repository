using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    public static class GameServersMappingExtensions
    {
        public static GameServerDto ToDto(this GameServer entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var (fileTransportEnabled, fileTransportType) = ResolveFileTransportState(entity);

            return new GameServerDto
            {
                GameServerId = entity.GameServerId,
                Title = entity.Title ?? string.Empty,
                Hostname = entity.Hostname ?? string.Empty,
                QueryPort = entity.QueryPort,
                GameType = entity.GameType.ToGameType(),
                ServerListPosition = entity.ServerListPosition,
                AgentEnabled = entity.AgentEnabled,
                FileTransportEnabled = fileTransportEnabled,
                FileTransportType = fileTransportType,
                FtpEnabled = fileTransportEnabled && fileTransportType == FileTransportType.Ftp,
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

            var entity = new GameServer
            {
                Title = dto.Title,
                Hostname = dto.Hostname,
                QueryPort = dto.QueryPort,
                GameType = dto.GameType.ToGameTypeInt(),
                ServerListPosition = dto.ServerListPosition,
                AgentEnabled = dto.AgentEnabled,
                RconEnabled = dto.RconEnabled,
                BanFileSyncEnabled = dto.BanFileSyncEnabled,
                BanFileRootPath = string.IsNullOrWhiteSpace(dto.BanFileRootPath) ? "/" : dto.BanFileRootPath,
                ServerListEnabled = dto.ServerListEnabled
            };

            ApplyCreateFileTransport(dto, entity);

            return entity;
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

            var hasTransportEnabled = dto.FileTransportEnabled is not null;
            var hasTransportType = dto.FileTransportType is not null;
            var hasTransportFields = hasTransportEnabled || hasTransportType;

            if (dto.Title is not null)
            {
                entity.Title = dto.Title;
            }

            if (dto.Hostname is not null)
            {
                entity.Hostname = dto.Hostname;
            }

            if (dto.QueryPort is not null)
            {
                entity.QueryPort = dto.QueryPort.Value;
            }

            if (dto.ServerListPosition is not null)
            {
                entity.ServerListPosition = dto.ServerListPosition.Value;
            }

            if (dto.AgentEnabled is not null)
            {
                entity.AgentEnabled = dto.AgentEnabled.Value;
            }

            if (dto.FileTransportEnabled is not null)
            {
                entity.FileTransportEnabled = dto.FileTransportEnabled.Value;
            }

            if (dto.FileTransportType is not null)
            {
                entity.FileTransportType = (int)dto.FileTransportType.Value;
            }

            if (dto.FileTransportType is not null && dto.FileTransportEnabled is null)
            {
                entity.FileTransportEnabled = entity.FileTransportEnabled || entity.FileTransportType != (int)FileTransportType.Unknown;
            }

            if (dto.FileTransportEnabled == true && dto.FileTransportType is null && entity.FileTransportType == (int)FileTransportType.Unknown)
            {
                entity.FileTransportType = (int)FileTransportType.Ftp;
            }

            if (hasTransportFields)
            {
                var fileTransportType = NormalizeFileTransportType(entity.FileTransportType, entity.FileTransportEnabled);
                entity.FileTransportType = (int)fileTransportType;
                entity.FtpEnabled = entity.FileTransportEnabled && fileTransportType == FileTransportType.Ftp;
            }
            else
            {
                var currentType = NormalizeFileTransportType(entity.FileTransportType, entity.FileTransportEnabled);
                entity.FileTransportType = (int)currentType;
                entity.FtpEnabled = entity.FileTransportEnabled && currentType == FileTransportType.Ftp;
            }

            if (dto.RconEnabled is not null)
            {
                entity.RconEnabled = dto.RconEnabled.Value;
            }

            if (dto.BanFileSyncEnabled is not null)
            {
                entity.BanFileSyncEnabled = dto.BanFileSyncEnabled.Value;
            }

            if (dto.BanFileRootPath is not null)
            {
                entity.BanFileRootPath = string.IsNullOrWhiteSpace(dto.BanFileRootPath) ? "/" : dto.BanFileRootPath;
            }

            if (dto.ServerListEnabled is not null)
            {
                entity.ServerListEnabled = dto.ServerListEnabled.Value;
            }

            if (dto.Deleted is not null)
            {
                entity.Deleted = dto.Deleted.Value;
            }
        }

        private static void ApplyCreateFileTransport(CreateGameServerDto dto, GameServer entity)
        {
            var fileTransportEnabled = dto.FileTransportEnabled ?? false;
            var fileTransportType = dto.FileTransportType ?? (fileTransportEnabled ? FileTransportType.Ftp : FileTransportType.Unknown);

            fileTransportType = NormalizeFileTransportType((int)fileTransportType, fileTransportEnabled);

            entity.FileTransportEnabled = fileTransportEnabled;
            entity.FileTransportType = (int)fileTransportType;
            entity.FtpEnabled = fileTransportEnabled && fileTransportType == FileTransportType.Ftp;
        }

        private static (bool FileTransportEnabled, FileTransportType FileTransportType) ResolveFileTransportState(GameServer entity)
        {
            var fileTransportEnabled = entity.FileTransportEnabled;
            var fileTransportType = NormalizeFileTransportType(entity.FileTransportType, fileTransportEnabled);
            return (fileTransportEnabled, fileTransportType);
        }

        private static FileTransportType NormalizeFileTransportType(int rawType, bool fileTransportEnabled)
        {
            if (Enum.IsDefined(typeof(FileTransportType), rawType) && rawType != (int)FileTransportType.Unknown)
            {
                return (FileTransportType)rawType;
            }

            return fileTransportEnabled ? FileTransportType.Ftp : FileTransportType.Unknown;
        }
    }
}