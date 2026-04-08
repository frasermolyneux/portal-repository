using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
{
    /// <summary>
    /// DTO for recording a player session start. Updates LastSeen and username/alias.
    /// IP address updates are handled separately via UpdatePlayerIpAddressDto.
    /// Preferred over EditPlayerDto for the "player connected" use case.
    /// </summary>
    public record RecordPlayerSessionDto : IDto
    {
        public RecordPlayerSessionDto(Guid playerId, string username)
        {
            PlayerId = playerId;
            Username = username;
        }

        [JsonProperty]
        public Guid PlayerId { get; set; }

        [JsonProperty]
        public string Username { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(PlayerId), PlayerId.ToString() },
            { nameof(Username), Username }
        };
    }
}
