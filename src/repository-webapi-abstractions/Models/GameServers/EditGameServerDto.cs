using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameServers
{
    public class EditGameServerDto : IDto
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
        public int? ServerListPosition { get; set; }

        [JsonProperty]
        public string? HtmlBanner { get; set; }

        [JsonProperty]
        public bool? BotEnabled { get; set; }

        [JsonProperty]
        public bool? BannerServerListEnabled { get; set; }

        [JsonProperty]
        public bool? PortalServerListEnabled { get; set; }

        [JsonProperty]
        public bool? ChatLogEnabled { get; set; }

        [JsonProperty]
        public bool? LiveTrackingEnabled { get; set; }

        [JsonProperty]
        public string? LiveTitle { get; set; }

        [JsonProperty]
        public string? LiveMap { get; set; }

        [JsonProperty]
        public string? LiveMod { get; set; }

        [JsonProperty]
        public int? LiveMaxPlayers { get; set; }

        [JsonProperty]
        public int? LiveCurrentPlayers { get; set; }

        [JsonProperty]
        public DateTime? LiveLastUpdated { get; set; }

        [JsonProperty]
        public bool? Deleted { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(GameServerId), GameServerId.ToString() },
                    { nameof(Title), Title is not null ? Title : string.Empty }
                };

                return telemetryProperties;
            }
        }
    }
}
