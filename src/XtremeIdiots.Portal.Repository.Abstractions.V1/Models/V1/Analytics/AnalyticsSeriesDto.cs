using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;

public record AnalyticsSeriesDto : IDto
{
    [JsonProperty]
    public string Key { get; internal set; } = string.Empty;

    [JsonProperty]
    public string Label { get; internal set; } = string.Empty;

    [JsonProperty]
    public List<AnalyticsSeriesValueDto> Values { get; internal set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
