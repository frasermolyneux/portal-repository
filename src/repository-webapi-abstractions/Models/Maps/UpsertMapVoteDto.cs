using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Maps
{
    public record UpsertMapVoteDto : IDto
    {
        public UpsertMapVoteDto(Guid mapId, Guid playerId, Guid gameServerId, bool like)
        {
            MapId = mapId;
            PlayerId = playerId;
            GameServerId = gameServerId;
            Like = like;
        }

        [JsonProperty]
        public Guid MapId { get; private set; }

        [JsonProperty]
        public Guid PlayerId { get; private set; }

        [JsonProperty]
        public Guid GameServerId { get; private set; }

        [JsonProperty]
        public bool Like { get; private set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(MapId), MapId.ToString() },
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(GameServerId), GameServerId.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}
