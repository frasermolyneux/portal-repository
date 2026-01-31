using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

[Obsolete("Use CollectionModel<PlayerDto> from MX.Api.Abstractions instead")]
public record PlayersCollectionDto
{
    public List<PlayerDto> Entries { get; set; } = [];
    public int TotalRecords { get; set; }
}
