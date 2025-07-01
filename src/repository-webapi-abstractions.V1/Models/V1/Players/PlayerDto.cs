using System.Text.Json.Serialization;

using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Reports;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Tags;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Players
{
    public record PlayerDto : IDto
    {
        [JsonProperty]
        public Guid PlayerId { get; internal set; }

        [JsonProperty]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType GameType { get; internal set; }

        [JsonProperty]
        public string Username { get; internal set; } = string.Empty;

        [JsonProperty]
        public string Guid { get; internal set; } = string.Empty;

        [JsonProperty]
        public DateTime FirstSeen { get; internal set; }

        [JsonProperty]
        public DateTime LastSeen { get; internal set; }

        [JsonProperty]
        public string IpAddress { get; internal set; } = string.Empty;

        [JsonProperty]
        public List<AliasDto> PlayerAliases { get; internal set; } = new List<AliasDto>();

        [JsonProperty]
        public List<IpAddressDto> PlayerIpAddresses { get; internal set; } = new List<IpAddressDto>();

        [JsonProperty]
        public List<AdminActionDto> AdminActions { get; internal set; } = new List<AdminActionDto>();

        [JsonProperty]
        public List<ReportDto> Reports { get; internal set; } = new List<ReportDto>();

        [JsonProperty]
        public List<RelatedPlayerDto> RelatedPlayers { get; internal set; } = new List<RelatedPlayerDto>();

        [JsonProperty]
        public List<ProtectedNameDto> ProtectedNames { get; internal set; } = new List<ProtectedNameDto>();

        public List<PlayerTagDto> Tags { get; internal set; } = new List<PlayerTagDto>();

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(PlayerId), PlayerId.ToString() },
                    { nameof(GameType), GameType.ToString() },
                    { nameof(Username), Username.ToString() },
                    { nameof(Guid), Guid.ToString() }
                };

                return telemetryProperties;
            }
        }
    }
}