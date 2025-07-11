using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors
{
    public record EditBanFileMonitorDto : IDto
    {
        [JsonConstructor]
        public EditBanFileMonitorDto(Guid banFileMonitorId)
        {
            BanFileMonitorId = banFileMonitorId;
        }

        public EditBanFileMonitorDto(Guid banFileMonitorId, string filePath)
        {
            BanFileMonitorId = banFileMonitorId;
            FilePath = filePath;
        }

        public EditBanFileMonitorDto(Guid banFileMonitorId, long remoteFileSize, DateTime lastSync)
        {
            BanFileMonitorId = banFileMonitorId;
            RemoteFileSize = remoteFileSize;
            LastSync = lastSync;
        }

        [JsonProperty]
        public Guid BanFileMonitorId { get; private set; }

        [JsonProperty]
        public string? FilePath { get; set; }

        [JsonProperty]
        public long? RemoteFileSize { get; set; }

        [JsonProperty]
        public DateTime? LastSync { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(BanFileMonitorId), BanFileMonitorId.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}
