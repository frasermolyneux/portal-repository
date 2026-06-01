using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers
{
    public record ActivateConnectedPlayerActivationCodeDto
    {
        [JsonProperty]
        public Guid UserProfileId { get; init; }
    }
}