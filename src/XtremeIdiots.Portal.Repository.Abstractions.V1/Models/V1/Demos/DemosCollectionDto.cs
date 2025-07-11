using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Demos;

[Obsolete("Use CollectionModel<DemoDto> from MX.Api.Abstractions instead")]
public record DemosCollectionDto
{
    public List<DemoDto> Entries { get; set; } = new List<DemoDto>();
    public int TotalRecords { get; set; }
}
