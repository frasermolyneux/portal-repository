using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Global;

public record GlobalGameBreakdownDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public List<GlobalGameBreakdownItemDto> Items { get; internal set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}