using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;

[Obsolete("Use CollectionModel<GameServerStatDto> from MX.Api.Abstractions instead")]
public record GameServerStatCollectionDto
{
    public List<GameServerStatDto> Entries { get; set; } = [];
    public int TotalRecords { get; set; }
}
