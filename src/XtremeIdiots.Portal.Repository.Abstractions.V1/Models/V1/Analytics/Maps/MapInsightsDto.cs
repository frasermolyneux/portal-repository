using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Analytics.Maps;

public record MapRatingDto
{
    public Guid MapId { get; init; }
    public GameType GameType { get; init; }
    public string MapName { get; init; } = "";
    public string? MapImageUri { get; init; }
    public int Likes { get; init; }
    public int Dislikes { get; init; }
    public int Votes => Likes + Dislikes;
    public double Approval => Votes == 0 ? 0 : Math.Round(100d * Likes / Votes, 1);
}

public record MapInsightsDto : IDto
{
    public int RatedMaps { get; init; }
    public int TotalLikes { get; init; }
    public int TotalDislikes { get; init; }
    public int TotalVotes => TotalLikes + TotalDislikes;
    public double Approval => TotalVotes == 0 ? 0 : Math.Round(100d * TotalLikes / TotalVotes, 1);
    public int MinimumRankingVotes { get; init; } = 5;
    public List<MapRatingDto> MostVoted { get; init; } = [];
    public List<MapRatingDto> Popular { get; init; } = [];
    public List<MapRatingDto> Unpopular { get; init; } = [];
    public List<MapHotspotItemDto> Hotspots { get; init; } = [];
    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
