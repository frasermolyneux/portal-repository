using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers
{
    public record Cod4xAdminRosterDto : IDto
    {
        [JsonProperty]
        public bool Enabled { get; internal set; }

        [JsonProperty]
        public int DefaultPower { get; internal set; }

        [JsonProperty]
        public List<Cod4xAdminRosterEntryDto> Entries { get; internal set; } = [];

        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(Enabled), Enabled.ToString() },
            { nameof(DefaultPower), DefaultPower.ToString() },
            { nameof(Entries), Entries.Count.ToString() }
        };
    }
}
