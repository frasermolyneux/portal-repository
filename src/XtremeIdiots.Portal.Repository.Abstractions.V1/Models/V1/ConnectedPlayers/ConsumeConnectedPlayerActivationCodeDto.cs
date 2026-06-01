using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers
{
    public record ConsumeConnectedPlayerActivationCodeDto
    {
        [JsonProperty]
        public Guid PlayerId { get; init; }

        [JsonProperty]
        public string Code { get; init; } = string.Empty;
    }
}