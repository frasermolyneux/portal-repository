namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Players;

public sealed record PlayerIpObservationResultDto
{
    public string Status { get; init; } = PlayerIpObservationStatuses.Unknown;
}

public static class PlayerIpObservationStatuses
{
    public const string Current = "Current";
    public const string NotCurrent = "NotCurrent";
    public const string Unknown = "Unknown";
}
