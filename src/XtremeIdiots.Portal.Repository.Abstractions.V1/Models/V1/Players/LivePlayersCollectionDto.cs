using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

[Obsolete("Use CollectionModel<LivePlayerDto> from MX.Api.Abstractions instead")]
public record LivePlayersCollectionDto
{
    public List<LivePlayerDto> Entries { get; set; } = [];
    public int TotalRecords { get; set; }
}
