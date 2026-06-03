using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots;

public record PendingScreenshotRequestDto : IDto
{
    [JsonProperty]
    public Guid ScreenshotPendingRequestId { get; internal set; }

    [JsonProperty]
    public Guid GameServerId { get; internal set; }

    [JsonProperty]
    public string PlayerIdentifier { get; internal set; } = string.Empty;

    [JsonProperty]
    public string? PlayerName { get; internal set; }

    [JsonProperty]
    public string? CorrelationKey { get; internal set; }

    [JsonProperty]
    public DateTime RequestedAtUtc { get; internal set; }

    [JsonProperty]
    public DateTime ExpiresAtUtc { get; internal set; }

    [JsonProperty]
    public DateTime? ConsumedAtUtc { get; internal set; }

    [JsonProperty]
    public string? CreatedBy { get; internal set; }

    [JsonProperty]
    public DateTime CreatedUtc { get; internal set; }

    [JsonProperty]
    public DateTime LastUpdatedUtc { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => new()
    {
        { nameof(ScreenshotPendingRequestId), ScreenshotPendingRequestId.ToString() },
        { nameof(GameServerId), GameServerId.ToString() },
        { nameof(PlayerIdentifier), PlayerIdentifier }
    };
}