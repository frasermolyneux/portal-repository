using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.GameServers
{
    public record GameServerEventDto : IDto
    {
        [JsonProperty]
        public Guid GameServerEventId { get; internal set; }

        [JsonProperty]
        public Guid GameServerId { get; internal set; }

        [JsonProperty]
        public DateTime Timestamp { get; internal set; }

        [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string EventType { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonProperty]
        public string? EventData { get; internal set; }

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
                    { nameof(GameServerEventId), GameServerEventId.ToString() },
                    { nameof(GameServerId), GameServerId.ToString() }
                };

                if (GameServer is not null)
                    telemetryProperties.AddAdditionalProperties(GameServer.TelemetryProperties);

                return telemetryProperties;
            }
        }
    }
}