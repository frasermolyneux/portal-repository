using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

public record PublicRotationMapDto
{
    public Guid? MapId { get; init; }
    public string MapName { get; init; } = "";
    public string? MapImageUri { get; init; }
    public int Likes { get; init; }
    public int Dislikes { get; init; }
    public int Votes => Likes + Dislikes;
    public double Approval => Votes == 0 ? 0 : Math.Round(100d * Likes / Votes, 1);
}

public record PublicServerRotationDto
{
    public Guid GameServerId { get; init; }
    public string Title { get; init; } = "";
    public GameType GameType { get; init; }
    public string? RotationTitle { get; init; }
    public bool IsObservedHistory { get; init; }
    public List<PublicRotationMapDto> Maps { get; init; } = [];
}

public record PublicServerRotationsDto : IDto
{
    public List<PublicServerRotationDto> Servers { get; init; } = [];
    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
