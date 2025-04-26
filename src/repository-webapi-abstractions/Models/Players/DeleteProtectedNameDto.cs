using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players
{
    /// <summary>
    /// DTO for deleting a protected name
    /// </summary>
    public record DeleteProtectedNameDto : IDto
    {
        /// <summary>
        /// Create a delete protected name request
        /// </summary>
        /// <param name="protectedNameId">ID of the protected name to delete</param>
        public DeleteProtectedNameDto(Guid protectedNameId)
        {
            ProtectedNameId = protectedNameId;
        }

        /// <summary>
        /// The ID of the protected name to delete
        /// </summary>
        [JsonProperty]
        public Guid ProtectedNameId { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(ProtectedNameId), ProtectedNameId.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}