using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;

public record AnalyticsSeriesDto : IDto
{
    [JsonProperty]
    public string Key { get; internal set; } = string.Empty;

    [JsonProperty]
    public string Label { get; internal set; } = string.Empty;

    /// <summary>
    /// Distinguishes the current-window series ("current") from prior-period overlay series ("comparison").
    /// </summary>
    [JsonProperty]
    public string Role { get; internal set; } = "current";

    /// <summary>
    /// Human-readable label for the comparison period this series represents (e.g. "Previous week").
    /// Empty for current-window series.
    /// </summary>
    [JsonProperty]
    public string PeriodLabel { get; internal set; } = string.Empty;

    [JsonProperty]
    public List<AnalyticsSeriesValueDto> Values { get; internal set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
