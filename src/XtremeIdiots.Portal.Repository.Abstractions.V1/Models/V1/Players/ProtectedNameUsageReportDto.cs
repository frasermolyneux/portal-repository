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
        public ProtectedNameDto ProtectedName { get; init; } = new ProtectedNameDto();

        /// <summary>
        /// The player who owns this protected name
        /// </summary>
        [JsonProperty]
        public PlayerDto OwningPlayer { get; init; } = new PlayerDto();

        /// <summary>
        /// List of players using this name and usage statistics
        /// </summary>
        [JsonProperty]
        public List<PlayerUsageDto> UsageInstances { get; init; } = [];

        /// <summary>
        /// Represents a player's usage of a protected name
        /// </summary>
        public record PlayerUsageDto
        {
            /// <summary>
            /// ID of the player using this name
            /// </summary>
            [JsonProperty]
            public Guid PlayerId { get; init; }

            /// <summary>
            /// Username of the player
            /// </summary>
            [JsonProperty]
            public string Username { get; init; } = string.Empty;

            /// <summary>
            /// Whether this player is the legitimate owner of the name
            /// </summary>
            [JsonProperty]
            public bool IsOwner { get; init; }

            /// <summary>
            /// When the player last used this name
            /// </summary>
            [JsonProperty]
            public DateTime LastUsed { get; init; }

            /// <summary>
            /// How many times this player has used this name
            /// </summary>
            [JsonProperty]
            public int UsageCount { get; init; }
        }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => [];
    }
}