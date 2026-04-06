using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

public record MapRotationMapDto : IDto
{
    [JsonProperty]
    public Guid MapRotationMapId { get; set; }

    [JsonProperty]
    public Guid MapId { get; set; }

    [JsonProperty]
    public int SortOrder { get; set; }

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
