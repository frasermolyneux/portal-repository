using System.Text.Json.Serialization;

using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.LiveStatus
{
    public record GameServerLiveStatusDto : IDto
    {
        [JsonProperty]
        public Guid ServerId { get; internal set; }

        [JsonProperty]
        public string? Title { get; internal set; }

        [JsonProperty]
        public string? Map { get; internal set; }

        [JsonProperty]
        public string? Mod { get; internal set; }

        [JsonProperty]
        public int MaxPlayers { get; internal set; }

        [JsonProperty]
        public int CurrentPlayers { get; internal set; }

        [JsonProperty]
        public DateTime? LastUpdated { get; internal set; }

        [JsonProperty]
        public bool IsOnline { get; internal set; }

        [Newtonsoft.Json.JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(ServerId), ServerId.ToString() },
            { nameof(Title), Title ?? string.Empty }
        };
    }
}
