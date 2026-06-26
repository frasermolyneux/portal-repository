using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Dashboard;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Global;

public record GlobalModerationSummaryDto : IDto
{
    [JsonProperty]
    public int TotalActions { get; internal set; }

    [JsonProperty]
    public int OpenReports { get; internal set; }

    [JsonProperty]
    public int ClosedReports { get; internal set; }

    [JsonProperty]
    public AdminActionCountsDto ByType { get; internal set; } = new();

    [JsonProperty]
    public List<AdminLeaderboardEntryDto> TopModerators { get; internal set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
