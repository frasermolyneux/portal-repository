using System;
using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapPacks;

public record CreateMapPackDto : IDto
{
    public CreateMapPackDto(Guid gameServerId, string title, string gameMode)
    {
        GameServerId = gameServerId;
        Title = title;
        GameMode = gameMode;
    }

    [JsonProperty]
    public Guid GameServerId { get; set; }

    [JsonProperty]
    public string Title { get; set; }

    [JsonProperty]
    public string Description { get; set; } = string.Empty;

    [JsonProperty]
    public string GameMode { get; set; }

    [JsonProperty]
    public bool SyncToGameServer { get; set; }

    [JsonProperty]
    public List<Guid> MapIds { get; set; } = [];

    [JsonIgnore]
    public Dictionary<string, string> TelemetryProperties
    {
        get
        {
            var telemetryProperties = new Dictionary<string, string>();
            return telemetryProperties;
        }
    }
}
