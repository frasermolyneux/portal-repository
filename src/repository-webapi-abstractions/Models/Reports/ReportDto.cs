using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Extensions;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.UserProfiles;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Reports
{
    public record ReportDto : IDto
    {
        [JsonProperty]
        public Guid ReportId { get; internal set; }

        [JsonProperty]
        public Guid PlayerId { get; internal set; }

        [JsonProperty]
        public Guid UserProfileId { get; internal set; }

        [JsonProperty]
        public Guid GameServerId { get; internal set; }

        [JsonProperty]
        public GameType GameType { get; internal set; }

        [JsonProperty]
        public string Comments { get; internal set; } = string.Empty;

        [JsonProperty]
        public DateTime Timestamp { get; internal set; }

        [JsonProperty]
        public Guid AdminUserProfileId { get; internal set; }

        [JsonProperty]
        public string? AdminClosingComments { get; internal set; }

        [JsonProperty]
        public bool Closed { get; internal set; }

        [JsonProperty]
        public DateTime ClosedTimestamp { get; internal set; }

        [JsonProperty]
        public UserProfileDto? UserProfile { get; internal set; }

        [JsonProperty]
        public UserProfileDto? AdminUserProfile { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(ReportId), ReportId.ToString() },
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(UserProfileId), UserProfileId.ToString() },
                    { nameof(GameServerId), GameServerId.ToString() },
                    { nameof(GameType), GameType.ToString() },
                    { nameof(AdminUserProfileId), AdminUserProfileId.ToString() }
                };

                if (UserProfile is not null)
                    telemetryProperties.AddAdditionalProperties(UserProfile.TelemetryProperties);

                if (AdminUserProfile is not null)
                    telemetryProperties.AddAdditionalProperties(AdminUserProfile.TelemetryProperties);

                return telemetryProperties;
            }
        }
    }
}
