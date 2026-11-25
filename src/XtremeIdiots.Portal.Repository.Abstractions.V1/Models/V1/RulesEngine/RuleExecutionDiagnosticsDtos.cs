namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum RuleExecutionDiagnosticCode
{
    WaitingForContext,
    WaitingForProtection,
    WaitingForDelay,
    WaitingForDelivery,
    NoMatch,
    SuppressedByProtection,
    Expired,
    ConnectionUnavailable,
    SourceUnavailable,
    FallbackUsed,
    UnsupportedDelivery,
    AudienceUnavailable,
    AudienceEmpty,
    DiagnosticsOmitted
}

public sealed record RuleExecutionDiagnostic
{
    public const int MaximumCount = 256;
    public const int MaximumDetailLength = 1024;
    public RuleExecutionDiagnosticCode Code { get; init; }
    public string? RuleId { get; init; }
    public string? ActionId { get; init; }
    public string? TokenKey { get; init; }
    public string? Source { get; init; }
    public string? Detail { get; init; }
    public int? OmittedCount { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record RuleFeatureExecutionQueryDto
{
    public Guid GameServerId { get; init; }
    public string FeatureId { get; init; } = string.Empty;
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int SkipEntries { get; init; }
    public int TakeEntries { get; init; } = 20;
}

public sealed record RuleExecutionAuditQueryDto
{
    public Guid? GameServerId { get; init; }
    public string? FeatureId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int SkipEntries { get; init; }
    public int TakeEntries { get; init; } = 20;
}

public sealed record RuleExecutionAuditDto
{
    public Guid ExecutionId { get; init; }
    public Guid GameServerId { get; init; }
    public Guid ConnectionId { get; init; }
    public string FeatureId { get; init; } = string.Empty;
    public string TriggerId { get; init; } = string.Empty;
    public string? PlayerGuid { get; init; }
    public string? Username { get; init; }
    public RuleFeatureExecutionState State { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public RuleExecutionAuditCoordinationDto? Coordination { get; init; }
    public RuleExecutionDiagnostic[] Diagnostics { get; init; } = [];
    public RuleExecutionAuditActionDto[] Actions { get; init; } = [];
    public RuleExecutionAuditPreparationDto[] PreparedActions { get; init; } = [];
}

public sealed record RuleExecutionAuditCoordinationDto
{
    public DateTimeOffset DueAtUtc { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public string? WaitingReason { get; init; }
}

public sealed record RuleExecutionAuditActionDto
{
    public string RuleId { get; init; } = string.Empty;
    public string ActionId { get; init; } = string.Empty;
    public int RuleIndex { get; init; }
    public int ActionIndex { get; init; }
    public string ActionTypeId { get; init; } = string.Empty;
    public RuleExecutionMode Mode { get; init; }
    public RuleActionExecutionOutcome Outcome { get; init; }
    public DateTimeOffset ClaimedAtUtc { get; init; }
    public DateTimeOffset? OutcomeAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public string? Detail { get; init; }
    public Guid? AdminActionId { get; init; }
    public int? MessageIndex { get; init; }
    public int? FragmentIndex { get; init; }
}

public sealed record RuleExecutionAuditPreparationDto
{
    public string ActionId { get; init; } = string.Empty;
    public int RuleIndex { get; init; }
    public int ActionIndex { get; init; }
    public RuleActionExecutionOutcome Outcome { get; init; }
    public string? Detail { get; init; }
    public int CompletedFragmentCount { get; init; }
    public int TotalFragmentCount { get; init; }
}
