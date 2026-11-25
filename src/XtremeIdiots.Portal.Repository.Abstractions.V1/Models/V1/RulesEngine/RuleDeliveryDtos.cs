using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

public sealed record RuleMessageRecipientTarget
{
    public Guid GameServerId { get; init; }
    public RuleRuntimeDescriptor Runtime { get; init; } = new();
    public Guid ConnectionId { get; init; }
    public ulong? ConnectionGeneration { get; init; }
    public string PlayerGuid { get; init; } = string.Empty;
    public int SlotId { get; init; }
}

public sealed record RuleDeliveryIdentity
{
    public string ParentLogicalEffectId { get; init; } = string.Empty;
    public Guid? RecipientConnectionId { get; init; }
    public int MessageIndex { get; init; }
    public int FragmentIndex { get; init; }

    public string ToLogicalEffectId() => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new object?[] { ParentLogicalEffectId, RecipientConnectionId, MessageIndex, FragmentIndex }))));
}

public sealed record RuleExecutionCoordination
{
    public DateTimeOffset DueAtUtc { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public Guid[] PredecessorExecutionIds { get; init; } = [];
    public string? WaitingReason { get; init; }
}

public sealed record PrepareRuleActionDto
{
    public long ExpectedVersion { get; init; }
    public int RuleIndex { get; init; }
    public int ActionIndex { get; init; }
    public string ResolvedParametersJson { get; init; } = "{}";
    public RuleActionCommand[] Commands { get; init; } = [];
    public RuleTaggedAudienceDto? AudienceSnapshot { get; init; }
}

public sealed record RulePreparedActionDto
{
    public Guid ExecutionId { get; init; }
    public int RuleIndex { get; init; }
    public int ActionIndex { get; init; }
    public string ActionId { get; init; } = string.Empty;
    public string ResolvedParametersJson { get; init; } = "{}";
    public RuleActionCommand[] Commands { get; init; } = [];
    public DateTimeOffset PreparedAtUtc { get; init; }
    public RuleTaggedAudienceDto? AudienceSnapshot { get; init; }
    public RuleExecutionCoordination? RuleCoordination { get; init; }
    public RuleActionExecutionOutcome Outcome { get; init; }
    public string? Detail { get; init; }
    public int CompletedFragmentCount { get; init; }
    public int TotalFragmentCount { get; init; }
}

[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleRecipientTagMatch
{
    Any = 0,
    All
}

public sealed record RuleTaggedAudienceRequestDto
{
    public Guid[] TagIds { get; init; } = [];
    public RuleRecipientTagMatch Match { get; init; }
}

public sealed record RuleTaggedAudienceDto
{
    public Guid GameServerId { get; init; }
    public Guid SnapshotId { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
    public bool Available { get; init; }
    public bool Partial { get; init; }
    public string? Detail { get; init; }
    public RuleMessageRecipientTarget[] Recipients { get; init; } = [];
}
