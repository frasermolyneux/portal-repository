using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles
{
    public record UserProfileClaimDto : IDto
    {
        [JsonProperty]
        public Guid UserProfileClaimId { get; set; }

        [JsonProperty]
        public Guid UserProfileId { get; internal set; }

        [JsonProperty]
        public bool SystemGenerated { get; internal set; }

        [JsonProperty]
        public string ClaimType { get; internal set; } = string.Empty;

        [JsonProperty]
        public string ClaimValue { get; internal set; } = string.Empty;

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(UserProfileClaimId), UserProfileClaimId.ToString() },
                    { nameof(UserProfileId), UserProfileId.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}
