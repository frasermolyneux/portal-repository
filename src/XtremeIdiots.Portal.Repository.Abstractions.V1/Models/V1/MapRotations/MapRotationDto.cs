using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

public record MapRotationDto : IDto
{
    public MapRotationDto(Guid mapRotationId, GameType gameType, string title, string? description, string gameMode, int version, string? contentHash, DateTime createdAt, DateTime updatedAt, List<MapRotationMapDto> mapRotationMaps, List<MapRotationServerAssignmentDto> serverAssignments)
    {
        MapRotationId = mapRotationId;
        GameType = gameType;
        Title = title;
        Description = description;
        GameMode = gameMode;
        Version = version;
        ContentHash = contentHash;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        MapRotationMaps = mapRotationMaps;
        ServerAssignments = serverAssignments;
    }

    [JsonProperty]
    public Guid MapRotationId { get; set; }

    [JsonProperty]
    public GameType GameType { get; set; }

    [JsonProperty]
    public string Title { get; set; }

    [JsonProperty]
    public string? Description { get; set; }

    [JsonProperty]
    public string GameMode { get; set; }

    [JsonProperty]
    public int Version { get; set; }

    [JsonProperty]
    public string? ContentHash { get; set; }

    [JsonProperty]
    public DateTime CreatedAt { get; set; }

    [JsonProperty]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty]
    public List<MapRotationMapDto> MapRotationMaps { get; set; }

    [JsonProperty]
    public List<MapRotationServerAssignmentDto> ServerAssignments { get; set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
