using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.CentralBanFileStatus
{
    /// <summary>
    /// Status snapshot of the central regenerated ban file blob for a single
    /// game type. Written by portal-sync after each regeneration; consumed by
    /// the ban file monitor dashboard to surface "is the central blob fresh?
    /// is it in sync with the database?" signals.
    /// </summary>
    public record CentralBanFileStatusDto : IDto
    {
        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; internal set; }

        [JsonProperty]
        public DateTime? BlobLastRegeneratedUtc { get; internal set; }

        [JsonProperty]
        public string? BlobETag { get; internal set; }

        [JsonProperty]
        public long? BlobSizeBytes { get; internal set; }

        [JsonProperty]
        public int? TotalLineCount { get; internal set; }

        [JsonProperty]
        public int? BanSyncLineCount { get; internal set; }

        [JsonProperty]
        public int? ExternalLineCount { get; internal set; }

        [JsonProperty]
        public DateTime? ExternalSourceLastModifiedUtc { get; internal set; }

        [JsonProperty]
        public int? LastRegenerationDurationMs { get; internal set; }

        [JsonProperty]
        public string? LastRegenerationError { get; internal set; }

        /// <summary>
        /// SHA-256 hex hash of the sorted active-ban GUIDs included in the most recent
        /// regeneration. Used by portal-sync to skip uploads when the active ban set
        /// has not changed since the previous regeneration.
        /// </summary>
        [JsonProperty]
        public string? ActiveBanSetHash { get; internal set; }

        [JsonProperty]
        public DateTime LastUpdatedUtc { get; internal set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(GameType), GameType.ToString() }
        };
    }
}
