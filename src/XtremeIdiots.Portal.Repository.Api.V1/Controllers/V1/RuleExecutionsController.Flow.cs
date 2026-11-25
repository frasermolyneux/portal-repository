using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public sealed partial class RuleExecutionsController
{
    [HttpGet("rule-executions/{executionId:guid}/flow")]
    [ProducesResponseType<ApiResponse<RuleExecutionFlowDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRuleExecutionFlow(Guid executionId, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).GetRuleExecutionFlow(executionId, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    async Task<ApiResult<RuleExecutionFlowDto>> IRuleExecutionsApi.GetRuleExecutionFlow(Guid executionId, CancellationToken cancellationToken)
    {
        var entity = await context.RuleFeatureExecutions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ExecutionId == executionId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            return new ApiResult<RuleExecutionFlowDto>(HttpStatusCode.NotFound);
        }
        var execution = await ToDtoAsync(entity, cancellationToken).ConfigureAwait(false);
        var (status, rules) = ParseFlowRules(execution.FrozenRulesJson);
        return new ApiResponse<RuleExecutionFlowDto>(new RuleExecutionFlowDto
        {
            Execution = ToFlowAudit(execution),
            RuleSetRevision = execution.RuleSetRevision,
            StructureStatus = status,
            Rules = rules
        }).ToApiResult();
    }

    private static RuleExecutionAuditDto ToFlowAudit(RuleFeatureExecutionDto execution) => new()
    {
        ExecutionId = execution.ExecutionId,
        GameServerId = execution.GameServerId,
        ConnectionId = execution.ConnectionId,
        FeatureId = execution.FeatureId,
        TriggerId = execution.Trigger.TriggerId,
        PlayerGuid = execution.Trigger.PlayerGuid,
        Username = execution.Trigger.Username,
        State = execution.State,
        CreatedAtUtc = execution.CreatedAtUtc,
        UpdatedAtUtc = execution.UpdatedAtUtc,
        Coordination = execution.Coordination is { } coordination ? new RuleExecutionAuditCoordinationDto
        {
            DueAtUtc = coordination.DueAtUtc,
            ExpiresAtUtc = coordination.ExpiresAtUtc,
            WaitingReason = execution.State == RuleFeatureExecutionState.Waiting ? coordination.WaitingReason : null
        } : null,
        Diagnostics = execution.Diagnostics,
        Actions = execution.Actions.Select(action => new RuleExecutionAuditActionDto
        {
            RuleId = action.RuleId,
            ActionId = action.ActionId,
            RuleIndex = action.RuleIndex,
            ActionIndex = action.ActionIndex,
            ActionTypeId = action.ActionTypeId,
            Mode = action.Mode,
            Outcome = action.Outcome,
            ClaimedAtUtc = action.ClaimedAtUtc,
            OutcomeAtUtc = action.OutcomeAtUtc,
            UpdatedAtUtc = action.UpdatedAtUtc,
            Detail = action.Detail,
            AdminActionId = action.AdminActionId,
            MessageIndex = action.Command.DeliveryIdentity?.MessageIndex,
            FragmentIndex = action.Command.DeliveryIdentity?.FragmentIndex
        }).ToArray(),
        PreparedActions = execution.PreparedActions.Select(plan => new RuleExecutionAuditPreparationDto
        {
            ActionId = plan.ActionId,
            RuleIndex = plan.RuleIndex,
            ActionIndex = plan.ActionIndex,
            Outcome = plan.Outcome,
            Detail = plan.Detail,
            CompletedFragmentCount = plan.CompletedFragmentCount,
            TotalFragmentCount = plan.TotalFragmentCount
        }).ToArray()
    };

    private static (RuleExecutionStructureStatus Status, RuleExecutionFlowRuleDto[] Rules) ParseFlowRules(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return (RuleExecutionStructureStatus.Missing, []);
        }
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (RuleExecutionStructureStatus.Malformed, []);
            }
            if (!document.RootElement.TryGetProperty("rules", out var rulesElement) || rulesElement.ValueKind == JsonValueKind.Null)
            {
                return (RuleExecutionStructureStatus.Missing, []);
            }
            if (rulesElement.ValueKind != JsonValueKind.Array || rulesElement.GetArrayLength() > 2 * RuleSetConstants.MaxRules)
            {
                return (RuleExecutionStructureStatus.Malformed, []);
            }
            var rules = new List<RuleExecutionFlowRuleDto>();
            foreach (var element in rulesElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object ||
                    !element.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(id.GetString()) || id.GetString()!.Length > RuleSetConstants.MaxIdLength ||
                    !element.TryGetProperty("condition", out var conditionElement) || conditionElement.ValueKind != JsonValueKind.Object ||
                    !element.TryGetProperty("actions", out var actionElements) || actionElements.ValueKind != JsonValueKind.Array ||
                    actionElements.GetArrayLength() > RuleSetConstants.MaxActionsPerRule)
                {
                    return (RuleExecutionStructureStatus.Malformed, []);
                }
                var condition = JsonSerializer.Deserialize<RuleConditionNode>(conditionElement.GetRawText(), JsonOptions);
                var count = 0;
                if (!TryProjectCondition(condition, 1, ref count, out var projected))
                {
                    return (RuleExecutionStructureStatus.Malformed, []);
                }
                bool? enabled = null;
                if (element.TryGetProperty("enabled", out var enabledElement))
                {
                    if (enabledElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    {
                        return (RuleExecutionStructureStatus.Malformed, []);
                    }
                    enabled = enabledElement.GetBoolean();
                }
                int? order = null;
                if (element.TryGetProperty("order", out var orderElement))
                {
                    if (orderElement.ValueKind != JsonValueKind.Number || !orderElement.TryGetInt32(out var value))
                    {
                        return (RuleExecutionStructureStatus.Malformed, []);
                    }
                    order = value;
                }
                var actions = new List<RuleExecutionFlowActionDto>();
                foreach (var action in actionElements.EnumerateArray())
                {
                    if (action.ValueKind != JsonValueKind.Object ||
                        !action.TryGetProperty("id", out var actionId) || actionId.ValueKind != JsonValueKind.String ||
                        string.IsNullOrWhiteSpace(actionId.GetString()) || actionId.GetString()!.Length > RuleSetConstants.MaxIdLength ||
                        !action.TryGetProperty("typeId", out var typeId) || typeId.ValueKind != JsonValueKind.String ||
                        string.IsNullOrWhiteSpace(typeId.GetString()) || typeId.GetString()!.Length > RuleSetConstants.MaxIdLength)
                    {
                        return (RuleExecutionStructureStatus.Malformed, []);
                    }
                    string[]? dependencies = null;
                    if (action.TryGetProperty("dependsOnActionIds", out var dependenciesElement) &&
                        dependenciesElement.ValueKind != JsonValueKind.Null)
                    {
                        if (dependenciesElement.ValueKind != JsonValueKind.Array ||
                            dependenciesElement.GetArrayLength() > RuleSetConstants.MaxActionsPerRule ||
                            dependenciesElement.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String ||
                                string.IsNullOrWhiteSpace(item.GetString()) || item.GetString()!.Length > RuleSetConstants.MaxIdLength))
                        {
                            return (RuleExecutionStructureStatus.Malformed, []);
                        }
                        dependencies = dependenciesElement.EnumerateArray().Select(item => item.GetString()!).ToArray();
                    }
                    var type = typeId.GetString()!;
                    actions.Add(new RuleExecutionFlowActionDto
                    {
                        ActionIndex = actions.Count,
                        Id = actionId.GetString()!,
                        TypeId = type,
                        DisplayName = RulesEngineCatalogueV1.Current.Actions.FirstOrDefault(item => item.ActionTypeId == type)?.DisplayName,
                        DependsOnActionIds = dependencies
                    });
                }
                string? afterMatch = null;
                if (element.TryGetProperty("afterMatch", out var afterElement) && afterElement.ValueKind != JsonValueKind.Null)
                {
                    afterMatch = afterElement.ValueKind switch
                    {
                        JsonValueKind.String when afterElement.GetString() is "StopProcessingRules" or "ContinueToNextRule" => afterElement.GetString(),
                        JsonValueKind.Number when afterElement.TryGetInt32(out var value) && value == (int)RuleAfterMatch.StopProcessingRules => nameof(RuleAfterMatch.StopProcessingRules),
                        JsonValueKind.Number when afterElement.TryGetInt32(out var value) && value == (int)RuleAfterMatch.ContinueToNextRule => nameof(RuleAfterMatch.ContinueToNextRule),
                        _ => null
                    };
                    if (afterMatch is null)
                    {
                        return (RuleExecutionStructureStatus.Malformed, []);
                    }
                }
                rules.Add(new RuleExecutionFlowRuleDto
                {
                    RuleIndex = rules.Count,
                    Id = id.GetString()!,
                    Enabled = enabled,
                    Order = order,
                    AfterMatch = afterMatch,
                    Condition = projected!,
                    Actions = actions.ToArray()
                });
            }
            return (RuleExecutionStructureStatus.Available, rules.ToArray());
        }
        catch (JsonException)
        {
            return (RuleExecutionStructureStatus.Malformed, []);
        }
    }

    private static bool TryProjectCondition(RuleConditionNode? node, int depth, ref int count, out RuleExecutionFlowConditionDto? result)
    {
        result = null;
        if (node is null || depth > RuleConditionTreeConstants.MaxDepth || ++count > RuleConditionTreeConstants.MaxConditionsPerRule)
        {
            return false;
        }
        if (node.GroupOperator is { } group)
        {
            if (group is not (RuleConditionGroupOperator.And or RuleConditionGroupOperator.Or) ||
                node.ConditionId is not null || node.Parameters is not null || node.Conditions is not { Count: > 0 })
            {
                return false;
            }
            var children = new List<RuleExecutionFlowConditionDto>();
            foreach (var child in node.Conditions)
            {
                if (!TryProjectCondition(child, depth + 1, ref count, out var projected))
                {
                    return false;
                }
                children.Add(projected!);
            }
            result = new() { GroupOperator = group.ToString(), Conditions = children.ToArray() };
            return true;
        }
        if (node.Conditions is not null || string.IsNullOrWhiteSpace(node.ConditionId) ||
            node.ConditionId.Length > RuleSetConstants.MaxIdLength)
        {
            return false;
        }
        var definition = RulesEngineCatalogueV1.Current.Conditions.FirstOrDefault(item => item.ConditionId == node.ConditionId);
        var parameters = node.Parameters ?? new Dictionary<string, JsonElement>();
        string? operation = null;
        if (parameters.TryGetValue("operator", out var operationElement) && operationElement.ValueKind == JsonValueKind.String &&
            definition?.Operators.Contains(operationElement.GetString()!, StringComparer.Ordinal) == true)
        {
            operation = operationElement.GetString();
        }
        var safe = GetSafeConditionParameters(node.ConditionId, parameters, operation);
        result = new()
        {
            ConditionId = node.ConditionId,
            DisplayName = definition?.DisplayName,
            Operator = operation,
            SafeParameters = safe,
            HasUnavailableParameters = parameters.Keys.Any(key =>
                key == "operator" ? operation is null : safe.All(item => item.Name != key))
        };
        return true;
    }

    private static RuleExecutionFlowParameterDto[] GetSafeConditionParameters(
        string conditionId, IReadOnlyDictionary<string, JsonElement> parameters, string? operation)
    {
        var safe = new List<RuleExecutionFlowParameterDto>();
        var definition = RulesEngineCatalogueV1.Current.Conditions.FirstOrDefault(item => item.ConditionId == conditionId);
        foreach (var name in new[] { "signal", "referenceScope" })
        {
            var allowed = definition?.Parameters.FirstOrDefault(item => item.Name == name)?.AllowedValues;
            if (allowed is not null && parameters.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String &&
                allowed.Contains(value.GetString()!, StringComparer.Ordinal))
            {
                safe.Add(new() { Name = name, Value = value.GetString()! });
            }
        }
        var durationName = conditionId == "player.profile-age" && operation is not null ? "duration" :
            conditionId is "player.first-seen-guid" or "player.first-seen-ip" && operation == "WithinAge" ? "value" : null;
        if (durationName is not null && parameters.TryGetValue(durationName, out var durationElement) &&
            durationElement.ValueKind == JsonValueKind.String &&
            TimeSpan.TryParse(durationElement.GetString(), CultureInfo.InvariantCulture, out var duration) &&
            duration > TimeSpan.Zero && duration <= TimeSpan.FromDays(36500))
        {
            safe.Add(new() { Name = durationName, Value = duration.ToString("c", CultureInfo.InvariantCulture) });
        }
        if (conditionId == "vpn.intelligence" && safe.FirstOrDefault(item => item.Name == "signal") is { } signal &&
            parameters.TryGetValue("expectedValue", out var expected) && expected.ValueKind == JsonValueKind.String)
        {
            var text = expected.GetString();
            if (signal.Value is "ProxyCheckIsProxy" or "ProxyCheckIsVpn" or "MaxMindIsAnonymous" or
                "MaxMindIsAnonymousVpn" or "MaxMindIsHostingProvider" or "MaxMindIsPublicProxy" or
                "MaxMindIsResidentialProxy" or "MaxMindIsTorExitNode" or "IsPartial" &&
                operation is "Equal" or "NotEqual" && bool.TryParse(text, out var boolean))
            {
                safe.Add(new() { Name = "expectedValue", Value = boolean ? "true" : "false" });
            }
            else if (signal.Value is "ProxyCheckRiskScore" or "MaxMindAnonymizerConfidence" &&
                operation is "Equal" or "NotEqual" or "GreaterThan" or "GreaterThanOrEqual" or "LessThan" or "LessThanOrEqual" &&
                int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number is >= 0 and <= 100)
            {
                safe.Add(new() { Name = "expectedValue", Value = number.ToString(CultureInfo.InvariantCulture) });
            }
        }
        return safe.ToArray();
    }
}
