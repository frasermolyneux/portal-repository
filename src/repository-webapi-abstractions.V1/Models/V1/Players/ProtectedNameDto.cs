using Newtonsoft.Json;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.UserProfiles;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Players
{
    /// <summary>
    /// Represents a protected name that belongs to a player
    /// </summary>
    public record ProtectedNameDto : IDto
    {
        /// <summary>
        /// Unique identifier for the protected name
        /// </summary>
        [JsonProperty]
        public Guid ProtectedNameId { get; set; }

        /// <summary>
        /// The player ID that owns this protected name
        /// </summary>

        [JsonProperty]
        public Guid PlayerId { get; set; }

        /// <summary>
        /// The protected name string
        /// </summary>
        [JsonProperty]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// When the protected name was created
        /// </summary>
        [JsonProperty]
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Who created the protected name
        /// </summary>
        [JsonProperty]
        public Guid CreatedByUserProfileId { get; set; }

        [JsonProperty]
        public UserProfileDto? CreatedByUserProfile { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(ProtectedNameId), ProtectedNameId.ToString() },
                    { nameof(PlayerId), PlayerId.ToString() }
                };

                if (CreatedByUserProfile is not null)
                    telemetryProperties.AddAdditionalProperties(CreatedByUserProfile.TelemetryProperties);

                return telemetryProperties;
            }
        }
    }
}