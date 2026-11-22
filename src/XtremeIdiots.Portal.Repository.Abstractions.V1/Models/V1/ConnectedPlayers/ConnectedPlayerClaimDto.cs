using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;

public record ConnectedPlayerClaimDto : IDto
{
    public Guid ClaimId { get; init; }
    public Guid UserProfileId { get; init; }
    public Guid PlayerId { get; init; }
    public Guid GameServerId { get; init; }
    public string? GameServerName { get; init; }
    public string Username { get; init; } = string.Empty;
    public GameType GameType { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }
    public Guid? ConnectedPlayerProfileId { get; init; }
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Dictionary<string, string> TelemetryProperties => [];
}
