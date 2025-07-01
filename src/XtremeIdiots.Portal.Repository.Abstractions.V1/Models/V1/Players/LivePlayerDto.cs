using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
{
    public record LivePlayerDto : IDto
    {
        [JsonProperty]
        public Guid LivePlayerId { get; internal set; }

        [JsonProperty]
        public string? Name { get; internal set; }

        [JsonProperty]
        public int Score { get; internal set; }

        [JsonProperty]
        public int Ping { get; internal set; }

        [JsonProperty]
        public int Num { get; internal set; }

        [JsonProperty]
        public int Rate { get; internal set; }

        [JsonProperty]
        public string? Team { get; internal set; }

        [JsonProperty]
        public TimeSpan Time { get; internal set; }

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
        public Guid? GameServerServerId { get; internal set; }

        [JsonProperty]
        public PlayerDto? Player { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(LivePlayerId), LivePlayerId.ToString() },
                    { nameof(GameType), GameType.ToString() },
                    { nameof(PlayerId), PlayerId is not null ? ((Guid)PlayerId).ToString() : string.Empty },
                    { nameof(GameServerServerId), GameServerServerId is not null ? ((Guid)GameServerServerId).ToString() : string.Empty },
                };

                if (Player is not null)
                    telemetryProperties.AddAdditionalProperties(Player.TelemetryProperties);

                return telemetryProperties;
            }
        }
    }
}
