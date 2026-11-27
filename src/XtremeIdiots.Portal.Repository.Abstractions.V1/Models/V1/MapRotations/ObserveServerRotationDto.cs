using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

public record ObserveServerRotationDto : IDto
{
    public DateTimeOffset ObservedAt { get; set; }
    public List<ObservedRotationEntryDto> Entries { get; set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}

public record ObservedRotationEntryDto
{
    public string MapName { get; set; } = "";
    public string? GameMode { get; set; }
}

public record MapRotationEntryDto
{
    public Guid MapId { get; set; }
    public string? GameMode { get; set; }
}
