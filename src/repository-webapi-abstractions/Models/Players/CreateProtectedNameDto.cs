using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players
{
    /// <summary>
    /// DTO for creating a new protected name
    /// </summary>
    public record CreateProtectedNameDto : IDto
    {
        /// <summary>
        /// Create a new protected name request
        /// </summary>
        /// <param name="playerId">ID of player who owns this name</param>
        /// <param name="name">The name to protect</param>
        /// <param name="adminId">ID of the creator</param>
        public CreateProtectedNameDto(Guid playerId, string name, string adminId)
        {
            PlayerId = playerId;
            Name = name;
            AdminId = adminId;
        }

        /// <summary>
        /// The player ID that will own this protected name
        /// </summary>
        [JsonProperty]
        public Guid PlayerId { get; set; }

        /// <summary>
        /// The name to protect
        /// </summary>
        [JsonProperty]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Who is creating the protected name
        /// </summary>
        [JsonProperty]
        public string AdminId { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(AdminId), AdminId.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}