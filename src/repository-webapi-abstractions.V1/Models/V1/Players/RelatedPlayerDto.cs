using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Players
{
    public record RelatedPlayerDto : IDto
    {
        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; internal set; }

        [JsonProperty]
        public string Username { get; internal set; } = string.Empty;

        [JsonProperty]
        public Guid PlayerId { get; internal set; }

        [JsonProperty]
        public string IpAddress { get; internal set; } = string.Empty;

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(GameType), GameType.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}