using System;
using Newtonsoft.Json;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Players;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.UserProfiles;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Tags
{
    /// <summary>
    /// Data transfer object for PlayerTag entity.
    /// </summary>
    public class PlayerTagDto
    {
        [JsonProperty]
        public Guid PlayerTagId { get; set; }

        [JsonProperty]
        public Guid? PlayerId { get; set; }

        [JsonProperty]
        public Guid? TagId { get; set; }

        [JsonProperty]
        public Guid? UserProfileId { get; set; }

        [JsonProperty]
        public DateTime Assigned { get; set; }

        [JsonProperty]
        public PlayerDto? Player { get; set; }

        [JsonProperty]
        public TagDto? Tag { get; set; }

        [JsonProperty]
        public UserProfileDto? UserProfile { get; set; }
    }
}
