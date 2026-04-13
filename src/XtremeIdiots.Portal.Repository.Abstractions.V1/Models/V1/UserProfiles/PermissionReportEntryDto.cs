using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles
{
    public record PermissionReportEntryDto : IDto
    {
        [JsonProperty]
        public Guid UserProfileId { get; set; }

        [JsonProperty]
        public string DisplayName { get; set; } = string.Empty;

        [JsonProperty]
        public string? XtremeIdiotsForumId { get; set; }

        [JsonProperty]
        public string ClaimType { get; set; } = string.Empty;

        [JsonProperty]
        public string ClaimValue { get; set; } = string.Empty;

        [JsonProperty]
        public bool SystemGenerated { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(UserProfileId), UserProfileId.ToString() },
            { nameof(ClaimType), ClaimType }
        };
    }
}
