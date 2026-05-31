using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers
{
    public record ConnectedPlayerDto : IDto
    {
        [JsonProperty]
        public Guid ConnectedPlayerProfileId { get; internal set; }

        [JsonProperty]
        public Guid PlayerId { get; internal set; }

        [JsonProperty]
        public Guid UserProfileId { get; internal set; }

        [JsonProperty]
        public GameType GameType { get; internal set; }

        [JsonProperty]
        public string Username { get; internal set; } = string.Empty;

        [JsonProperty]
        public ConnectedPlayerLinkMethod LinkMethod { get; internal set; }

        [JsonProperty]
        public DateTime LinkedAtUtc { get; internal set; }

        [JsonProperty]
        public Guid? LinkedByUserProfileId { get; internal set; }

        [JsonProperty]
        public DateTime? UnlinkedAtUtc { get; internal set; }

        [JsonProperty]
        public Guid? UnlinkedByUserProfileId { get; internal set; }

        [JsonProperty]
        public bool IsActive { get; internal set; }

        public Dictionary<string, string> TelemetryProperties => new()
        {
            { nameof(ConnectedPlayerProfileId), ConnectedPlayerProfileId.ToString() },
            { nameof(PlayerId), PlayerId.ToString() },
            { nameof(UserProfileId), UserProfileId.ToString() },
            { nameof(GameType), GameType.ToString() },
            { nameof(LinkMethod), LinkMethod.ToString() }
        };
    }
}
