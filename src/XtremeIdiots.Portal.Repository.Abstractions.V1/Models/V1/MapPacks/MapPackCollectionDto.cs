using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapPacks;

[Obsolete("Use CollectionModel<MapPackDto> from MX.Api.Abstractions instead")]
public record MapPackCollectionDto
{
    public List<MapPackDto> Entries { get; set; } = new List<MapPackDto>();
    public int TotalRecords { get; set; }
}
