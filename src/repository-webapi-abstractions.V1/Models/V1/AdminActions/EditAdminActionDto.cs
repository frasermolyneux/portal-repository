using Newtonsoft.Json;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.AdminActions
{
    public record EditAdminActionDto : IDto
    {
        public EditAdminActionDto(Guid adminActionId)
        {
            AdminActionId = adminActionId;
        }

        [JsonProperty]
        public Guid AdminActionId { get; private set; }

        [JsonProperty]
        public string? Text { get; set; }

        [JsonProperty]
        public DateTime? Expires { get; set; }

        [JsonProperty]
        public int? ForumTopicId { get; set; }

        [JsonProperty]
        public string? AdminId { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(AdminActionId), AdminActionId.ToString() },
                    { nameof(AdminId), AdminId is not null ? AdminId.ToString() : string.Empty }
                };

                return telemetryProperties;
            }
        }
    }
}
