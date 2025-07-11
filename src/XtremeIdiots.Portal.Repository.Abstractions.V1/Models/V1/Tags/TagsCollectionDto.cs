using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;

[Obsolete("Use CollectionModel<TagDto> from MX.Api.Abstractions instead")]
public record TagsCollectionDto
{
    public List<TagDto> Entries { get; set; } = new List<TagDto>();
    public int TotalRecords { get; set; }
}
