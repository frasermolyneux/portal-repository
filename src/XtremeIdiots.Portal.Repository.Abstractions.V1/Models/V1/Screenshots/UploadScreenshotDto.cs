using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots
{
    public record UploadScreenshotDto : IDto
    {
        [JsonProperty]
        public Guid GameServerId { get; init; }

        [JsonProperty]
        public string GameType { get; init; } = string.Empty;

        [JsonProperty]
        public string? PlayerIdentifier { get; init; }

        [JsonProperty]
        public string? PlayerName { get; init; }

        [JsonProperty]
        public DateTime CapturedUtc { get; init; }

        [JsonProperty]
        public string Source { get; init; } = string.Empty;

        [JsonProperty]
        public string Fingerprint { get; init; } = string.Empty;

        [JsonProperty]
        public string SourceFileName { get; init; } = string.Empty;

        [JsonProperty]
        public long SourceSizeBytes { get; init; }

        [JsonProperty]
        public DateTime SourceLastWriteUtc { get; init; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var properties = new Dictionary<string, string>
                {
                    { nameof(GameServerId), GameServerId.ToString() },
                    { nameof(GameType), GameType },
                    { nameof(Fingerprint), Fingerprint }
                };

                if (!string.IsNullOrWhiteSpace(PlayerIdentifier))
                {
                    properties.Add(nameof(PlayerIdentifier), PlayerIdentifier);
                }

                return properties;
            }
        }
    }
}
