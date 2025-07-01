using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles
{
    public record CreateUserProfileDto : IDto
    {
        public CreateUserProfileDto(string xtremeIdiotsForumId, string displayName, string email)
        {
            XtremeIdiotsForumId = xtremeIdiotsForumId;
            DisplayName = displayName;
            Email = email;
        }

        [JsonProperty]
        public string? IdentityOid { get; set; }

        [JsonProperty]
        public string? XtremeIdiotsForumId { get; private set; }

        [JsonProperty]
        public string? DemoAuthKey { get; set; }

        [JsonProperty]
        public string? DisplayName { get; private set; }

        [JsonProperty]
        public string? Title { get; set; }

        [JsonProperty]
        public string? FormattedName { get; set; }

        [JsonProperty]
        public string? PrimaryGroup { get; set; }

        [JsonProperty]
        public string? Email { get; private set; }

        [JsonProperty]
        public string? PhotoUrl { get; set; }

        [JsonProperty]
        public string? ProfileUrl { get; set; }

        [JsonProperty]
        public string? TimeZone { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(XtremeIdiotsForumId), XtremeIdiotsForumId is not null ? XtremeIdiotsForumId.ToString() : string.Empty }
                };

                return telemetryProperties;
            }
        }
    }
}
