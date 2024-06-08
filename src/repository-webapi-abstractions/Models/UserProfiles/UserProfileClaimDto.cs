using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.UserProfiles
{
    public class UserProfileClaimDto : IDto
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
