using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Reports
{
    public class CreateReportDto : IDto
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
                    { nameof(GameServerId), GameServerId is not null ? ((Guid)GameServerId).ToString() : string.Empty }
                };

                return telemetryProperties;
            }
        }
    }
}
