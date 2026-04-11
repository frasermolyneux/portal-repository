using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers
{
    public record EditGameServerDto : IDto
    {
        public EditGameServerDto(Guid gameServerId)
        {
            GameServerId = gameServerId;
        }

        [JsonProperty]
        public Guid GameServerId { get; private set; }

        [JsonProperty]
        public string? Title { get; set; }

        [JsonProperty]
        public string? Hostname { get; set; }

        [JsonProperty]
        public int? QueryPort { get; set; }

        [JsonProperty]
        public int? ServerListPosition { get; set; }

        [JsonProperty]
        public bool? AgentEnabled { get; set; }

        [JsonProperty]
        public bool? FtpEnabled { get; set; }

        [JsonProperty]
        public bool? RconEnabled { get; set; }

        [JsonProperty]
        public bool? BanFileSyncEnabled { get; set; }

        [JsonProperty]
        public bool? ServerListEnabled { get; set; }

        [JsonProperty]
        public bool? Deleted { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(GameServerId), GameServerId.ToString() },
            { nameof(Title), Title is not null ? Title : string.Empty }
        };
    }
}
