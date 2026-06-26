using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Players;

public record PlayerRelatedActivityDto : IDto
{
    [JsonProperty]
    public AnalyticsTimeWindowDto Window { get; internal set; } = new();

    [JsonProperty]
    public int RelatedPlayersCount { get; internal set; }

    [JsonProperty]
    public int SharedIpAddressesCount { get; internal set; }

    [JsonProperty]
    public int SharedAliasesCount { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}