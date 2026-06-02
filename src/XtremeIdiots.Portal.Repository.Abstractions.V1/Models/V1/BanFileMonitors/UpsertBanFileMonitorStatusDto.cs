using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors
{
    /// <summary>
    /// Bulk-friendly status update sent by the server agent after each check cycle.
    /// Keyed by <see cref="GameServerId"/>: the server upserts (creates the row when
    /// no monitor exists for the game server, updates otherwise). Only properties with
    /// non-null values are applied so a partial cycle (e.g. check-only, no push) does
    /// not blank out unrelated fields.
    /// </summary>
    public record UpsertBanFileMonitorStatusDto : IDto
    {
        public UpsertBanFileMonitorStatusDto(Guid gameServerId)
        {
            GameServerId = gameServerId;
        }

        [JsonProperty]
        public Guid GameServerId { get; private set; }

        // Check
        [JsonProperty]
        public DateTime? LastCheckUtc { get; set; }

        [JsonProperty]
        public string? LastCheckResult { get; set; }

        [JsonProperty]
        public string? LastCheckErrorMessage { get; set; }

        [JsonProperty]
        public string? RemoteFilePath { get; set; }

        [JsonProperty]
        public string? ResolvedForMod { get; set; }

        [JsonProperty]
        public long? RemoteFileSize { get; set; }

        [JsonProperty]
        public DateTime? LegacyLastCheckUtc { get; set; }

        [JsonProperty]
        public string? LegacyLastCheckResult { get; set; }

        [JsonProperty]
        public string? LegacyLastCheckErrorMessage { get; set; }

        [JsonProperty]
        public string? LegacyRemoteFilePath { get; set; }

        [JsonProperty]
        public string? LegacyResolvedForMod { get; set; }

        [JsonProperty]
        public long? LegacyRemoteFileSize { get; set; }

        [JsonProperty]
        public DateTime? LegacyLastPushUtc { get; set; }

        [JsonProperty]
        public string? LegacyLastPushedETag { get; set; }

        [JsonProperty]
        public long? LegacyLastPushedSize { get; set; }

        // Import
        [JsonProperty]
        public DateTime? LastImportUtc { get; set; }

        [JsonProperty]
        public int? LastImportBanCount { get; set; }

        [JsonProperty]
        public string? LastImportSampleNames { get; set; }

        // Push
        [JsonProperty]
        public DateTime? LastPushUtc { get; set; }

        [JsonProperty]
        public string? LastPushedETag { get; set; }

        [JsonProperty]
        public long? LastPushedSize { get; set; }

        // Central awareness
        [JsonProperty]
        public string? LastCentralBlobETag { get; set; }

        [JsonProperty]
        public DateTime? LastCentralBlobUtc { get; set; }

        [JsonProperty]
        public string? LegacyLastCentralBlobETag { get; set; }

        [JsonProperty]
        public DateTime? LegacyLastCentralBlobUtc { get; set; }

        // Failure tracking — when set, replaces the value (does not increment).
        // Pass 0 on success to clear, or the new running total on failure.
        [JsonProperty]
        public int? ConsecutiveFailureCount { get; set; }

        // Counts
        [JsonProperty]
        public int? RemoteTotalLineCount { get; set; }

        [JsonProperty]
        public int? RemoteUntaggedCount { get; set; }

        [JsonProperty]
        public int? RemoteBanSyncCount { get; set; }

        [JsonProperty]
        public int? RemoteExternalCount { get; set; }

        [JsonProperty]
        public int? LegacyRemoteTotalLineCount { get; set; }

        [JsonProperty]
        public int? LegacyRemoteUntaggedCount { get; set; }

        [JsonProperty]
        public int? LegacyRemoteBanSyncCount { get; set; }

        [JsonProperty]
        public int? LegacyRemoteExternalCount { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(GameServerId), GameServerId.ToString() }
        };
    }
}
