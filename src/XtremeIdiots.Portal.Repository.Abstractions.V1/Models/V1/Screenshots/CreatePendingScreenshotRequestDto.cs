using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Screenshots;

public record CreatePendingScreenshotRequestDto : IDto
{
    [JsonProperty]
    public Guid GameServerId { get; init; }

    [JsonProperty]
    public string PlayerIdentifier { get; init; } = string.Empty;

    [JsonProperty]
    public string? PlayerName { get; init; }

    [JsonProperty]
    public string? CorrelationKey { get; init; }

    [JsonProperty]
    public DateTime? RequestedAtUtc { get; init; }

    [JsonProperty]
    public DateTime? ExpiresAtUtc { get; init; }

    [JsonProperty]
    public string? CreatedBy { get; init; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties
    {
        get
        {
            var properties = new Dictionary<string, string>
            {
                { nameof(GameServerId), GameServerId.ToString() }
            };

            if (!string.IsNullOrWhiteSpace(PlayerIdentifier))
            {
                properties.Add(nameof(PlayerIdentifier), PlayerIdentifier);
            }

            return properties;
        }
    }
}