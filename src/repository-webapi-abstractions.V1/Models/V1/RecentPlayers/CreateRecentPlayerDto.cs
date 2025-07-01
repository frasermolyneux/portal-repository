using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.RecentPlayers
{
    public record CreateRecentPlayerDto : IDto
    {
        public CreateRecentPlayerDto(string name, GameType gameType, Guid playerId)
        {
            Name = name;
            GameType = gameType;
            PlayerId = playerId;
        }

        [JsonProperty]
        public string Name { get; private set; }

        [JsonProperty]
        public string? IpAddress { get; set; }

        [JsonProperty]
        public double? Lat { get; set; }

        [JsonProperty]
        public double? Long { get; set; }

        [JsonProperty]
        public string? CountryCode { get; set; }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; private set; }

        [JsonProperty]
        public Guid PlayerId { get; private set; }

        [JsonProperty]
        public Guid? GameServerId { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(GameType), GameType.ToString() },
                    { nameof(GameServerId), GameServerId is not null ? ((Guid)GameServerId).ToString() : string.Empty}
                };

                return telemetryProperties;
            }
        }
    }
}
