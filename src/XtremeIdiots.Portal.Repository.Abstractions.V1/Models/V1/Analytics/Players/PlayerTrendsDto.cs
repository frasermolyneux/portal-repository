using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

public record PlayerTrendsDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public AnalyticsBucket Bucket { get; internal set; }

    [JsonProperty]
    public List<AnalyticsTimeseriesPointDto> Points { get; internal set; } = [];

    [JsonProperty]
    public List<DateTime> Labels { get; internal set; } = [];

    [JsonProperty]
    public List<AnalyticsSeriesDto> Series { get; internal set; } = [];

    [JsonProperty]
    public AnalyticsCompareSummaryDto? Summary { get; internal set; }

    [JsonProperty]
    public AnalyticsCompareMetaDto? Meta { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}