using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors
{
    public record BanFileMonitorDto : IDto
    {
        [JsonProperty]
        public Guid BanFileMonitorId { get; internal set; }

        [JsonProperty]
        public Guid GameServerId { get; internal set; }

        [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string FilePath { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonProperty]
        public long? RemoteFileSize { get; internal set; }

        [JsonProperty]
        public DateTime? LastSync { get; internal set; }

        [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GameServerDto GameServer { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(BanFileMonitorId), BanFileMonitorId.ToString() },
                    { nameof(GameServerId), GameServerId.ToString() }
                };

                if (GameServer is not null)
                    telemetryProperties.AddAdditionalProperties(GameServer.TelemetryProperties);

                return telemetryProperties;
            }
        }
    }
}
