using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
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

        [JsonProperty]
        public DateTime LastSeen { get; internal set; }

        [JsonProperty]
        public bool HasActiveBan { get; internal set; }

        [JsonProperty]
        public int AdminActionCount { get; internal set; }

        /// <summary>
        /// The most recently used IP address that links the viewed player to this related player.
        /// </summary>
        [JsonProperty]
        public string LinkingIpAddress { get; internal set; } = string.Empty;

        /// <summary>
        /// When the viewed player last used the linking IP address.
        /// </summary>
        [JsonProperty]
        public DateTime LinkingIpLastUsedByPlayer { get; internal set; }

        /// <summary>
        /// When this related player last used the linking IP address.
        /// </summary>
        [JsonProperty]
        public DateTime LinkingIpLastUsedByRelated { get; internal set; }

        /// <summary>
        /// Whether the linking IP is the viewed player's current IP address.
        /// </summary>
        [JsonProperty]
        public bool IsCurrentIp { get; internal set; }

        /// <summary>
        /// Total number of distinct IP addresses shared between the viewed player and this related player.
        /// </summary>
        [JsonProperty]
        public int SharedIpCount { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(PlayerId), PlayerId.ToString() },
            { nameof(GameType), GameType.ToString() }
        };
    }
}