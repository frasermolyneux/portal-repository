using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;

public record DashboardServerDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public Guid GameServerId { get; internal set; }

    [JsonProperty]
    public string Title { get; internal set; } = string.Empty;

    [JsonProperty]
    public GameType GameType { get; internal set; }

    [JsonProperty]
    public bool Online { get; internal set; }

    [JsonProperty]
    public int CurrentPlayers { get; internal set; }

    [JsonProperty]
    public int MaxPlayers { get; internal set; }

    [JsonProperty]
    public string? MapName { get; internal set; }

    [JsonProperty]
    public double AvgPlayers { get; internal set; }

    [JsonProperty]
    public int PeakPlayers { get; internal set; }

    [JsonProperty]
    public int EventsCount { get; internal set; }

    [JsonProperty]
    public int ChatCount { get; internal set; }

    [JsonProperty]
    public int UniquePlayers { get; internal set; }

    [JsonProperty]
    public AnalyticsBucket Bucket { get; internal set; }

    [JsonProperty]
    public List<AnalyticsTimeseriesPointDto> TrendPoints { get; internal set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
