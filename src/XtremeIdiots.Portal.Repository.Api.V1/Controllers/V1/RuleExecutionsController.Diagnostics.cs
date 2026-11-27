using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public sealed partial class RuleExecutionsController
{
    [HttpGet("rule-executions/audit")]
    [ProducesResponseType<ApiResponse<CollectionModel<RuleExecutionAuditDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchRuleExecutionAudit(
        [FromQuery] RuleExecutionAuditQueryDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).SearchRuleExecutionAudit(request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    async Task<ApiResult<CollectionModel<RuleExecutionAuditDto>>> IRuleExecutionsApi.SearchRuleExecutionAudit(
        RuleExecutionAuditQueryDto request, CancellationToken cancellationToken)
    {
        if (request is null || request.GameServerId == Guid.Empty ||
            request.FeatureId is { } feature && (string.IsNullOrWhiteSpace(feature) || feature.Length > 64) ||
            request.MatchStatus is { } matchStatus && !Enum.IsDefined(matchStatus) ||
            request.TriggerOccurrenceId == Guid.Empty ||
            request.SkipEntries < 0 || request.TakeEntries is <= 0 or > 100 ||
            request.FromUtc is { Kind: not DateTimeKind.Utc } || request.ToUtc is { Kind: not DateTimeKind.Utc } ||
            request.FromUtc is { } from && request.ToUtc is { } to && from >= to)
        {
            return new ApiResult<CollectionModel<RuleExecutionAuditDto>>(HttpStatusCode.BadRequest);
        }

        var scope = context.RuleFeatureExecutions.AsNoTracking().AsQueryable();
        if (request.GameServerId is { } serverId)
        {
            scope = scope.Where(item => item.GameServerId == serverId);
        }
        if (request.FeatureId is { } featureId)
        {
            scope = scope.Where(item => item.FeatureId == featureId);
        }
        if (request.MatchStatus is { } requestedMatchStatus)
        {
            var storedMatchStatus = requestedMatchStatus == RuleExecutionMatchStatus.Unknown
                ? null : requestedMatchStatus.ToString();
            scope = scope.Where(item => item.MatchStatus == storedMatchStatus);
        }
        if (request.TriggerOccurrenceId is { } occurrenceId)
        {
            scope = scope.Where(item => item.TriggerOccurrenceId == occurrenceId);
        }
        var total = await scope.CountAsync(cancellationToken).ConfigureAwait(false);
        if (request.FromUtc is { } fromUtc)
        {
            scope = scope.Where(item => item.CreatedAtUtc >= fromUtc);
        }
        if (request.ToUtc is { } toUtc)
        {
            scope = scope.Where(item => item.CreatedAtUtc < toUtc);
        }
        var filtered = await scope.CountAsync(cancellationToken).ConfigureAwait(false);
        var executions = await scope.OrderByDescending(item => item.CreatedAtUtc).ThenBy(item => item.ExecutionId)
            .Skip(request.SkipEntries).Take(request.TakeEntries)
            .Select(item => new
            {
                item.ExecutionId,
                item.GameServerId,
                item.ConnectionId,
                item.TriggerOccurrenceId,
                item.FeatureId,
                item.PlayerGuid,
                item.TriggerJson,
                item.State,
                item.MatchStatus,
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
                item.CoordinationJson,
                item.DiagnosticsJson,
                item.EvaluationEvidenceJson
            }).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var ids = executions.Select(item => item.ExecutionId).ToArray();
        var actions = await context.RuleActionExecutions.AsNoTracking().Where(item => ids.Contains(item.ExecutionId))
            .OrderBy(item => item.RuleIndex).ThenBy(item => item.ActionIndex).ThenBy(item => item.ActionExecutionId)
            .Select(item => new RuleActionExecution
            {
                ExecutionId = item.ExecutionId,
                CorrelationJson = item.CorrelationJson,
                CommandJson = item.CommandJson,
                LogicalEffectId = item.LogicalEffectId,
                ActionId = item.ActionId,
                RuleIndex = item.RuleIndex,
                ActionIndex = item.ActionIndex,
                ActionTypeId = item.ActionTypeId,
                Mode = item.Mode,
                Outcome = item.Outcome,
                ClaimedAtUtc = item.ClaimedAtUtc,
                OutcomeAtUtc = item.OutcomeAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc,
                Detail = item.Detail,
                AdminActionId = item.AdminActionId
            }).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var actionsByExecution = actions.ToLookup(item => item.ExecutionId);
        var preparations = await context.RulePreparedActions.AsNoTracking().Where(item => ids.Contains(item.ExecutionId))
            .OrderBy(item => item.RuleIndex).ThenBy(item => item.ActionIndex)
            .Select(item => new { item.ExecutionId, item.PlanJson })
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var preparationsByExecution = preparations.ToLookup(item => item.ExecutionId);
        var data = new CollectionModel<RuleExecutionAuditDto>(executions.Select(execution =>
        {
            var trigger = Deserialize<RuleTriggerEnvelope>(execution.TriggerJson);
            var coordination = execution.CoordinationJson is { } coordinationJson
                ? Deserialize<RuleExecutionCoordination>(coordinationJson) : null;
            return new RuleExecutionAuditDto
            {
                ExecutionId = execution.ExecutionId,
                GameServerId = execution.GameServerId,
                ConnectionId = execution.ConnectionId,
                FeatureId = execution.FeatureId,
                TriggerId = trigger.TriggerId,
                TriggerOccurrenceId = execution.TriggerOccurrenceId,
                PlayerGuid = execution.PlayerGuid,
                Username = trigger.Username,
                State = Enum.Parse<RuleFeatureExecutionState>(execution.State),
                MatchStatus = ToMatchStatus(execution.MatchStatus),
                CreatedAtUtc = Utc(execution.CreatedAtUtc),
                UpdatedAtUtc = Utc(execution.UpdatedAtUtc),
                Coordination = coordination is null ? null : new RuleExecutionAuditCoordinationDto
                {
                    DueAtUtc = coordination.DueAtUtc,
                    ExpiresAtUtc = coordination.ExpiresAtUtc,
                    WaitingReason = execution.State == nameof(RuleFeatureExecutionState.Waiting) ? coordination.WaitingReason : null
                },
                Diagnostics = execution.DiagnosticsJson is { } diagnostics ? Deserialize<RuleExecutionDiagnostic[]>(diagnostics) : [],
                EvaluationEvidence = execution.EvaluationEvidenceJson is { } evidence ? Deserialize<RuleEvaluationEvidence>(evidence) : null,
                Actions = actionsByExecution[execution.ExecutionId].Select(action =>
                {
                    var identity = Deserialize<RuleActionCommand>(action.CommandJson).DeliveryIdentity;
                    return new RuleExecutionAuditActionDto
                    {
                        RuleId = Deserialize<RuleActionCorrelation>(action.CorrelationJson).RuleId,
                        ActionId = action.ActionId,
                        RuleIndex = action.RuleIndex,
                        ActionIndex = action.ActionIndex,
                        ActionTypeId = action.ActionTypeId,
                        Mode = Enum.Parse<RuleExecutionMode>(action.Mode),
                        Outcome = Enum.Parse<RuleActionExecutionOutcome>(action.Outcome),
                        ClaimedAtUtc = Utc(action.ClaimedAtUtc),
                        OutcomeAtUtc = action.OutcomeAtUtc is { } outcomeAt ? Utc(outcomeAt) : null,
                        UpdatedAtUtc = Utc(action.UpdatedAtUtc),
                        Detail = action.Detail,
                        AdminActionId = action.AdminActionId,
                        MessageIndex = identity?.MessageIndex,
                        FragmentIndex = identity?.FragmentIndex
                    };
                }).ToArray(),
                PreparedActions = preparationsByExecution[execution.ExecutionId].Select(item =>
                {
                    var plan = WithDeliveryOutcome(Deserialize<RulePreparedActionDto>(item.PlanJson),
                        actionsByExecution[execution.ExecutionId].ToArray());
                    return new RuleExecutionAuditPreparationDto
                    {
                        ActionId = plan.ActionId,
                        RuleIndex = plan.RuleIndex,
                        ActionIndex = plan.ActionIndex,
                        Outcome = plan.Outcome,
                        Detail = plan.Detail,
                        CompletedFragmentCount = plan.CompletedFragmentCount,
                        TotalFragmentCount = plan.TotalFragmentCount
                    };
                }).ToArray()
            };
        }).ToList());
        return new ApiResponse<CollectionModel<RuleExecutionAuditDto>>(data)
        {
            Pagination = new ApiPagination(total, filtered, request.SkipEntries, request.TakeEntries)
        }.ToApiResult();
    }

    [HttpGet("game-servers/{gameServerId:guid}/rule-executions")]
    [ProducesResponseType<ApiResponse<CollectionModel<RuleFeatureExecutionDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchRuleExecutions(
        Guid gameServerId, [FromQuery] RuleFeatureExecutionQueryDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).SearchRuleExecutions(request with { GameServerId = gameServerId }, cancellationToken)
            .ConfigureAwait(false)).ToHttpResult();

    async Task<ApiResult<CollectionModel<RuleFeatureExecutionDto>>> IRuleExecutionsApi.SearchRuleExecutions(
        RuleFeatureExecutionQueryDto request, CancellationToken cancellationToken)
    {
        if (request is null || request.GameServerId == Guid.Empty || string.IsNullOrWhiteSpace(request.FeatureId) ||
            request.FeatureId.Length > 64 || request.SkipEntries < 0 || request.TakeEntries is <= 0 or > 100 ||
            request.FromUtc is { } from && request.ToUtc is { } to && from >= to)
        {
            return new ApiResult<CollectionModel<RuleFeatureExecutionDto>>(HttpStatusCode.BadRequest);
        }
        var scope = context.RuleFeatureExecutions.AsNoTracking().Where(item =>
            item.GameServerId == request.GameServerId && item.FeatureId == request.FeatureId);
        var total = await scope.CountAsync(cancellationToken).ConfigureAwait(false);
        if (request.FromUtc is { } fromUtc)
        {
            scope = scope.Where(item => item.CreatedAtUtc >= fromUtc);
        }
        if (request.ToUtc is { } toUtc)
        {
            scope = scope.Where(item => item.CreatedAtUtc < toUtc);
        }
        var filtered = await scope.CountAsync(cancellationToken).ConfigureAwait(false);
        var executions = await scope.OrderByDescending(item => item.CreatedAtUtc).ThenBy(item => item.ExecutionId)
            .Skip(request.SkipEntries).Take(request.TakeEntries).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var ids = executions.Select(item => item.ExecutionId).ToArray();
        var actions = await context.RuleActionExecutions.AsNoTracking().Where(item => ids.Contains(item.ExecutionId))
            .OrderBy(item => item.RuleIndex).ThenBy(item => item.ActionIndex).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var preparations = await context.RulePreparedActions.AsNoTracking().Where(item => ids.Contains(item.ExecutionId))
            .OrderBy(item => item.RuleIndex).ThenBy(item => item.ActionIndex).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var data = new CollectionModel<RuleFeatureExecutionDto>(executions.Select(execution => ToExecutionDto(execution,
            actions.Where(item => item.ExecutionId == execution.ExecutionId).ToArray(),
            preparations.Where(item => item.ExecutionId == execution.ExecutionId).ToArray())).ToList());
        return new ApiResponse<CollectionModel<RuleFeatureExecutionDto>>(data)
        {
            Pagination = new ApiPagination(total, filtered, request.SkipEntries, request.TakeEntries)
        }.ToApiResult();
    }

    private static bool ValidDiagnostics(RuleExecutionDiagnostic[]? diagnostics) =>
        diagnostics is null || diagnostics.Length <= RuleExecutionDiagnostic.MaximumCount && diagnostics.All(item =>
            item is not null && Enum.IsDefined(item.Code) && item.ObservedAtUtc != default &&
            ValidDiagnosticIdentifier(item.RuleId, 128) && ValidDiagnosticIdentifier(item.ActionId, 128) &&
            ValidDiagnosticIdentifier(item.TokenKey, 256) && ValidDiagnosticIdentifier(item.Source, 64) &&
            item.Detail is not { Length: > RuleExecutionDiagnostic.MaximumDetailLength } &&
            (item.Code == RuleExecutionDiagnosticCode.DiagnosticsOmitted ? item.OmittedCount is > 0 : item.OmittedCount is null));

    private static bool ValidDiagnosticIdentifier(string? value, int maximumLength) =>
        value is null || !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength;
}
