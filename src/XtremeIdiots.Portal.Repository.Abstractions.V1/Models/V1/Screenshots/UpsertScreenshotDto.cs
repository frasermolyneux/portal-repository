using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots
{
    public record UpsertScreenshotDto : IDto
    {
        [JsonProperty]
        public Guid GameServerId { get; init; }

        [JsonProperty]
        public string GameType { get; init; } = string.Empty;

        [JsonProperty]
        public string PlayerIdentifier { get; init; } = string.Empty;

        [JsonProperty]
        public string? PlayerName { get; init; }

        [JsonProperty]
        public DateTime CapturedUtc { get; init; }

        [JsonProperty]
        public string BlobContainer { get; init; } = string.Empty;

        [JsonProperty]
        public string BlobName { get; init; } = string.Empty;

        [JsonProperty]
        public string? BlobUri { get; init; }

        [JsonProperty]
        public string ContentType { get; init; } = string.Empty;

        [JsonProperty]
        public long SizeBytes { get; init; }

        [JsonProperty]
        public string? ETag { get; init; }

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
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(GameServerId), GameServerId.ToString() },
            { nameof(GameType), GameType },
            { nameof(PlayerIdentifier), PlayerIdentifier },
            { nameof(Fingerprint), Fingerprint }
        };
    }
}
