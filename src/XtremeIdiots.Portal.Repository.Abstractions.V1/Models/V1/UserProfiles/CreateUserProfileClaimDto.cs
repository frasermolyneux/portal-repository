using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles
{
    public record CreateUserProfileClaimDto : IDto
    {
        public CreateUserProfileClaimDto(Guid userProfileId, string claimType, string claimValue, bool systemGenerated)
        {
            UserProfileId = userProfileId;
            ClaimType = claimType;
            ClaimValue = claimValue;
            SystemGenerated = systemGenerated;
        }

        [JsonProperty]
        public Guid UserProfileId { get; private set; }

        [JsonProperty]
        public bool SystemGenerated { get; private set; }

        [JsonProperty]
        public string ClaimType { get; private set; }

        [JsonProperty]
        public string ClaimValue { get; private set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(UserProfileId), UserProfileId.ToString() }
        };
    }
}
