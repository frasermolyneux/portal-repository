using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
{
    /// <summary>
    /// DTO for updating only a player's username and alias history.
    /// Does not modify IP address or LastSeen.
    /// </summary>
    public record UpdatePlayerUsernameDto : IDto
    {
        public UpdatePlayerUsernameDto(Guid playerId, string username)
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
