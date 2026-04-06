using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

public record UpdateMapRotationDto : IDto
{
    public UpdateMapRotationDto(Guid mapRotationId)
    {
        MapRotationId = mapRotationId;
    }

    [JsonProperty]
    public Guid MapRotationId { get; private set; }

    [JsonProperty]
    public string? Title { get; set; }

    [JsonProperty]
    public string? Description { get; set; }

    [JsonProperty]
    public string? GameMode { get; set; }

    [JsonProperty]
    public List<Guid>? MapIds { get; set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
