using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

[Obsolete("Use CollectionModel<PlayerAnalyticPerGameEntryDto> from MX.Api.Abstractions instead")]
public record PlayerAnalyticPerGameEntryCollectionDto
{
    public List<PlayerAnalyticPerGameEntryDto> Entries { get; set; } = new List<PlayerAnalyticPerGameEntryDto>();
    public int TotalRecords { get; set; }
}
