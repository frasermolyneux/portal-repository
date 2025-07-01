using Newtonsoft.Json;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Players
{
    public record PlayerAliasesCollectionDto : IDto
    {
        [JsonProperty]
        public IEnumerable<PlayerAliasDto> Entries { get; internal set; } = Enumerable.Empty<PlayerAliasDto>();

        [JsonProperty]
        public int TotalRecords { get; internal set; }

        [JsonProperty]
        public string ContinuationToken { get; internal set; } = string.Empty;

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { "TotalRecords", TotalRecords.ToString() },
                    { "EntriesCount", Entries.Count().ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}
