using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.CentralBanFileStatus
{
    /// <summary>
    /// Upsert payload sent by portal-sync after regenerating the central ban file
    /// blob for a game type. Server-side either updates the existing row keyed by
    /// <see cref="GameType"/> or creates a new one. Only non-null properties are
    /// applied so a partial update (e.g. recording a regeneration error without
    /// new counts) does not clobber unrelated fields.
    /// </summary>
    public record UpsertCentralBanFileStatusDto : IDto
    {
        public UpsertCentralBanFileStatusDto(GameType gameType)
        {
            GameType = gameType;
        }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; private set; }

        [JsonProperty]
        public DateTime? BlobLastRegeneratedUtc { get; set; }

        [JsonProperty]
        public string? BlobETag { get; set; }

        [JsonProperty]
        public long? BlobSizeBytes { get; set; }

        [JsonProperty]
        public int? TotalLineCount { get; set; }

        [JsonProperty]
        public int? BanSyncLineCount { get; set; }

        [JsonProperty]
        public int? ExternalLineCount { get; set; }

        [JsonProperty]
        public DateTime? ExternalSourceLastModifiedUtc { get; set; }

        [JsonProperty]
        public int? LastRegenerationDurationMs { get; set; }

        [JsonProperty]
        public string? LastRegenerationError { get; set; }

        [JsonProperty]
        public string? ActiveBanSetHash { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(GameType), GameType.ToString() }
        };
    }
}
