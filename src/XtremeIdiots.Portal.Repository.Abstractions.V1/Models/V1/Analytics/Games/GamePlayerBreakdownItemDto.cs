using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Games;

public record GamePlayerBreakdownItemDto : IDto
{
    [JsonProperty]
    public Guid PlayerId { get; internal set; }

    [JsonProperty]
    public string DisplayName { get; internal set; } = string.Empty;

    [JsonProperty]
    public int ActivityCount { get; internal set; }

    [JsonProperty]
    public DateTime LastSeenUtc { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
