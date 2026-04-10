using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

public record CreateMapRotationDto : IDto
{
    public CreateMapRotationDto(GameType gameType, string title, string gameMode)
    {
        GameType = gameType;
        Title = title;
        GameMode = gameMode;
    }

    [JsonProperty]
    public GameType GameType { get; set; }

    [JsonProperty]
    public string Title { get; set; }

    [JsonProperty]
    public string? Description { get; set; }

    [JsonProperty]
    public string GameMode { get; set; }

    [JsonProperty]
    public MapRotationStatus? Status { get; set; }

    [JsonProperty]
    public string? Category { get; set; }

    [JsonProperty]
    public int? SequenceOrder { get; set; }

    [JsonProperty]
    public Guid? CreatedByUserId { get; set; }

    [JsonProperty]
    public List<Guid> MapIds { get; set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
