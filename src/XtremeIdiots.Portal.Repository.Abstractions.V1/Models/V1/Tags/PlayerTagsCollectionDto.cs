using System.Collections.Generic;
using Newtonsoft.Json;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags
{
    /// <summary>
    /// Collection DTO for PlayerTags.
    /// </summary>
    public class PlayerTagsCollectionDto
    {
        [JsonProperty]
        public List<PlayerTagDto> Entries { get; set; } = new List<PlayerTagDto>();
    }
}
