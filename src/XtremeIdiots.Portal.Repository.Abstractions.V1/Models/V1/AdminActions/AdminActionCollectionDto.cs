using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;

[Obsolete("Use CollectionModel<AdminActionDto> from MX.Api.Abstractions instead")]
public record AdminActionCollectionDto
{
    public List<AdminActionDto> Entries { get; set; } = [];
    public int TotalRecords { get; set; }
}
