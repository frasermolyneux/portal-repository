using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Dashboard;

public record DashboardRecordedActivityPointDto
{
    [JsonProperty]
    public DateTime BucketStartUtc { get; internal set; }

    [JsonProperty]
    public DateTime BucketEndUtc { get; internal set; }

    [JsonProperty]
    public int RecordedPlayers { get; internal set; }

    [JsonProperty]
    public int SessionStarts { get; internal set; }

    [JsonProperty]
    public decimal ObservedSeconds { get; internal set; }
}
