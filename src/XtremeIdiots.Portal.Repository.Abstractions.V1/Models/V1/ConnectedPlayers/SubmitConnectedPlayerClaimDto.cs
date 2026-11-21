namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;

public record SubmitConnectedPlayerClaimDto
{
    public Guid RequestId { get; init; }
    public Guid PlayerId { get; init; }
    public Guid GameServerId { get; init; }
    public string Code { get; init; } = string.Empty;
}
