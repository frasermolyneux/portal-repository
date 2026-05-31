using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers
{
    public record CreateConnectedPlayerLinkDto
    {
        [JsonProperty]
        public Guid PlayerId { get; init; }

        [JsonProperty]
        public Guid UserProfileId { get; init; }

        [JsonProperty]
        public Guid? LinkedByUserProfileId { get; init; }

        [JsonProperty]
        public ConnectedPlayerLinkMethod LinkMethod { get; init; } = ConnectedPlayerLinkMethod.TrustedWebsite;
    }
}
