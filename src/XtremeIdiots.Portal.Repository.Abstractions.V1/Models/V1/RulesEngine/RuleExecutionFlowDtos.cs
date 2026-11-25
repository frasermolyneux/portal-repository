namespace XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum RuleExecutionStructureStatus
{
    Available,
    Missing,
    Malformed
}

public sealed record RuleExecutionFlowDto
{
    public RuleExecutionAuditDto Execution { get; init; } = new();
    public string RuleSetRevision { get; init; } = string.Empty;
    public RuleExecutionStructureStatus StructureStatus { get; init; }
    public RuleExecutionFlowRuleDto[] Rules { get; init; } = [];
}

public sealed record RuleExecutionFlowRuleDto
{
    public int RuleIndex { get; init; }
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public bool? Enabled { get; init; }
    public int? Order { get; init; }
    public string? AfterMatch { get; init; }
    public RuleExecutionFlowConditionDto Condition { get; init; } = new();
    public RuleExecutionFlowActionDto[] Actions { get; init; } = [];
}

public sealed record RuleExecutionFlowConditionDto
{
    public string? GroupOperator { get; init; }
    public string? ConditionId { get; init; }
    public string? DisplayName { get; init; }
    public string? Operator { get; init; }
    public RuleExecutionFlowParameterDto[] SafeParameters { get; init; } = [];
    public bool HasUnavailableParameters { get; init; }
    public RuleExecutionFlowConditionDto[] Conditions { get; init; } = [];
}

public sealed record RuleExecutionFlowParameterDto
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

public sealed record RuleExecutionFlowActionDto
{
    public int ActionIndex { get; init; }
    public string Id { get; init; } = string.Empty;
    public string TypeId { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string[]? DependsOnActionIds { get; init; }
}
