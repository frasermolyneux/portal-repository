using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers
{
    public record IssueConnectedPlayerRegistrationTokenResultDto : IDto
    {
        [JsonProperty]
        public Guid ConnectedPlayerRegistrationTokenId { get; internal set; }

        [JsonProperty]
        public Guid PlayerId { get; internal set; }

        [JsonProperty]
        public string Token { get; internal set; } = string.Empty;

        [JsonProperty]
        public DateTime ExpiresAtUtc { get; internal set; }

        [JsonProperty]
        public int MaxAttempts { get; internal set; }

        [JsonProperty]
        public bool IsActive { get; internal set; }

        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(ConnectedPlayerRegistrationTokenId), ConnectedPlayerRegistrationTokenId.ToString() },
            { nameof(PlayerId), PlayerId.ToString() }
        };
    }
}
