using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors
{
    /// <summary>
    /// Status snapshot for a game server's ban file. Now owned and written by the
    /// server agent — admins should not create or edit these directly. Lifecycle
    /// follows <see cref="GameServerDto.BanFileSyncEnabled"/>.
    /// </summary>
    public record BanFileMonitorDto : IDto
    {
        [JsonProperty]
        public Guid BanFileMonitorId { get; internal set; }

        [JsonProperty]
        public Guid GameServerId { get; internal set; }

        // === Status snapshot (written by the agent on every check) ===

        /// <summary>UTC timestamp of the most recent check attempt (success or failure).</summary>
        [JsonProperty]
        public DateTime? LastCheckUtc { get; internal set; }

        /// <summary>One of <c>Success</c>, <c>FtpError</c>, <c>FileNotFound</c>, <c>Skipped</c>.</summary>
        [JsonProperty]
        public string? LastCheckResult { get; internal set; }

        /// <summary>Diagnostic message when <see cref="LastCheckResult"/> is non-success.</summary>
        [JsonProperty]
        public string? LastCheckErrorMessage { get; internal set; }

        /// <summary>Path resolved by the agent for the most recent check.</summary>
        [JsonProperty]
        public string? RemoteFilePath { get; internal set; }

        /// <summary>Active mod the resolved path was targeted at, when applicable.</summary>
        [JsonProperty]
        public string? ResolvedForMod { get; internal set; }

        /// <summary>Size in bytes of the remote ban file at the most recent successful check.</summary>
        [JsonProperty]
        public long? RemoteFileSize { get; internal set; }

        // === Last import (manual ban detection) ===

        [JsonProperty]
        public DateTime? LastImportUtc { get; internal set; }

        [JsonProperty]
        public int? LastImportBanCount { get; internal set; }

        /// <summary>JSON array of the last few player names imported, for UI display.</summary>
        [JsonProperty]
        public string? LastImportSampleNames { get; internal set; }

        // === Last push (central blob propagation) ===

        [JsonProperty]
        public DateTime? LastPushUtc { get; internal set; }

        [JsonProperty]
        public string? LastPushedETag { get; internal set; }

        [JsonProperty]
        public long? LastPushedSize { get; internal set; }

        // === Central blob awareness (used by the dashboard to flag drift) ===

        [JsonProperty]
        public string? LastCentralBlobETag { get; internal set; }

        [JsonProperty]
        public DateTime? LastCentralBlobUtc { get; internal set; }

        // === Failure tracking ===

        [JsonProperty]
        public int ConsecutiveFailureCount { get; internal set; }

        // === Per-tag breakdown of the remote file at last check ===

        [JsonProperty]
        public int? RemoteTotalLineCount { get; internal set; }

        [JsonProperty]
        public int? RemoteUntaggedCount { get; internal set; }

        [JsonProperty]
        public int? RemoteBanSyncCount { get; internal set; }

        [JsonProperty]
        public int? RemoteExternalCount { get; internal set; }

        [JsonProperty]
#pragma warning disable CS8618
        public GameServerDto GameServer { get; internal set; }
#pragma warning restore CS8618

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(BanFileMonitorId), BanFileMonitorId.ToString() },
                    { nameof(GameServerId), GameServerId.ToString() }
                };

                if (GameServer is not null)
                    telemetryProperties.AddAdditionalProperties(GameServer.TelemetryProperties);

                return telemetryProperties;
            }
        }
    }
}

