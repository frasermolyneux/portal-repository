using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameServers
{
    public record CreateGameServerEventDto : IDto
    {
        public CreateGameServerEventDto(Guid gameServerId, string eventType, string eventData)
        {
            GameServerId = gameServerId;
            EventType = eventType;
            EventData = eventData;
        }

        [JsonProperty]
        public Guid GameServerId { get; set; }

        [JsonProperty]
        public string EventType { get; set; }

        [JsonProperty]
        public string EventData { get; set; }

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
