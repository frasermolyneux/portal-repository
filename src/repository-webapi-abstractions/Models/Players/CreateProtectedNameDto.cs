using System.Text.Json.Serialization;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players
{
    /// <summary>
    /// DTO for creating a new protected name
    /// </summary>
    public record CreateProtectedNameDto : IDto
    {
        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public CreateProtectedNameDto()
        {
        }

        /// <summary>
        /// Create a new protected name request
        /// </summary>
        /// <param name="playerId">ID of player who owns this name</param>
        /// <param name="name">The name to protect</param>
        /// <param name="createdBy">ID or username of the creator</param>
        public CreateProtectedNameDto(Guid playerId, string name, Guid createdByUserProfileId)
        {
            PlayerId = playerId;
            Name = name;
            CreatedByUserProfileId = createdByUserProfileId;
        }

        /// <summary>
        /// The player ID that will own this protected name
        /// </summary>
        public Guid PlayerId { get; set; }

        /// <summary>
        /// The name to protect
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Who is creating the protected name
        /// </summary>
        public Guid CreatedByUserProfileId { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(CreatedByUserProfileId), CreatedByUserProfileId.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}