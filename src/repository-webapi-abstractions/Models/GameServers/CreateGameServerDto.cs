using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameServers
{
    public class CreateGameServerDto : IDto
    {
        public CreateGameServerDto(string title, GameType gameType, string hostname, int queryPort)
        {
            Title = title;
            GameType = gameType;
            Hostname = hostname;
            QueryPort = queryPort;
        }

        [JsonProperty]
        public string Title { get; set; }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; set; }

        [JsonProperty]
        public string Hostname { get; set; }

        [JsonProperty]
        public int QueryPort { get; set; }

        [JsonProperty]
        public string? FtpHostname { get; set; }

        [JsonProperty]
        public int? FtpPort { get; set; }

        [JsonProperty]
        public string? FtpUsername { get; set; }

        [JsonProperty]
        public string? FtpPassword { get; set; }

        [JsonProperty]
        public string? RconPassword { get; set; }

        [JsonProperty]
        public int ServerListPosition { get; set; }

        [JsonProperty]
        public string? HtmlBanner { get; set; }

        [JsonProperty]
        public bool BotEnabled { get; set; }

        [JsonProperty]
        public bool BannerServerListEnabled { get; set; }

        [JsonProperty]
        public bool PortalServerListEnabled { get; set; }

        [JsonProperty]
        public bool ChatLogEnabled { get; set; }

        [JsonProperty]
        public bool LiveTrackingEnabled { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(GameType), GameType.ToString() },
                    { nameof(Title), Title }
                };

                return telemetryProperties;
            }
        }
    }
}
