using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers
{
    public record ForceUnlinkConnectedPlayerDto
    {
        [JsonProperty]
        public Guid? UnlinkedByUserProfileId { get; init; }
    }
}
