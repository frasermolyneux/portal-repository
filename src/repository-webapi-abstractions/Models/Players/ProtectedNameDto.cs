using System.Text.Json.Serialization;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players
{
    /// <summary>
    /// Represents a protected name that belongs to a player
    /// </summary>
    public record ProtectedNameDto : IDto
    {
        /// <summary>
        /// Unique identifier for the protected name
        /// </summary>
        public Guid ProtectedNameId { get; set; }

        /// <summary>
        /// The player ID that owns this protected name
        /// </summary>
        public Guid PlayerId { get; set; }

        /// <summary>
        /// The protected name string
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// When the protected name was created
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Who created the protected name
        /// </summary>
        public Guid CreatedByUserProfileId { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(ProtectedNameId), ProtectedNameId.ToString() },
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(CreatedByUserProfileId), CreatedByUserProfileId.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}