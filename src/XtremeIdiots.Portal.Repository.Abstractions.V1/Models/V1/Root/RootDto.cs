using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.V1.Models.Root
{
    /// <summary>
    /// Root API response DTO
    /// </summary>
    public record RootDto : IDto
    {
        /// <summary>
        /// API name
        /// </summary>
        [JsonProperty]
        public string Name { get; set; } = "Portal Repository API";

        /// <summary>
        /// API version
        /// </summary>
        [JsonProperty]
        public string Version { get; set; } = "v1.1";

        /// <summary>
        /// API description
        /// </summary>
        [JsonProperty]
        public string Description { get; set; } = "XtremeIdiots Portal Repository API";

        /// <summary>
        /// API documentation URL
        /// </summary>
        [JsonProperty]
        public string? DocumentationUrl { get; set; }

        /// <summary>
        /// Server timestamp
        /// </summary>
        [JsonProperty]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(Name), Name },
                    { nameof(Version), Version }
                };

                return telemetryProperties;
            }
        }
    }
}
