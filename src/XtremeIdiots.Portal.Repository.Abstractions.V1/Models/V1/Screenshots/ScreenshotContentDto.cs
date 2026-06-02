using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots
{
    public record ScreenshotContentDto : IDto
    {
        [JsonProperty]
        public Guid ScreenshotId { get; init; }

        [JsonProperty]
        public string ContentType { get; init; } = string.Empty;

        [JsonProperty]
        public string FileName { get; init; } = string.Empty;

        [JsonProperty]
        public byte[] Content { get; init; } = [];

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(ScreenshotId), ScreenshotId.ToString() },
            { nameof(ContentType), ContentType },
            { nameof(FileName), FileName }
        };
    }
}
