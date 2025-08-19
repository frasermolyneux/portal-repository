using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

[Obsolete("Use CollectionModel<GameServerDto> from MX.Api.Abstractions instead")]
public record GameServersCollectionDto
{
    public List<GameServerDto> Entries { get; set; } = new List<GameServerDto>();
    public int TotalRecords { get; set; }
}
