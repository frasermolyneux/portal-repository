using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Reports
{
    public record CreateReportDto : IDto
    {
        public CreateReportDto(Guid playerId, Guid userProfileId, string comments)
        {
            PlayerId = playerId;
            UserProfileId = userProfileId;
            Comments = comments;
        }

        [JsonProperty]
        public Guid PlayerId { get; private set; }

        [JsonProperty]
        public Guid UserProfileId { get; private set; }

        [JsonProperty]
        public Guid? GameServerId { get; private set; }

        [JsonProperty]
        public string Comments { get; private set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(UserProfileId), UserProfileId.ToString() },
                    { nameof(GameServerId), GameServerId?.ToString() ?? string.Empty }
                };

                return telemetryProperties;
            }
        }
    }
}
