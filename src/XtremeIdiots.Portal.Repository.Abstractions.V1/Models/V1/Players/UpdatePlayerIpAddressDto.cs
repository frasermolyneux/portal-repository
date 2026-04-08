using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players
{
    /// <summary>
    /// DTO for updating only a player's IP address and IP history.
    /// Does not modify username, aliases, or LastSeen.
    /// </summary>
    public record UpdatePlayerIpAddressDto : IDto
    {
        public UpdatePlayerIpAddressDto(Guid playerId, string ipAddress)
        {
            PlayerId = playerId;
            IpAddress = ipAddress;
        }

        [JsonProperty]
        public Guid PlayerId { get; set; }

        [JsonProperty]
        public string IpAddress { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(PlayerId), PlayerId.ToString() },
            { nameof(IpAddress), IpAddress }
        };
    }
}
