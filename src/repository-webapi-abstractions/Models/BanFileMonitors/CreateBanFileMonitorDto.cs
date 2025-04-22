using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.BanFileMonitors
{
    public record CreateBanFileMonitorDto : IDto
    {
        public CreateBanFileMonitorDto(Guid gameServerId, string filePath, GameType gameType)
        {
            GameServerId = gameServerId;
            FilePath = filePath;
            GameType = gameType;
        }

        [JsonProperty]
        public string FilePath { get; private set; }

        [JsonProperty]
        public Guid GameServerId { get; private set; }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; private set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(GameServerId), GameServerId.ToString() },
                    { nameof(GameType), GameType.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}
