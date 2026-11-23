namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

public sealed record RecordPlayerIpObservationDto
{
    public string MessageId { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
}
