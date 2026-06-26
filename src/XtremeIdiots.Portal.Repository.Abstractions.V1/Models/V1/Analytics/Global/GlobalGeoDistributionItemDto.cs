using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Global;

public record GlobalGeoDistributionItemDto : IDto
{
    [JsonProperty]
    public string CountryCode { get; internal set; } = string.Empty;

    [JsonProperty]
    public int PlayerCount { get; internal set; }

    [JsonProperty]
    public double Percentage { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
