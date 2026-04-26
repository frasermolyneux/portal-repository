using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers
{
    public record CreateGameServerDto : IDto
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
        public int ServerListPosition { get; set; }

        [JsonProperty]
        public bool AgentEnabled { get; set; }

        [JsonProperty]
        public bool FtpEnabled { get; set; }

        [JsonProperty]
        public bool RconEnabled { get; set; }

        [JsonProperty]
        public bool BanFileSyncEnabled { get; set; }

        [JsonProperty]
        public string BanFileRootPath { get; set; } = "/";

        [JsonProperty]
        public bool ServerListEnabled { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(GameType), GameType.ToString() },
            { nameof(Title), Title }
        };
    }
}
