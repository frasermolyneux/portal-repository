using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;

[Obsolete("Use CollectionModel<MapDto> from MX.Api.Abstractions instead")]
public record MapsCollectionDto
{
    public List<MapDto> Entries { get; set; } = new List<MapDto>();
    public int TotalRecords { get; set; }
}
