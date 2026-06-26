using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Games;

public record GameTimeseriesDto : IDto
{
    [JsonProperty]
    public GameType GameType { get; internal set; }

    [JsonProperty]
    public AnalyticsBucket Bucket { get; internal set; }

    [JsonProperty]
    public List<GameTimeseriesPointDto> Points { get; internal set; } = [];

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
