using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.BanFileMonitors;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.GameServers
{
    public record GameServerDto : IDto
    {
        [JsonProperty]
        public Guid GameServerId { get; internal set; }

        [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Title { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; internal set; }

        [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Hostname { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonProperty]
        public int QueryPort { get; internal set; }

        [JsonProperty]
        public string? FtpHostname { get; internal set; }

        [JsonProperty]
        public int? FtpPort { get; internal set; }

        [JsonProperty]
        public string? FtpUsername { get; internal set; }

        [JsonProperty]
        public string? FtpPassword { get; internal set; }

        [JsonProperty]
        public string? RconPassword { get; internal set; }

        [JsonProperty]
        public int ServerListPosition { get; internal set; }

        [JsonProperty]
        public string? HtmlBanner { get; internal set; }

        [JsonProperty]
        public bool BotEnabled { get; set; }

        [JsonProperty]
        public bool BannerServerListEnabled { get; internal set; }

        [JsonProperty]
        public bool PortalServerListEnabled { get; internal set; }

        [JsonProperty]
        public bool ChatLogEnabled { get; internal set; }

        [JsonProperty]
        public bool LiveTrackingEnabled { get; internal set; }

        [JsonProperty]
        public string? LiveTitle { get; internal set; }

        [JsonProperty]
        public string? LiveMap { get; internal set; }

        [JsonProperty]
        public string? LiveMod { get; internal set; }

        [JsonProperty]
        public string? LiveLogFile { get; internal set; }

        [JsonProperty]
        public int? LiveMaxPlayers { get; internal set; }

        [JsonProperty]
        public int? LiveCurrentPlayers { get; internal set; }

        [JsonProperty]
        public DateTime? LiveLastUpdated { get; internal set; }

        [JsonProperty]
        public bool Deleted { get; internal set; }

        [JsonProperty]
        public List<BanFileMonitorDto> BanFileMonitors { get; internal set; } = new List<BanFileMonitorDto>();

        [JsonProperty]
        public List<LivePlayerDto> LivePlayers { get; internal set; } = new List<LivePlayerDto>();

        public void ClearFtpCredentials()
        {
            FtpHostname = null;
            FtpUsername = null;
            FtpPassword = null;
        }

        public void ClearRconCredentials()
        {
            RconPassword = null;
        }

        public void ClearNoPermissionBanFileMonitors(GameType[] gameTypes, Guid[] banFileMonitorIds)
        {
            BanFileMonitors = BanFileMonitors.Where(bfm => bfm.GameServer != null && gameTypes.Contains(bfm.GameServer.GameType) || banFileMonitorIds.Contains(bfm.BanFileMonitorId)).ToList();
        }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(GameServerId), GameServerId.ToString() },
                    { nameof(GameType), GameType.ToString() },
                    { nameof(Title), Title }
                };

                return telemetryProperties;
            }
        }
    }
}