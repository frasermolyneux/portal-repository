using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;

public record DashboardHomeDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public DashboardSummaryDto Summary { get; internal set; } = new();

    [JsonProperty]
    public AnalyticsBucket Bucket { get; internal set; }

    [JsonProperty]
    public List<AnalyticsTimeseriesPointDto> TrendPoints { get; internal set; } = [];

    [JsonProperty]
    public DashboardCompositionDto Composition { get; internal set; } = new();

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
