using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
{
    public record CreateLivePlayerDto : IDto
    {
        [JsonProperty]
        public string? Name { get; set; }

        [JsonProperty]
        public int Score { get; set; }

        [JsonProperty]
        public int Ping { get; set; }

        [JsonProperty]
        public int Num { get; set; }

        [JsonProperty]
        public int Rate { get; set; }

        [JsonProperty]
        public string? Team { get; set; }

        [JsonProperty]
        public TimeSpan Time { get; set; }

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
        public GameType GameType { get; set; }

        [JsonProperty]
        public Guid? PlayerId { get; set; }

        [JsonProperty]
        public Guid? GameServerId { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(GameType), GameType.ToString() },
                    { nameof(PlayerId), PlayerId is not null ? ((Guid)PlayerId).ToString() : string.Empty },
                    { nameof(GameServerId), GameServerId is not null ? ((Guid)GameServerId).ToString() : string.Empty }
                };

                return telemetryProperties;
            }
        }
    }
}
