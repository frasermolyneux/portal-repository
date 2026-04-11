using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.LiveStatus
{
    public record SetGameServerLiveStatusDto : IDto
    {
        [JsonProperty]
        public string? Title { get; set; }

        [JsonProperty]
        public string? Map { get; set; }

        [JsonProperty]
        public string? Mod { get; set; }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; set; }

        [JsonProperty]
        public int MaxPlayers { get; set; }

        [JsonProperty]
        public int CurrentPlayers { get; set; }

        [JsonProperty]
        public List<CreateLivePlayerDto> Players { get; set; } = [];

        [Newtonsoft.Json.JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(Title), Title ?? string.Empty },
            { nameof(CurrentPlayers), CurrentPlayers.ToString() }
        };
    }
}
