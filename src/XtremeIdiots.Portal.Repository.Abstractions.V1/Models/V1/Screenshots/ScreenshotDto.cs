using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots
{
    public record ScreenshotDto : IDto
    {
        [JsonProperty]
        public Guid ScreenshotId { get; internal set; }

        [JsonProperty]
        public Guid GameServerId { get; internal set; }

        [JsonProperty]
        public string GameType { get; internal set; } = string.Empty;

        [JsonProperty]
        public string PlayerIdentifier { get; internal set; } = string.Empty;

        [JsonProperty]
        public string? PlayerName { get; internal set; }

        [JsonProperty]
        public DateTime CapturedUtc { get; internal set; }

        [JsonProperty]
        public string BlobContainer { get; internal set; } = string.Empty;

        [JsonProperty]
        public string BlobName { get; internal set; } = string.Empty;

        [JsonProperty]
        public string? BlobUri { get; internal set; }

        [JsonProperty]
        public string ContentType { get; internal set; } = string.Empty;

        [JsonProperty]
        public long SizeBytes { get; internal set; }

        [JsonProperty]
        public string? ETag { get; internal set; }

        [JsonProperty]
        public string Source { get; internal set; } = string.Empty;

        [JsonProperty]
        public string Fingerprint { get; internal set; } = string.Empty;

        [JsonProperty]
        public string SourceFileName { get; internal set; } = string.Empty;

        [JsonProperty]
        public long SourceSizeBytes { get; internal set; }

        [JsonProperty]
        public DateTime SourceLastWriteUtc { get; internal set; }

        [JsonProperty]
        public bool Deleted { get; internal set; }

        [JsonProperty]
        public DateTime? DeletedUtc { get; internal set; }

        [JsonProperty]
        public DateTime CreatedUtc { get; internal set; }

        [JsonProperty]
        public DateTime LastUpdatedUtc { get; internal set; }

        [JsonProperty]
        public GameServerDto? GameServer { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(ScreenshotId), ScreenshotId.ToString() },
                    { nameof(GameServerId), GameServerId.ToString() },
                    { nameof(GameType), GameType },
                    { nameof(PlayerIdentifier), PlayerIdentifier }
                };

                if (GameServer is not null)
                {
                    telemetryProperties.AddAdditionalProperties(GameServer.TelemetryProperties);
                }

                return telemetryProperties;
            }
        }
    }
}
