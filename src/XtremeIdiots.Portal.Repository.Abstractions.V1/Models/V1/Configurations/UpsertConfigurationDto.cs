using Newtonsoft.Json;

using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations
{
    public record UpsertConfigurationDto : IDto
    {
        [JsonProperty]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Configuration { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonIgnore]
        public Dictionary<string, string> TelemetryProperties => new()
        {
            { "Operation", "Upsert" }
        };
    }
}
