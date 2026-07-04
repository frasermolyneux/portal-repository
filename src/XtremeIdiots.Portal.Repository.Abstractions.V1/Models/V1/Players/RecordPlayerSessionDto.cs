using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
{
    /// <summary>
    /// DTO for recording a player session start. Updates LastSeen and username/alias.
    /// IP address updates are handled separately via UpdatePlayerIpAddressDto.
    /// </summary>
    public record RecordPlayerSessionDto : IDto
    {
        public RecordPlayerSessionDto(Guid playerId, string username, string? steamId = null)
        {
            PlayerId = playerId;
            Username = username;
            SteamId = steamId;
        }

        [JsonProperty]
        public Guid PlayerId { get; set; }

        [JsonProperty]
        public string Username { get; set; }

        [JsonProperty]
        public string? SteamId { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(PlayerId), PlayerId.ToString() },
            { nameof(Username), Username },
            { nameof(SteamId), SteamId ?? string.Empty }
        };
    }
}
