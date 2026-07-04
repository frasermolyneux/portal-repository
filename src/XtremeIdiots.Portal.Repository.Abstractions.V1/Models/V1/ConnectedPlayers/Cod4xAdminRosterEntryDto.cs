using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers
{
    public record Cod4xAdminRosterEntryDto : IDto
    {
        [JsonProperty]
        public string PlayerGuid { get; internal set; } = string.Empty;

        [JsonProperty]
        public int Power { get; internal set; }

        [JsonProperty]
        public List<string> Tags { get; internal set; } = [];

        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(PlayerGuid), PlayerGuid },
            { nameof(Power), Power.ToString() }
        };
    }
}
