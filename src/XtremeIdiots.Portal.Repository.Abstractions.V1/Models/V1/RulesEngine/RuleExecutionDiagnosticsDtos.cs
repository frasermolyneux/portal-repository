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
