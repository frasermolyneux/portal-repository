using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

[Obsolete("Use CollectionModel<ProtectedNameDto> from MX.Api.Abstractions instead")]
public record ProtectedNamesCollectionDto
{
    public List<ProtectedNameDto> Entries { get; set; } = new List<ProtectedNameDto>();
    public int TotalRecords { get; set; }
}
