using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
{
    /// <summary>
    /// DTO containing usage report data for a protected name
    /// </summary>
    public record ProtectedNameUsageReportDto : IDto
    {
        /// <summary>
        /// The protected name details
        /// </summary>
        [JsonProperty]
        public ProtectedNameDto ProtectedName { get; set; } = new ProtectedNameDto();

        /// <summary>
        /// The player who owns this protected name
        /// </summary>
        [JsonProperty]
        public PlayerDto OwningPlayer { get; set; } = new PlayerDto();

        /// <summary>
        /// List of players using this name and usage statistics
        /// </summary>
        [JsonProperty]
        public List<PlayerUsageDto> UsageInstances { get; set; } = new List<PlayerUsageDto>();

        /// <summary>
        /// Represents a player's usage of a protected name
        /// </summary>
        public record PlayerUsageDto
        {
            /// <summary>
            /// ID of the player using this name
            /// </summary>
            [JsonProperty]
            public Guid PlayerId { get; set; }

            /// <summary>
            /// Username of the player
            /// </summary>
            [JsonProperty]
            public string Username { get; set; } = string.Empty;

            /// <summary>
            /// Whether this player is the legitimate owner of the name
            /// </summary>
            [JsonProperty]
            public bool IsOwner { get; set; }

            /// <summary>
            /// When the player last used this name
            /// </summary>
            [JsonProperty]
            public DateTime LastUsed { get; set; }

            /// <summary>
            /// How many times this player has used this name
            /// </summary>
            [JsonProperty]
            public int UsageCount { get; set; }
        }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string> { };

                return telemetryProperties;
            }
        }
    }
}