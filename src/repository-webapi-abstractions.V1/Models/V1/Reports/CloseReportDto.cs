using Newtonsoft.Json;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Reports
{
    public record CloseReportDto : IDto
    {
        public CloseReportDto(Guid userProfileId, string closingComments)
        {
            AdminUserProfileId = userProfileId;
            AdminClosingComments = closingComments;
        }

        [JsonProperty]
        public Guid AdminUserProfileId { get; private set; }

        [JsonProperty]
        public string AdminClosingComments { get; private set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(AdminUserProfileId), AdminUserProfileId.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}
