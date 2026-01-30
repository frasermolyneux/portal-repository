using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

[Obsolete("Use CollectionModel<PlayerAnalyticEntryDto> from MX.Api.Abstractions instead")]
public record PlayerAnalyticEntryCollectionDto
{
    public List<PlayerAnalyticEntryDto> Entries { get; set; } = [];
    public int TotalRecords { get; set; }
}
