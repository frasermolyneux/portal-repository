using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

[Obsolete("Use CollectionModel<PlayerAliasDto> from MX.Api.Abstractions instead")]
public record PlayerAliasesCollectionDto
{
    public List<PlayerAliasDto> Entries { get; set; } = new List<PlayerAliasDto>();
    public int TotalRecords { get; set; }
}
