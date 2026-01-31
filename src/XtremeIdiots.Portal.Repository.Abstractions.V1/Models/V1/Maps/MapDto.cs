using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps
{
    public record MapDto : IDto
    {
        [JsonProperty]
        public Guid MapId { get; internal set; }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; internal set; }

        [JsonProperty]
        public string MapName { get; internal set; } = string.Empty;

        [JsonProperty]
        public string MapImageUri { get; internal set; } = string.Empty;

        [JsonProperty]
        public int TotalLikes { get; internal set; } = 0;

        [JsonProperty]
        public int TotalDislikes { get; internal set; } = 0;

        [JsonProperty]
        public int TotalVotes { get; internal set; } = 0;

        [JsonProperty]
        public double LikePercentage { get; internal set; } = 0;

        [JsonProperty]
        public double DislikePercentage { get; internal set; } = 0;

        [JsonProperty]
        public List<MapFileDto> MapFiles { get; internal set; } = [];

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(MapId), MapId.ToString() },
            { nameof(GameType), GameType.ToString() }
        };
    }
}