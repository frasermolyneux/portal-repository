using System.Text.Json.Serialization;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

[Newtonsoft.Json.JsonConverter(typeof(ForwardCompatibleNewtonsoftEnumConverter<RuleOccurrenceEffectDisposition>))]
[JsonConverter(typeof(ForwardCompatibleEnumConverter<RuleOccurrenceEffectDisposition>))]
public enum RuleOccurrenceEffectDisposition
{
    Unrecognized = 0,
    Acquired,
    AlreadyClaimed,
    CooldownSuppressed
}

[Newtonsoft.Json.JsonConverter(typeof(ForwardCompatibleNewtonsoftEnumConverter<RuleOccurrenceEffectOutcome>))]
[JsonConverter(typeof(ForwardCompatibleEnumConverter<RuleOccurrenceEffectOutcome>))]
public enum RuleOccurrenceEffectOutcome
{
    Unrecognized = 0,
    Claimed,
    CooldownSuppressed,
    Dispatched,
    Applied,
    Failed,
    OutcomeUnknown
}

public sealed record RuleOccurrenceCooldownDto
{
    public string PolicyId { get; init; } = string.Empty;
    public int DurationSeconds { get; init; }
}

public sealed record ClaimRuleOccurrenceEffectDto
{
    public string LogicalEffectId { get; init; } = string.Empty;
    public Guid PlayerId { get; init; }
    public string FeatureId { get; init; } = string.Empty;
    public string RuleId { get; init; } = string.Empty;
    public int RuleIndex { get; init; }
    public string ActionId { get; init; } = string.Empty;
    public int ActionIndex { get; init; }
    public string ActionTypeId { get; init; } = string.Empty;
    public string RuleSetRevision { get; init; } = string.Empty;
    public RuleExecutionMode Mode { get; init; }
    public RuleOccurrenceCooldownDto? Cooldown { get; init; }
}

public sealed record EnsureRuleOccurrenceAdminActionDto
{
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AdminActionType? Type { get; init; }
    public string? Text { get; init; }
    public DateTime? Expires { get; init; }
    public string? AdminId { get; init; }
    public Guid? ExistingAdminActionId { get; init; }
}

public sealed record RuleOccurrenceEffectDto
{
    public Guid GameServerId { get; init; }
    public Guid OccurrenceId { get; init; }
    public required ClaimRuleOccurrenceEffectDto Claim { get; init; }
    public bool AdminActionRecorded { get; init; }
    public Guid? AdminActionId { get; init; }
    public RuleOccurrenceEffectOutcome Outcome { get; init; }
    public DateTimeOffset? CooldownUntilUtc { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}

public sealed record ClaimRuleOccurrenceEffectResultDto
{
    public RuleOccurrenceEffectDisposition Disposition { get; init; }
    public bool ShouldDispatch { get; init; }
    public required RuleOccurrenceEffectDto Effect { get; init; }
}

public sealed record UpdateRuleOccurrenceEffectOutcomeDto
{
    public RuleOccurrenceEffectOutcome Outcome { get; init; }
}
