using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players
{
    /// <summary>
    /// Data transfer object for creating a player alias
    /// </summary>
    public record CreatePlayerAliasDto : IDto
    {
        public CreatePlayerAliasDto(string name)
        {
            Name = name;
        }

        [JsonProperty]
        public string Name { get; private set; }

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties
        {
            get
            {
                var telemetryProperties = new Dictionary<string, string>
                {
                    { nameof(Name), Name }
                };

                return telemetryProperties;
            }
        }
    }
}
