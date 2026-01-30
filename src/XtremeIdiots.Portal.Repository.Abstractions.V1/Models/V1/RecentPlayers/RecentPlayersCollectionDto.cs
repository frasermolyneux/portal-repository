using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RecentPlayers;

[Obsolete("Use CollectionModel<RecentPlayerDto> from MX.Api.Abstractions instead")]
public record RecentPlayersCollectionDto
{
    public List<RecentPlayerDto> Entries { get; set; } = [];
    public int TotalRecords { get; set; }
}
