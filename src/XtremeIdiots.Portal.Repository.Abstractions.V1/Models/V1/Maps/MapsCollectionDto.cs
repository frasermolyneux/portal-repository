using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;

[Obsolete("Use CollectionModel<MapDto> from MX.Api.Abstractions instead")]
public record MapsCollectionDto
{
    public List<MapDto> Entries { get; set; } = [];
    public int TotalRecords { get; set; }
}
