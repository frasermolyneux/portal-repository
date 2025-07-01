using System.Collections.Generic;
using Newtonsoft.Json;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Players
{
    /// <summary>
    /// Collection DTO for IpAddresses.
    /// </summary>
    public class IpAddressesCollectionDto
    {
        [JsonProperty]
        public List<IpAddressDto> Entries { get; set; } = new List<IpAddressDto>();

        [JsonProperty]
        public int TotalCount { get; set; }
    }
}
