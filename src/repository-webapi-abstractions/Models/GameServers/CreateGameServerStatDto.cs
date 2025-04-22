using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameServers
{
    public record CreateGameServerStatDto : IDto
    {
        public CreateGameServerStatDto(Guid gameServerId, int playerCount, string mapName)
        {
            GameServerId = gameServerId;
            PlayerCount = playerCount;
            MapName = mapName;
        }

        [JsonProperty]
        public Guid GameServerId { get; private set; }

        [JsonProperty]
        public int PlayerCount { get; private set; }

        [JsonProperty]
        public string MapName { get; private set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(GameServerId), GameServerId.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}
