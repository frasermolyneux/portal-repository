using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions
{
    /// <summary>
    /// Aggregate counts of currently-active bans for a game type, used by the
    /// ban file monitor dashboard to surface "is the server protected?" signals.
    /// </summary>
    public record ActiveBanCountsDto : IDto
    {
        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; internal set; }

        /// <summary>Active permanent bans (<see cref="AdminActionType.Ban"/>, no expiry or expiry in the future).</summary>
        [JsonProperty]
        public int ActivePermanentBanCount { get; internal set; }

        /// <summary>Active temporary bans (<see cref="AdminActionType.TempBan"/>, expiry in the future).</summary>
        [JsonProperty]
        public int ActiveTempBanCount { get; internal set; }

        /// <summary>Temporary bans that will expire within the next 24 hours.</summary>
        [JsonProperty]
        public int ExpiringTempBansNext24h { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(GameType), GameType.ToString() }
        };
    }
}
