using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Global;

public record GlobalPlayerActivityItemDto : IDto
{
    [JsonProperty]
    public Guid PlayerId { get; internal set; }

    [JsonProperty]
    public string DisplayName { get; internal set; } = string.Empty;

    [JsonProperty]
    public GameType GameType { get; internal set; }

    [JsonProperty]
    public int ActivityCount { get; internal set; }

    [JsonProperty]
    public DateTime LastSeenUtc { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
