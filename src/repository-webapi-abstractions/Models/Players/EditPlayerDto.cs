using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players
{
    public record EditPlayerDto : IDto
    {
        public EditPlayerDto(Guid playerId)
        {
            PlayerId = playerId;
        }

        [JsonProperty]
        public Guid PlayerId { get; set; }

        [JsonProperty]
        public string? Username { get; set; }

        [JsonProperty]
        public string? IpAddress { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(Username), Username is not null ? Username.ToString() : string.Empty }
                };

                return telemetryProperties;
            }
        }
    }
}
