using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers
{
    public record ConnectedPlayerActivationCodeDto : IDto
    {
        [JsonProperty]
        public Guid ConnectedPlayerActivationCodeId { get; internal set; }

        [JsonProperty]
        public Guid UserProfileId { get; internal set; }

        [JsonProperty]
        public string Code { get; internal set; } = string.Empty;

        [JsonProperty]
        public DateTime ExpiresAtUtc { get; internal set; }

        [JsonProperty]
        public int AttemptCount { get; internal set; }

        [JsonProperty]
        public int MaxAttempts { get; internal set; }

        [JsonProperty]
        public bool IsActive { get; internal set; }

        [JsonProperty]
        public DateTime ActivatedAtUtc { get; internal set; }

        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(ConnectedPlayerActivationCodeId), ConnectedPlayerActivationCodeId.ToString() },
            { nameof(UserProfileId), UserProfileId.ToString() }
        };
    }
}