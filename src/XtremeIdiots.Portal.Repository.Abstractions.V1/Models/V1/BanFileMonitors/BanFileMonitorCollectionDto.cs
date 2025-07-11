using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors;

[Obsolete("Use CollectionModel<BanFileMonitorDto> from MX.Api.Abstractions instead")]
public record BanFileMonitorCollectionDto
{
    public List<BanFileMonitorDto> Entries { get; set; } = new List<BanFileMonitorDto>();
    public int TotalRecords { get; set; }
}
