using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models
{
    public class CollectionDto<T>
    {
        [JsonProperty]
        public int TotalRecords { get; set; }

        [JsonProperty]
        public int FilteredRecords { get; set; }

        [JsonProperty]
        public List<T> Entries { get; set; } = new List<T>();
    }
}
