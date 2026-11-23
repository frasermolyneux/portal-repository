using System.ComponentModel.DataAnnotations;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.SiegeMode;

[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum SiegeSessionMode
{
    Unknown = 0,
    Enforce,
    DryRun
}

public static class SiegeActionOutcomes
{
    public const string Pending = "Pending";
    public const string Queued = "Queued";
    public const string Dispatched = "Dispatched";
    public const string Applied = "Applied";
    public const string Failed = "Failed";
    public const string Stale = "Stale";
    public const string WouldKick = "WouldKick";
}

public sealed record SiegeSessionDto
{
    public Guid SessionId { get; init; }
    public Guid GameServerId { get; init; }
    public int GameType { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public DateTime? EndedAtUtc { get; init; }
    public string StartedBy { get; init; } = string.Empty;
    public SiegeSessionMode Mode { get; init; }
    public string SettingsJson { get; init; } = string.Empty;
    public int ActionCount { get; init; }
}

public sealed record StartSiegeSessionDto
{
    public Guid OperationId { get; init; }
    public string Actor { get; init; } = string.Empty;
    public int ExpectedDurationMinutes { get; init; }
    public SiegeSessionMode Mode { get; init; }
}

public sealed record ChangeSiegeSessionDto
{
    public Guid OperationId { get; init; }
    public string Actor { get; init; } = string.Empty;
    public int? ExtensionMinutes { get; init; }
}

public sealed record SiegeBaselineRequestDto
{
    public string? PlayerGuid { get; init; }
    public string? IpAddress { get; init; }
}

public sealed record SiegeBaselineDto
{
    public bool? NewGuid { get; init; }
    public bool? NewIp { get; init; }
    public int BaselineRevision { get; init; }
}

public sealed record ClaimSiegeActionDto
{
    public Guid ActionId { get; init; }
    public Guid ConnectionId { get; init; }
    public string PlayerGuid { get; init; } = string.Empty;
    [Required]
    public int? SlotId { get; init; }
    public string RuleId { get; init; } = string.Empty;
    [Required]
    public int? BaselineRevision { get; init; }
}

public sealed record SiegeActionDto
{
    public Guid ActionId { get; init; }
    public Guid SessionId { get; init; }
    public Guid GameServerId { get; init; }
    public int GameType { get; init; }
    public SiegeSessionMode Mode { get; init; }
    public Guid ConnectionId { get; init; }
    public string PlayerGuid { get; init; } = string.Empty;
    public int SlotId { get; init; }
    public string RuleId { get; init; } = string.Empty;
    public DateTime ClaimedAtUtc { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public DateTime? OutcomeAtUtc { get; init; }
    public string? Detail { get; init; }
}

public sealed record SiegeActionClaimDto
{
    public SiegeActionDto Action { get; init; } = new();
    public bool ShouldDispatch { get; init; }
}

public sealed record SiegeActionSummaryDto
{
    public SiegeActionOutcomeCountDto[] Counts { get; init; } = [];
}

public sealed record SiegeActionOutcomeCountDto
{
    public SiegeSessionMode Mode { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record UpdateSiegeActionOutcomeDto
{
    public Guid ConnectionId { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public string? Detail { get; init; }
}

public sealed record RecordSiegeCheckDto
{
    public Guid CheckId { get; init; }
    public Guid? ConnectionId { get; init; }
    public string? PlayerGuid { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}

public sealed record SiegeCheckDto
{
    public Guid CheckId { get; init; }
    public Guid SessionId { get; init; }
    public Guid GameServerId { get; init; }
    public int GameType { get; init; }
    public SiegeSessionMode Mode { get; init; }
    public Guid? ConnectionId { get; init; }
    public string? PlayerGuid { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}

public sealed record SiegeSessionEventDto
{
    public Guid OperationId { get; init; }
    public Guid SessionId { get; init; }
    public SiegeSessionMode Mode { get; init; }
    public string EventType { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; }
    public string Actor { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public int? ExtensionMinutes { get; init; }
}

public sealed record SiegeAuditQueryDto
{
    public Guid[] GameServerIds { get; init; } = [];
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int SkipEntries { get; init; }
    public int TakeEntries { get; init; } = 20;
}
