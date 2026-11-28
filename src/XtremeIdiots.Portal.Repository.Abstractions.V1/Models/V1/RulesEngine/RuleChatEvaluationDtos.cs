using System.Text.Json.Serialization;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

[Newtonsoft.Json.JsonConverter(typeof(ForwardCompatibleNewtonsoftEnumConverter<RuleChatSafetyCategory>))]
[JsonConverter(typeof(ForwardCompatibleEnumConverter<RuleChatSafetyCategory>))]
public enum RuleChatSafetyCategory
{
    Unrecognized = 0,
    Hate,
    Sexual,
    Violence,
    SelfHarm
}

public sealed record RuleChatCheckDetail
{
    public int RuleIndex { get; init; }
    public int[]? ConditionPath { get; init; }
    public string CheckId { get; init; } = string.Empty;
    public RuleChatSafetyCategory? Category { get; init; }
    public int? ObservedSeverity { get; init; }
    public int? Threshold { get; init; }
    public string? MatcherVersion { get; init; }
    public string? MatchedEntryId { get; init; }
}

public sealed record CreateRuleChatEvaluationDto
{
    public Guid AttemptId { get; init; }
    public Guid PlayerId { get; init; }
    public Guid OccurrenceId { get; init; }
    public string FeatureId { get; init; } = string.Empty;
    public string RuleSetRevision { get; init; } = string.Empty;
    public RuleExecutionMode Mode { get; init; }
    public DateTimeOffset EvaluatedAtUtc { get; init; }
    public RuleExecutionMatchStatus MatchStatus { get; init; }
    public RuleEvaluationEvidence Evidence { get; init; } = null!;
    public RuleChatCheckDetail[] CheckDetails { get; init; } = [];
}

public sealed record RuleChatEvaluationDto
{
    public Guid GameServerId { get; init; }
    public required CreateRuleChatEvaluationDto Evaluation { get; init; }
    public DateTimeOffset RecordedAtUtc { get; init; }
}

public sealed record RuleChatEvaluationQueryDto
{
    public Guid? GameServerId { get; init; }
    public Guid? PlayerId { get; init; }
    public Guid? OccurrenceId { get; init; }
    public string? FeatureId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public DateTime? BeforeRecordedAtUtc { get; init; }
    public Guid? AfterAttemptId { get; init; }
    public int SkipEntries { get; init; }
    public int TakeEntries { get; init; } = 50;
}
