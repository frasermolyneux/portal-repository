using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Tags
{
    public class TagsCollectionDto
    {
        [JsonProperty]
        public List<TagDto> Entries { get; set; } = new List<TagDto>();
    }
}