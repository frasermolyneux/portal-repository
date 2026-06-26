using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics;

public record AnalyticsCompareMetaDto : IDto
{
    [JsonProperty]
    public AnalyticsCompareMode CompareMode { get; internal set; }

    [JsonProperty]
    public int ComparePeriods { get; internal set; }

    [JsonProperty]
    public AnalyticsAlignMode AlignMode { get; internal set; }

    [JsonProperty]
    public string Timezone { get; internal set; } = "UTC";

    [JsonProperty]
    public bool Normalize { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
