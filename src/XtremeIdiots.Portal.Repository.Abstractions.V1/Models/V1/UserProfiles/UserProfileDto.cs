using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles
{
    public record UserProfileDto : IDto
    {
        [JsonProperty]
        public Guid UserProfileId { get; internal set; }

        [JsonProperty]
        public string? IdentityOid { get; internal set; }

        [JsonProperty]
        public string? XtremeIdiotsForumId { get; internal set; }

        [JsonProperty]
        public string? DemoAuthKey { get; set; }

        [JsonProperty]
        public string? DisplayName { get; internal set; }

        [JsonProperty]
        public string? FormattedName { get; internal set; }

        [JsonProperty]
        public string? PrimaryGroup { get; internal set; }

        [JsonProperty]
        public string? Email { get; internal set; }

        [JsonProperty]
        public string? PhotoUrl { get; internal set; }

        [JsonProperty]
        public string? ProfileUrl { get; internal set; }

        [JsonProperty]
        public string? TimeZone { get; internal set; }

        [JsonProperty]
        public List<UserProfileClaimDto> UserProfileClaims { get; set; } = [];

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(UserProfileId), UserProfileId.ToString() },
                    { nameof(IdentityOid), IdentityOid is not null ? IdentityOid.ToString() : string.Empty},
                    { nameof(XtremeIdiotsForumId), XtremeIdiotsForumId is not null ? XtremeIdiotsForumId.ToString() : string.Empty},
                    { nameof(DisplayName), DisplayName is not null ? DisplayName.ToString() : string.Empty}
                };

                return telemetryProperties;
            }
        }
    }
}
