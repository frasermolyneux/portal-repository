using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Servers;

public record ServerChatCommandsSummaryDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public int TotalExecutions { get; internal set; }

    [JsonProperty]
    public int TotalDenied { get; internal set; }

    [JsonProperty]
    public List<ServerChatCommandCountDto> Commands { get; internal set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
