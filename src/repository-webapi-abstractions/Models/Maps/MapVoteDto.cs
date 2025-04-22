using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Extensions;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameServers;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Maps
{
    public record MapVoteDto : IDto
    {
        [JsonProperty]
        public Guid MapVoteId { get; internal set; }

        [JsonProperty]
        public Guid MapId { get; internal set; }

        [JsonProperty]
        public Guid PlayerId { get; internal set; }

        [JsonProperty]
        public Guid? GameServerId { get; internal set; }

        [JsonProperty]
        public bool Like { get; internal set; }

        [JsonProperty]
        public DateTime Timestamp { get; internal set; }

        [JsonProperty]
        public GameServerDto? GameServer { get; internal set; }

        [JsonProperty]
        public MapDto? Map { get; internal set; }

        [JsonProperty]
        public PlayerDto? Player { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(MapVoteId), MapVoteId.ToString() },
                    { nameof(MapId), MapId.ToString() },
                    { nameof(PlayerId), PlayerId.ToString() }
                };

                if (GameServer is not null)
                    telemetryProperties.AddAdditionalProperties(GameServer.TelemetryProperties);

                if (Map is not null)
                    telemetryProperties.AddAdditionalProperties(Map.TelemetryProperties);

                if (Player is not null)
                    telemetryProperties.AddAdditionalProperties(Player.TelemetryProperties);

                return telemetryProperties;
            }
        }
    }
}
