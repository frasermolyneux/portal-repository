using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers
{
    public record VerifyConnectedPlayerRegistrationTokenDto
    {
        [JsonProperty]
        public Guid PlayerId { get; init; }

        [JsonProperty]
        public Guid UserProfileId { get; init; }

        [JsonProperty]
        public string Token { get; init; } = string.Empty;

        [JsonProperty]
        public Guid? LinkedByUserProfileId { get; init; }
    }
}
