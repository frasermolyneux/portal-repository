using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;

[Obsolete("Use CollectionModel<PlayerTagDto> from MX.Api.Abstractions instead")]
public record PlayerTagsCollectionDto
{
    public List<PlayerTagDto> Entries { get; set; } = new List<PlayerTagDto>();
    public int TotalRecords { get; set; }
}
