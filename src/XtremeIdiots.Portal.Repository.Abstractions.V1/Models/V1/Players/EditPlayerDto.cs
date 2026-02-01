using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
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
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(PlayerId), PlayerId.ToString() },
            { nameof(Username), Username ?? string.Empty }
        };
    }
}
