using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Extensions;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.RecentPlayers
{
    public class RecentPlayerDto : IDto
    {
        [JsonProperty]
        public Guid RecentPlayerId { get; internal set; }

        [JsonProperty]
        public string? Name { get; internal set; }

        [JsonProperty]
        public string? IpAddress { get; internal set; }

        [JsonProperty]
        public double? Lat { get; internal set; }

        [JsonProperty]
        public double? Long { get; internal set; }

        [JsonProperty]
        public string? CountryCode { get; internal set; }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; internal set; }

        [JsonProperty]
        public Guid? PlayerId { get; internal set; }

        [JsonProperty]
        public Guid? GameServerId { get; internal set; }

        [JsonProperty]
        public DateTime Timestamp { get; internal set; }

        [JsonProperty]
        public PlayerDto? Player { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(RecentPlayerId), RecentPlayerId.ToString() },
                    { nameof(GameType), GameType.ToString() },
                    { nameof(PlayerId), PlayerId is not null ? ((Guid)PlayerId).ToString() : string.Empty },
                    { nameof(GameServerId), GameServerId is not null ? ((Guid)GameServerId).ToString() : string.Empty}
                };

                if (Player is not null)
                    telemetryProperties.AddAdditionalProperties(telemetryProperties);

                return telemetryProperties;
            }
        }
    }
}
