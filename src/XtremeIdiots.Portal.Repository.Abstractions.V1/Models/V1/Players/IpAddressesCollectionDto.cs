using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

[Obsolete("Use CollectionModel<IpAddressDto> from MX.Api.Abstractions instead")]
public record IpAddressesCollectionDto
{
    public List<IpAddressDto> Entries { get; set; } = new List<IpAddressDto>();
    public int TotalRecords { get; set; }
}
