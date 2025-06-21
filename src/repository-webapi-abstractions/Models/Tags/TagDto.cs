using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Tags
{
    /// <summary>
    /// Data transfer object for Tag entity.
    /// </summary>
    public class TagDto
    {
        [JsonProperty]
        public Guid TagId { get; set; }

        [JsonProperty]
        public string Name { get; set; } = string.Empty;

        [JsonProperty]
        public string? Description { get; set; }

        [JsonProperty]
        public bool UserDefined { get; set; }

        [JsonProperty]
        public string? TagHtml { get; set; }
    }
}
