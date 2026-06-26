using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Games;

public record GameMapBreakdownItemDto : IDto
{
    [JsonProperty]
    public Guid? MapId { get; internal set; }

    [JsonProperty]
    public string MapName { get; internal set; } = string.Empty;

    [JsonProperty]
    public int Plays { get; internal set; }

    [JsonProperty]
    public int UpVotes { get; internal set; }

    [JsonProperty]
    public int DownVotes { get; internal set; }

    [JsonProperty]
    public double Score { get; internal set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
