using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers
{
    public record IssueConnectedPlayerRegistrationTokenDto
    {
        [JsonProperty]
        public Guid PlayerId { get; init; }

        [JsonProperty]
        public int ExpiryMinutes { get; init; } = 5;

        [JsonProperty]
        public int MaxAttempts { get; init; } = 5;

        [JsonProperty]
        public string IssuedBy { get; init; } = "RegisterCommand";
    }
}
