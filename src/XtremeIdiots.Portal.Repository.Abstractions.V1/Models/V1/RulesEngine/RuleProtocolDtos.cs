using System.Text.Json;
using System.Text.Json.Serialization;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleExecutionMode
{
    Unknown = 0,
    Enforce,
    DryRun
}

[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleActionExecutionOutcome
{
    Pending = 0,
    Claimed,
    Dispatched,
    Applied,
    Failed,
    Stale,
    SkippedUnsupported,
    SkippedDependency,
    WouldExecute,
    OutcomeUnknown
}

[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleFeatureExecutionState
{
    Evaluating = 0,
    AwaitingActionOutcome,
    Completed,
    Stopped
}

[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleNativeEffectType
{
    Unknown = 0,
    DropPlayer,
    PublicMessage,
    PrivateMessage
}

public sealed record RuleRuntimeDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public int ProtocolVersion { get; init; } = 1;
    public string[] Capabilities { get; init; } = [];
}

public sealed record RuleTriggerEnvelope
{
    public int ProtocolVersion { get; init; } = 1;
    public Guid ObservationId { get; init; }
    public string TriggerId { get; init; } = string.Empty;
    public Guid TriggerOccurrenceId { get; init; }
    public DateTimeOffset OccurredAtUtc { get; init; }
    public Guid GameServerId { get; init; }
    public int GameType { get; init; }
    public Guid ConnectionId { get; init; }
    public ulong ConnectionGeneration { get; init; }
    public string? PlayerGuid { get; init; }
    public string? Username { get; init; }
    public string? PlayerIpAddress { get; init; }
    public int? SlotId { get; init; }
    public RuleRuntimeDescriptor? Runtime { get; init; }
}

public sealed record RuleActionCorrelation
{
    public string TriggerId { get; init; } = string.Empty;
    public Guid TriggerOccurrenceId { get; init; }
    public Guid ConnectionId { get; init; }
    public string FeatureId { get; init; } = string.Empty;
    public string RuleId { get; init; } = string.Empty;
    public string RuleSetRevision { get; init; } = string.Empty;
    public Guid? SiegeSessionId { get; init; }
    public string ActionId { get; init; } = string.Empty;
}

public sealed record RuleNativeEffect
{
    public RuleNativeEffectType Type { get; init; }
    public string? Reason { get; init; }
    public string? Message { get; init; }
}

public sealed record RuleActionCommand
{
    public int ProtocolVersion { get; init; } = 1;
    public Guid ExecutionId { get; init; }
    public string ActionId { get; init; } = string.Empty;
    public string LogicalEffectId { get; init; } = string.Empty;
    public RuleActionCorrelation Correlation { get; init; } = new();
    public Guid GameServerId { get; init; }
    public int SlotId { get; init; }
    public string PlayerGuid { get; init; } = string.Empty;
    public Guid ConnectionId { get; init; }
    public ulong ConnectionGeneration { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public RuleExecutionMode Mode { get; init; }
    public RuleNativeEffect Effect { get; init; } = new();
}

public sealed record RuleActionResult
{
    public int ProtocolVersion { get; init; } = 1;
    public Guid ExecutionId { get; init; }
    public string ActionId { get; init; } = string.Empty;
    public string LogicalEffectId { get; init; } = string.Empty;
    public RuleActionCorrelation Correlation { get; init; } = new();
    public RuleActionExecutionOutcome Outcome { get; init; }
    public DateTimeOffset OccurredAtUtc { get; init; }
    public string? Detail { get; init; }
}

public sealed record RuleEvaluationResponse
{
    public int ProtocolVersion { get; init; } = 1;
    public RuleActionCommand[] Commands { get; init; } = [];
    public Guid[] ExecutionIds { get; init; } = [];
}

public sealed record RulePollRequest
{
    public int ProtocolVersion { get; init; } = 1;
    public Guid GameServerId { get; init; }
    public Guid ConnectionId { get; init; }
    public ulong ConnectionGeneration { get; init; }
    public string PlayerGuid { get; init; } = string.Empty;
    public int SlotId { get; init; }
    public Guid[] ExecutionIds { get; init; } = [];
}

public sealed record RuleFeatureExecutionDto
{
    public Guid ExecutionId { get; init; }
    public Guid GameServerId { get; init; }
    public Guid ConnectionId { get; init; }
    public string FeatureId { get; init; } = string.Empty;
    public Guid? SiegeSessionId { get; init; }
    public string RuleSetRevision { get; init; } = string.Empty;
    public string FrozenRulesJson { get; init; } = string.Empty;
    public RuleTriggerEnvelope Trigger { get; init; } = new();
    public Guid? FactSnapshotId { get; init; }
    public RuleFeatureExecutionState State { get; init; }
    public int RuleIndex { get; init; }
    public int ActionIndex { get; init; }
    public string? DecisionSnapshotJson { get; init; }
    public RuleActionExecutionDto[] Actions { get; init; } = [];
    public long Version { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}

public sealed record StartRuleFeatureExecutionDto
{
    public RuleTriggerEnvelope Trigger { get; init; } = new();
    public string FeatureId { get; init; } = string.Empty;
    public Guid? SiegeSessionId { get; init; }
    public string RuleSetRevision { get; init; } = string.Empty;
    public string FrozenRulesJson { get; init; } = string.Empty;
    public Guid? FactSnapshotId { get; init; }
}

public sealed record UpdateRuleFeatureExecutionProgressDto
{
    public long ExpectedVersion { get; init; }
    public int RuleIndex { get; init; }
    public int ActionIndex { get; init; }
    public RuleFeatureExecutionState State { get; init; }
    public string? DecisionSnapshotJson { get; init; }
    public Guid? FactSnapshotId { get; init; }
}

public sealed record ClaimRuleActionDto
{
    public long ExpectedVersion { get; init; }
    public int RuleIndex { get; init; }
    public int ActionIndex { get; init; }
    public string LogicalEffectId { get; init; } = string.Empty;
    public RuleActionCommand Command { get; init; } = new();
}

public sealed record RuleActionExecutionDto
{
    public Guid ExecutionId { get; init; }
    public string LogicalEffectId { get; init; } = string.Empty;
    public RuleActionCorrelation Correlation { get; init; } = new();
    public RuleActionCommand Command { get; init; } = new();
    public Guid GameServerId { get; init; }
    public string ActionTypeId { get; init; } = string.Empty;
    public string ParametersJson { get; init; } = string.Empty;
    public RuleExecutionMode Mode { get; init; }
    public RuleActionExecutionOutcome Outcome { get; init; }
    public DateTimeOffset ClaimedAtUtc { get; init; }
    public DateTimeOffset? OutcomeAtUtc { get; init; }
    public string? ResultJson { get; init; }
    public string? Detail { get; init; }
}

public sealed record RuleActionClaimResultDto
{
    public RuleActionExecutionDto Action { get; init; } = new();
    public RuleFeatureExecutionDto Execution { get; init; } = new();
    public bool ShouldDispatch { get; init; }
}

public sealed record UpdateRuleActionOutcomeDto
{
    public long ExpectedVersion { get; init; }
    public RuleActionResult Result { get; init; } = new();
    public int NextRuleIndex { get; init; }
    public int NextActionIndex { get; init; }
    public RuleFeatureExecutionState NextState { get; init; }
    public string? DecisionSnapshotJson { get; init; }
}

public sealed record RuleFactSnapshotRequestDto
{
    public int ProtocolVersion { get; init; } = 1;
    public Guid ObservationId { get; init; }
    public Guid ConnectionId { get; init; }
    public int GameType { get; init; }
    public string? PlayerGuid { get; init; }
    public string? PlayerIpAddress { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
}

public sealed record RuleFactSnapshotDto
{
    public Guid SnapshotId { get; init; }
    public Guid GameServerId { get; init; }
    public Guid ConnectionId { get; init; }
    public int GameType { get; init; }
    public string? PlayerGuid { get; init; }
    public string? PlayerIpAddress { get; init; }
    public DateTimeOffset CapturedAtUtc { get; init; }
    public DateTimeOffset? GuidFirstSeenAtUtc { get; init; }
    public DateTimeOffset? IpFirstSeenAtUtc { get; init; }
    public DateTimeOffset? ProfileFirstSeenAtUtc { get; init; }
    public Guid[] TagIds { get; init; } = [];
    public bool GuidHistoryAvailable { get; init; }
    public bool IpHistoryAvailable { get; init; }
    public bool TagsAvailable { get; init; }
}
