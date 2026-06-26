using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;

public record DashboardCompositionDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public List<AnalyticsTopItemDto> TopGames { get; internal set; } = [];

    [JsonProperty]
    public List<AnalyticsTopItemDto> TopServers { get; internal set; } = [];

    [JsonProperty]
    public List<AnalyticsTopItemDto> TopMaps { get; internal set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}