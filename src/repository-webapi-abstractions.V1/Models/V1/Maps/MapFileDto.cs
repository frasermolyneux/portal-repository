using Newtonsoft.Json;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Maps
{
    public record MapFileDto : IDto
    {
        public MapFileDto(string fileName, string url)
        {
            FileName = fileName;
            Url = url;
        }

        [JsonProperty]
        public string FileName { get; set; }

        [JsonProperty]
        public string Url { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>();
                return telemetryProperties;
            }
        }
    }
}