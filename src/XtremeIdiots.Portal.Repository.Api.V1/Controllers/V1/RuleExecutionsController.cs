using System.Data;
using System.Net;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public sealed class RuleExecutionsController(PortalDbContext context) : ControllerBase, IRuleExecutionsApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan LegacyObservationPrecisionMargin = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan NativeTimestampSkewAllowance = TimeSpan.FromSeconds(30);

    [HttpPost("game-servers/{gameServerId:guid}/rule-executions")]
    [ProducesResponseType<ApiResponse<RuleFeatureExecutionDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartExecution(Guid gameServerId, StartRuleFeatureExecutionDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).StartExecution(gameServerId, request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    [HttpGet("game-servers/{gameServerId:guid}/rule-executions/{executionId:guid}")]
    [ProducesResponseType<ApiResponse<RuleFeatureExecutionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExecution(Guid gameServerId, Guid executionId, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).GetExecution(gameServerId, executionId, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    [HttpPut("game-servers/{gameServerId:guid}/rule-executions/{executionId:guid}/progress")]
    [ProducesResponseType<ApiResponse<RuleFeatureExecutionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProgress(Guid gameServerId, Guid executionId, UpdateRuleFeatureExecutionProgressDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).UpdateProgress(gameServerId, executionId, request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    [HttpPost("game-servers/{gameServerId:guid}/rule-executions/{executionId:guid}/actions")]
    [ProducesResponseType<ApiResponse<RuleActionClaimResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClaimAction(Guid gameServerId, Guid executionId, ClaimRuleActionDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).ClaimAction(gameServerId, executionId, request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    [HttpPost("game-servers/{gameServerId:guid}/rule-executions/{executionId:guid}/actions/{actionId}/admin-action")]
    [ProducesResponseType<ApiResponse<EnsureAutomatedActionResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<EnsureAutomatedActionResultDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EnsureRuleActionAdminAction(Guid gameServerId, Guid executionId, string actionId, EnsureRuleActionAdminActionDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).EnsureRuleActionAdminAction(gameServerId, executionId, actionId, request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    [HttpPut("game-servers/{gameServerId:guid}/rule-executions/{executionId:guid}/actions/{actionId}/outcome")]
    [ProducesResponseType<ApiResponse<RuleActionClaimResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateActionOutcome(Guid gameServerId, Guid executionId, string actionId, UpdateRuleActionOutcomeDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).UpdateActionOutcome(gameServerId, executionId, actionId, request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    [HttpPost("game-servers/{gameServerId:guid}/rule-fact-snapshots")]
    [ProducesResponseType<ApiResponse<RuleFactSnapshotDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateFactSnapshot(Guid gameServerId, RuleFactSnapshotRequestDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).CreateFactSnapshot(gameServerId, request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    [HttpPost("rule-actions/search")]
    [ProducesResponseType<ApiResponse<CollectionModel<RuleActionExecutionDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchRuleActions(RuleActionAuditQueryDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).SearchRuleActions(request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    async Task<ApiResult<CollectionModel<RuleActionExecutionDto>>> IRuleExecutionsApi.SearchRuleActions(
        RuleActionAuditQueryDto request, CancellationToken cancellationToken)
    {
        if (!ValidRuleActionAuditQuery(request))
        {
            return new ApiResult<CollectionModel<RuleActionExecutionDto>>(HttpStatusCode.BadRequest);
        }

        var authorizedScope = context.RuleActionExecutions.AsNoTracking()
            .Where(action => request.GameServerIds.Contains(action.GameServerId));
        var totalCount = await authorizedScope.CountAsync(cancellationToken).ConfigureAwait(false);
        var filteredCount = await ApplyRuleActionAuditFilters(authorizedScope, request)
            .CountAsync(cancellationToken).ConfigureAwait(false);
        var dataQuery = context.RuleActionExecutions.AsNoTracking()
            .Where(action => request.GameServerIds.Contains(action.GameServerId));
        var actions = await ApplyRuleActionAuditFilters(dataQuery, request)
            .OrderByDescending(action => action.ClaimedAtUtc)
            .ThenBy(action => action.ActionExecutionId)
            .Skip(request.SkipEntries)
            .Take(request.TakeEntries)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var data = new CollectionModel<RuleActionExecutionDto>(
            actions.Select(action => ToActionDto(action)).ToList());

        return new ApiResponse<CollectionModel<RuleActionExecutionDto>>(data)
        {
            Pagination = new ApiPagination(totalCount, filteredCount, request.SkipEntries, request.TakeEntries)
        }.ToApiResult();
    }

    async Task<ApiResult<RuleFeatureExecutionDto>> IRuleExecutionsApi.StartExecution(
        Guid gameServerId, StartRuleFeatureExecutionDto request, CancellationToken cancellationToken)
    {
        if (!ValidStartRequest(gameServerId, request))
        {
            return new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.BadRequest);
        }

        return await ExecuteMutationAsync(async () =>
        {
            var siegeSessionKey = request.SiegeSessionId?.ToString("N") ?? "none";
            if (!await AcquireTransactionLockAsync(
                    $"rules-execution:{gameServerId:N}:{request.Trigger.ConnectionId:N}:{request.FeatureId}:{siegeSessionKey}",
                    cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.Conflict);
            }
            var serverGameType = await context.GameServers.AsNoTracking()
                .Where(s => s.GameServerId == gameServerId && !s.Deleted)
                .Select(s => (int?)s.GameType)
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (serverGameType is null)
            {
                return new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.NotFound);
            }
            if (serverGameType.Value != (int)request.Trigger.GameType)
            {
                return new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.Conflict);
            }
            if (request.SiegeSessionId is { } siegeSessionId &&
                !await context.SiegeSessions.AnyAsync(s => s.SessionId == siegeSessionId && s.GameServerId == gameServerId, cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.Conflict);
            }
            if (request.FactSnapshotId is { } factSnapshotId &&
                !await context.RuleFactSnapshots.AnyAsync(f => f.SnapshotId == factSnapshotId && f.GameServerId == gameServerId &&
                    f.ConnectionId == request.Trigger.ConnectionId, cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.Conflict);
            }

            var existing = await context.RuleFeatureExecutions.SingleOrDefaultAsync(
                e => e.GameServerId == gameServerId && e.ConnectionId == request.Trigger.ConnectionId &&
                    e.FeatureId == request.FeatureId && e.SiegeSessionId == request.SiegeSessionId,
                cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                return SameConnectionIdentity(Deserialize<RuleTriggerEnvelope>(existing.TriggerJson), request.Trigger)
                    ? new ApiResponse<RuleFeatureExecutionDto>(await ToDtoAsync(existing, cancellationToken).ConfigureAwait(false)).ToApiResult()
                    : new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.Conflict);
            }

            var now = DateTime.UtcNow;
            var execution = new RuleFeatureExecution
            {
                ExecutionId = Guid.NewGuid(),
                GameServerId = gameServerId,
                ConnectionId = request.Trigger.ConnectionId,
                TriggerOccurrenceId = request.Trigger.TriggerOccurrenceId,
                FeatureId = request.FeatureId,
                SiegeSessionId = request.SiegeSessionId,
                RuleSetRevision = request.RuleSetRevision,
                FrozenRulesJson = request.FrozenRulesJson,
                TriggerJson = JsonSerializer.Serialize(request.Trigger, JsonOptions),
                FactSnapshotId = request.FactSnapshotId,
                State = RuleFeatureExecutionState.Evaluating.ToString(),
                RuleIndex = 0,
                ActionIndex = 0,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            context.RuleFeatureExecutions.Add(execution);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse<RuleFeatureExecutionDto>(await ToDtoAsync(execution, cancellationToken).ConfigureAwait(false))
                .ToApiResult(HttpStatusCode.Created);
        }, cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<RuleFeatureExecutionDto>> IRuleExecutionsApi.GetExecution(
        Guid gameServerId, Guid executionId, CancellationToken cancellationToken)
    {
        var execution = await context.RuleFeatureExecutions.AsNoTracking().SingleOrDefaultAsync(
            e => e.GameServerId == gameServerId && e.ExecutionId == executionId, cancellationToken).ConfigureAwait(false);
        return execution is null
            ? new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.NotFound)
            : new ApiResponse<RuleFeatureExecutionDto>(await ToDtoAsync(execution, cancellationToken).ConfigureAwait(false)).ToApiResult();
    }

    async Task<ApiResult<RuleFeatureExecutionDto>> IRuleExecutionsApi.UpdateProgress(
        Guid gameServerId, Guid executionId, UpdateRuleFeatureExecutionProgressDto request, CancellationToken cancellationToken)
    {
        if (request is null || request.ExpectedVersion <= 0 || request.RuleIndex < 0 || request.ActionIndex < 0 ||
            !ValidExecutionState(request.State) || !ValidOptionalJson(request.DecisionSnapshotJson))
        {
            return new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.BadRequest);
        }

        return await ExecuteMutationAsync(async () =>
        {
            if (!await AcquireTransactionLockAsync($"rules-execution-id:{executionId:N}", cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.Conflict);
            }
            var execution = await context.RuleFeatureExecutions.SingleOrDefaultAsync(
                e => e.GameServerId == gameServerId && e.ExecutionId == executionId, cancellationToken).ConfigureAwait(false);
            if (execution is null)
            {
                return new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.NotFound);
            }
            if (execution.Version != request.ExpectedVersion)
            {
                return new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.Conflict);
            }
            if (execution.State is nameof(RuleFeatureExecutionState.Completed) or nameof(RuleFeatureExecutionState.Stopped) ||
                await context.RuleActionExecutions.AnyAsync(a => a.ExecutionId == executionId &&
                    (a.Outcome == RuleActionExecutionOutcome.Claimed.ToString() ||
                     a.Outcome == RuleActionExecutionOutcome.Dispatched.ToString()), cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.Conflict);
            }
            if (request.FactSnapshotId is { } factSnapshotId &&
                !await context.RuleFactSnapshots.AnyAsync(f => f.SnapshotId == factSnapshotId &&
                    f.GameServerId == gameServerId && f.ConnectionId == execution.ConnectionId, cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleFeatureExecutionDto>(HttpStatusCode.BadRequest);
            }
            execution.RuleIndex = request.RuleIndex;
            execution.ActionIndex = request.ActionIndex;
            execution.State = request.State.ToString();
            execution.DecisionSnapshotJson = request.DecisionSnapshotJson ?? execution.DecisionSnapshotJson;
            execution.FactSnapshotId = request.FactSnapshotId ?? execution.FactSnapshotId;
            execution.Version++;
            execution.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse<RuleFeatureExecutionDto>(await ToDtoAsync(execution, cancellationToken).ConfigureAwait(false)).ToApiResult();
        }, cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<RuleActionClaimResultDto>> IRuleExecutionsApi.ClaimAction(
        Guid gameServerId, Guid executionId, ClaimRuleActionDto request, CancellationToken cancellationToken)
    {
        if (!ValidClaimRequest(gameServerId, executionId, request))
        {
            return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.BadRequest);
        }

        return await ExecuteMutationAsync(async () =>
        {
            if (!await AcquireTransactionLockAsync($"rules-execution-id:{executionId:N}", cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
            }
            var execution = await context.RuleFeatureExecutions.SingleOrDefaultAsync(
                e => e.GameServerId == gameServerId && e.ExecutionId == executionId, cancellationToken).ConfigureAwait(false);
            if (execution is null)
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.NotFound);
            }
            if (!TryGetContextTrigger(execution, out var contextTrigger) ||
                !CorrelationMatchesExecution(request.Command.Correlation, execution, contextTrigger) ||
                !CommandMatchesTrigger(request.Command, contextTrigger))
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.BadRequest);
            }
            if (!TryResolveFrozenAction(execution, request, out var frozenAction) ||
                !NativeEffectMatchesAction(frozenAction.TypeId, request.Command.Effect.Type))
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.BadRequest);
            }
            if (!await AcquireTransactionLockAsync($"rules-effect:{request.LogicalEffectId}", cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
            }

            var existing = await context.RuleActionExecutions.SingleOrDefaultAsync(
                a => a.LogicalEffectId == request.LogicalEffectId, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                var existingCorrelation = Deserialize<RuleActionCorrelation>(existing.CorrelationJson);
                if (!SameLogicalAction(existing, existingCorrelation, execution, request.Command.Correlation))
                {
                    return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
                }
                return await ClaimResultAsync(existing, execution, shouldDispatch: false, cancellationToken).ConfigureAwait(false);
            }
            if (execution.Version != request.ExpectedVersion ||
                !Enum.TryParse<RuleFeatureExecutionState>(execution.State, out var state) ||
                state != RuleFeatureExecutionState.Evaluating ||
                execution.RuleIndex != request.RuleIndex || execution.ActionIndex != request.ActionIndex ||
                await context.RuleActionExecutions.AnyAsync(a => a.ExecutionId == executionId &&
                    (a.Outcome == RuleActionExecutionOutcome.Claimed.ToString() ||
                     a.Outcome == RuleActionExecutionOutcome.Dispatched.ToString()), cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
            }

            var now = DateTime.UtcNow;
            var command = request.Command;
            var action = new RuleActionExecution
            {
                ActionExecutionId = Guid.NewGuid(),
                ExecutionId = executionId,
                GameServerId = gameServerId,
                SiegeSessionId = execution.SiegeSessionId,
                LogicalEffectId = request.LogicalEffectId,
                ActionId = command.ActionId,
                ActionTypeId = frozenAction.TypeId,
                ParametersJson = frozenAction.ParametersJson,
                CorrelationJson = JsonSerializer.Serialize(command.Correlation, JsonOptions),
                CommandJson = JsonSerializer.Serialize(command, JsonOptions),
                Mode = command.Mode.ToString(),
                Outcome = RuleActionExecutionOutcome.Claimed.ToString(),
                RuleIndex = request.RuleIndex,
                ActionIndex = request.ActionIndex,
                ClaimedAtUtc = now,
                UpdatedAtUtc = now
            };
            context.RuleActionExecutions.Add(action);
            execution.RuleIndex = request.RuleIndex;
            execution.ActionIndex = request.ActionIndex;
            execution.State = RuleFeatureExecutionState.AwaitingActionOutcome.ToString();
            execution.Version++;
            execution.UpdatedAtUtc = now;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var shouldDispatch = command.Mode == RuleExecutionMode.Enforce &&
                command.ExpiresAtUtc > DateTimeOffset.UtcNow &&
                command.Effect.Type is RuleNativeEffectType.DropPlayer or RuleNativeEffectType.PublicMessage or RuleNativeEffectType.PrivateMessage;
            return await ClaimResultAsync(action, execution, shouldDispatch, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<EnsureAutomatedActionResultDto>> IRuleExecutionsApi.EnsureRuleActionAdminAction(
        Guid gameServerId, Guid executionId, string actionId, EnsureRuleActionAdminActionDto request, CancellationToken cancellationToken)
    {
        if (gameServerId == Guid.Empty || executionId == Guid.Empty ||
            string.IsNullOrWhiteSpace(actionId) || actionId.Length > 64 ||
            request is null || request.ExpectedVersion <= 0 ||
            !ValidLogicalEffectId(request.LogicalEffectId) ||
            string.IsNullOrWhiteSpace(request.Text) ||
            request.Type is not (AdminActionType.Observation or AdminActionType.Kick or AdminActionType.TempBan or AdminActionType.Ban) ||
            request.Type == AdminActionType.TempBan && request.Expires is null ||
            request.Type != AdminActionType.TempBan && request.Expires is not null)
        {
            return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.BadRequest);
        }

        return await ExecuteMutationAsync(async () =>
        {
            if (!await AcquireTransactionLockAsync($"rules-execution-id:{executionId:N}", cancellationToken).ConfigureAwait(false) ||
                !await AcquireTransactionLockAsync($"rules-effect:{request.LogicalEffectId}", cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.Conflict);
            }

            var execution = await context.RuleFeatureExecutions.SingleOrDefaultAsync(
                e => e.GameServerId == gameServerId && e.ExecutionId == executionId, cancellationToken).ConfigureAwait(false);
            if (execution is null)
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.NotFound);
            }

            var action = await context.RuleActionExecutions.SingleOrDefaultAsync(
                a => a.ExecutionId == executionId && a.ActionId == actionId &&
                    a.LogicalEffectId == request.LogicalEffectId, cancellationToken).ConfigureAwait(false);
            if (action is null)
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.NotFound);
            }

            var correlation = Deserialize<RuleActionCorrelation>(action.CorrelationJson);
            var command = Deserialize<RuleActionCommand>(action.CommandJson);
            if (!TryGetContextTrigger(execution, out var contextTrigger) ||
                correlation.ActionId != actionId ||
                !CorrelationMatchesExecution(correlation, execution, contextTrigger) ||
                command.ExecutionId != executionId || command.GameServerId != gameServerId ||
                command.ActionId != actionId || command.LogicalEffectId != request.LogicalEffectId ||
                command.Correlation != correlation || !CommandMatchesTrigger(command, contextTrigger) ||
                action.GameServerId != gameServerId)
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.Conflict);
            }

            var expectedFeature = correlation.FeatureId switch
            {
                "vpn-protection" => AutomationFeature.VpnProtection,
                "siege-mode" => AutomationFeature.SiegeMode,
                _ => (AutomationFeature?)null
            };
            if (expectedFeature is null || ExpectedAdminActionType(action.ActionTypeId) != request.Type ||
                command.Mode != RuleExecutionMode.Enforce ||
                request.Type == AdminActionType.Observation && command.Effect.Type != RuleNativeEffectType.Unknown ||
                request.Type != AdminActionType.Observation && command.Effect.Type != RuleNativeEffectType.DropPlayer)
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.BadRequest);
            }

            if (action.AdminActionId is { } existingAdminActionId)
            {
                var existingAdminAction = await context.AdminActions.SingleOrDefaultAsync(
                    a => a.AdminActionId == existingAdminActionId, cancellationToken).ConfigureAwait(false);
                if (existingAdminAction is null ||
                    existingAdminAction.Source != (byte)ActionSource.Automation ||
                    existingAdminAction.AutomationFeature != (int)expectedFeature.Value ||
                    existingAdminAction.AutomationRuleId != correlation.RuleId ||
                    existingAdminAction.Type != request.Type.ToAdminActionTypeInt())
                {
                    return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.Conflict);
                }

                return new ApiResponse<EnsureAutomatedActionResultDto>(new EnsureAutomatedActionResultDto
                {
                    Created = false,
                    AdminAction = existingAdminAction.ToDto(expand: false)
                }).ToApiResult();
            }

            if (execution.Version != request.ExpectedVersion ||
                execution.State != RuleFeatureExecutionState.AwaitingActionOutcome.ToString() ||
                action.Outcome != RuleActionExecutionOutcome.Claimed.ToString() ||
                command.ExpiresAtUtc == default || command.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.Conflict);
            }

            if (request.Type == AdminActionType.TempBan && request.Expires <= DateTime.UtcNow)
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.BadRequest);
            }

            var playerGuid = NormalizeGuid(command.PlayerGuid);
            var trigger = Deserialize<RuleTriggerEnvelope>(execution.TriggerJson);
            var serverGameType = await context.GameServers.AsNoTracking()
                .Where(server => server.GameServerId == gameServerId && !server.Deleted)
                .Select(server => (int?)server.GameType)
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (serverGameType is null)
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.NotFound);
            }
            if (serverGameType.Value != (int)trigger.GameType)
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.Conflict);
            }

            var player = playerGuid is null
                ? null
                : await context.Players.SingleOrDefaultAsync(
                    p => p.GameType == serverGameType.Value &&
                        p.Guid != null && p.Guid.ToLower() == playerGuid.ToLower(),
                    cancellationToken).ConfigureAwait(false);
            if (player is null)
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.NotFound);
            }

            var adminAction = new AdminAction
            {
                AdminActionId = Guid.NewGuid(),
                PlayerId = player.PlayerId,
                Type = request.Type.ToAdminActionTypeInt(),
                Text = request.Text,
                Created = DateTime.UtcNow,
                Expires = request.Expires,
                Source = (byte)ActionSource.Automation,
                AutomationFeature = (int)expectedFeature.Value,
                AutomationRuleId = correlation.RuleId,
                AutomationIsActive = request.Type is AdminActionType.TempBan or AdminActionType.Ban
            };
            if (!string.IsNullOrWhiteSpace(request.AdminId))
            {
                adminAction.UserProfileId = await context.UserProfiles
                    .Where(profile => profile.IdentityOid == request.AdminId || profile.XtremeIdiotsForumId == request.AdminId)
                    .Select(profile => (Guid?)profile.UserProfileId)
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            }

            action.AdminActionId = adminAction.AdminActionId;
            action.UpdatedAtUtc = DateTime.UtcNow;
            context.AdminActions.Add(adminAction);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse<EnsureAutomatedActionResultDto>(new EnsureAutomatedActionResultDto
            {
                Created = true,
                AdminAction = adminAction.ToDto(expand: false)
            }).ToApiResult(HttpStatusCode.Created);
        }, cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<RuleActionClaimResultDto>> IRuleExecutionsApi.UpdateActionOutcome(
        Guid gameServerId, Guid executionId, string actionId, UpdateRuleActionOutcomeDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actionId) || actionId.Length > 64 || request?.Result is null ||
            request.ExpectedVersion <= 0 || !ValidOutcome(request.Result.Outcome) ||
            request.Result.Detail is { Length: > 1024 } ||
            request.AdminActionId == Guid.Empty ||
            request.NativeEvidence?.Detail is { Length: > 1024 } ||
            !ValidExecutionState(request.NextState) || request.NextRuleIndex < 0 || request.NextActionIndex < 0 ||
            !ValidOptionalJson(request.DecisionSnapshotJson))
        {
            return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.BadRequest);
        }

        return await ExecuteMutationAsync(async () =>
        {
            if (!await AcquireTransactionLockAsync($"rules-execution-id:{executionId:N}", cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
            }
            if (request.AdminActionId is { } requestedAdminActionId &&
                !await AcquireTransactionLockAsync($"rules-admin-action:{requestedAdminActionId:N}", cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
            }
            var execution = await context.RuleFeatureExecutions.SingleOrDefaultAsync(
                e => e.GameServerId == gameServerId && e.ExecutionId == executionId, cancellationToken).ConfigureAwait(false);
            if (execution is null)
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.NotFound);
            }
            var action = await context.RuleActionExecutions.SingleOrDefaultAsync(
                a => a.ExecutionId == executionId && a.ActionId == actionId &&
                    a.LogicalEffectId == request.Result.LogicalEffectId, cancellationToken).ConfigureAwait(false);
            if (action is null)
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.NotFound);
            }
            if (!ValidOutcomeEcho(gameServerId, executionId, actionId, action, request.Result))
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.BadRequest);
            }
            if (!OutcomeAllowedForAction(action, request.Result.Outcome, request.AdminActionId))
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.BadRequest);
            }
            if (request.NativeEvidence is { } nativeEvidence &&
                !ValidNativeEvidence(gameServerId, executionId, actionId, action, nativeEvidence))
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.BadRequest);
            }
            if (action.AdminActionId is { } linkedAdminActionId &&
                request.AdminActionId is { } requestedLinkId && linkedAdminActionId != requestedLinkId)
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
            }
            if (request.AdminActionId is { } adminActionId)
            {
                var existingLink = await context.RuleActionExecutions.AnyAsync(
                    a => a.AdminActionId == adminActionId && a.ActionExecutionId != action.ActionExecutionId,
                    cancellationToken).ConfigureAwait(false);
                if (existingLink)
                {
                    return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
                }

                var adminAction = await context.AdminActions.Include(a => a.Player)
                    .SingleOrDefaultAsync(a => a.AdminActionId == adminActionId, cancellationToken).ConfigureAwait(false);
                if (adminAction is null)
                {
                    return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.NotFound);
                }
                var serverGameType = await context.GameServers.AsNoTracking()
                    .Where(s => s.GameServerId == gameServerId && !s.Deleted)
                    .Select(s => (int?)s.GameType)
                    .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                var command = Deserialize<RuleActionCommand>(action.CommandJson);
                var correlation = Deserialize<RuleActionCorrelation>(action.CorrelationJson);
                var expectedAutomationFeature = correlation.FeatureId switch
                {
                    "vpn-protection" => AutomationFeature.VpnProtection,
                    "siege-mode" => AutomationFeature.SiegeMode,
                    _ => (AutomationFeature?)null
                };
                var isModerationRecordAction = adminAction.Type is (int)AdminActionType.Observation or
                    (int)AdminActionType.Kick or (int)AdminActionType.TempBan or (int)AdminActionType.Ban;
                if (!isModerationRecordAction || expectedAutomationFeature is null ||
                    adminAction.Source != (byte)ActionSource.Automation ||
                    adminAction.AutomationFeature != (int)expectedAutomationFeature.Value ||
                    adminAction.AutomationRuleId != correlation.RuleId ||
                    serverGameType != adminAction.Player.GameType ||
                    !string.Equals(adminAction.Player.Guid, command.PlayerGuid, StringComparison.OrdinalIgnoreCase))
                {
                    return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.BadRequest);
                }
            }

            var resultJson = JsonSerializer.Serialize(request.Result, JsonOptions);
            var nativeEvidenceJson = request.NativeEvidence is null
                ? null
                : JsonSerializer.Serialize(request.NativeEvidence, JsonOptions);
            if (action.NativeEvidenceJson is not null && nativeEvidenceJson is not null &&
                action.NativeEvidenceJson != nativeEvidenceJson)
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
            }
            if (action.Outcome is not (nameof(RuleActionExecutionOutcome.Claimed) or
                nameof(RuleActionExecutionOutcome.Dispatched)))
            {
                var hasProgressUpdate =
                    (nativeEvidenceJson is not null && action.NativeEvidenceJson is null) ||
                    (request.AdminActionId is not null && action.AdminActionId is null);
                if (hasProgressUpdate)
                {
                    if (action.ResultJson != resultJson || request.ExpectedVersion > execution.Version)
                    {
                        return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
                    }
                    action.NativeEvidenceJson ??= nativeEvidenceJson;
                    action.AdminActionId ??= request.AdminActionId;
                    action.UpdatedAtUtc = DateTime.UtcNow;
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return await ClaimResultAsync(action, execution, shouldDispatch: false, cancellationToken).ConfigureAwait(false);
                }

                if (action.ResultJson == resultJson)
                {
                    return await ClaimResultAsync(action, execution, shouldDispatch: false, cancellationToken).ConfigureAwait(false);
                }

                var hasAdminAction = action.AdminActionId is not null || request.AdminActionId is not null;
                if (action.Outcome == RuleActionExecutionOutcome.OutcomeUnknown.ToString() &&
                    request.Result.Outcome == RuleActionExecutionOutcome.Applied &&
                    request.NativeEvidence is null && request.ExpectedVersion <= execution.Version &&
                    (hasAdminAction
                        ? request.AdminActionId == action.AdminActionId &&
                            IsHostAggregateConfirmation(request.Result)
                        : request.AdminActionId is null && IsLateAppliedEvidence(action, request.Result)))
                {
                    action.Outcome = RuleActionExecutionOutcome.Applied.ToString();
                    action.OutcomeAtUtc = request.Result.OccurredAtUtc.UtcDateTime;
                    action.ResultJson = resultJson;
                    action.Detail = request.Result.Detail;
                    action.UpdatedAtUtc = DateTime.UtcNow;
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return await ClaimResultAsync(action, execution, shouldDispatch: false, cancellationToken).ConfigureAwait(false);
                }

                if (action.Outcome != RuleActionExecutionOutcome.Dispatched.ToString() ||
                    !CanTransitionFromDispatched(request.Result.Outcome) ||
                    request.Result.Outcome == RuleActionExecutionOutcome.Applied &&
                    request.NativeEvidence is not null)
                {
                    return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
                }
                if (request.Result.Outcome == RuleActionExecutionOutcome.Applied &&
                    (hasAdminAction
                        ? request.AdminActionId != action.AdminActionId ||
                            !IsHostAggregateConfirmation(request.Result)
                        : !IsAppliedWithinCommandWindow(action, request.Result)))
                {
                    return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
                }
            }
            if (execution.Version != request.ExpectedVersion ||
                execution.State != RuleFeatureExecutionState.AwaitingActionOutcome.ToString() ||
                request.NextRuleIndex < action.RuleIndex ||
                (request.NextRuleIndex == action.RuleIndex && request.NextActionIndex < action.ActionIndex))
            {
                return new ApiResult<RuleActionClaimResultDto>(HttpStatusCode.Conflict);
            }

            var outcomeAt = request.Result.OccurredAtUtc.UtcDateTime;
            action.AdminActionId ??= request.AdminActionId;
            action.NativeEvidenceJson ??= nativeEvidenceJson;
            action.Outcome = request.Result.Outcome.ToString();
            action.OutcomeAtUtc = outcomeAt;
            action.ResultJson = resultJson;
            action.Detail = request.Result.Detail;
            action.UpdatedAtUtc = DateTime.UtcNow;
            execution.RuleIndex = request.NextRuleIndex;
            execution.ActionIndex = request.NextActionIndex;
            execution.State = request.NextState.ToString();
            execution.DecisionSnapshotJson = request.DecisionSnapshotJson ?? execution.DecisionSnapshotJson;
            execution.Version++;
            execution.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return await ClaimResultAsync(action, execution, shouldDispatch: false, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<RuleFactSnapshotDto>> IRuleExecutionsApi.CreateFactSnapshot(
        Guid gameServerId, RuleFactSnapshotRequestDto request, CancellationToken cancellationToken)
    {
        if (!ValidFactRequest(gameServerId, request))
        {
            return new ApiResult<RuleFactSnapshotDto>(HttpStatusCode.BadRequest);
        }

        return await ExecuteMutationAsync(async () =>
        {
            var server = await context.GameServers.AsNoTracking().SingleOrDefaultAsync(
                s => s.GameServerId == gameServerId && !s.Deleted, cancellationToken).ConfigureAwait(false);
            if (server is null)
            {
                return new ApiResult<RuleFactSnapshotDto>(HttpStatusCode.NotFound);
            }
            if (server.GameType != (int)request.GameType)
            {
                return new ApiResult<RuleFactSnapshotDto>(HttpStatusCode.Conflict);
            }
            if (!await AcquireTransactionLockAsync($"rules-facts:{gameServerId:N}:{request.ConnectionId:N}", cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleFactSnapshotDto>(HttpStatusCode.Conflict);
            }
            var observedAt = request.ObservedAtUtc.UtcDateTime;
            var historyCutoff = observedAt > DateTime.MinValue.Add(LegacyObservationPrecisionMargin)
                ? observedAt.Subtract(LegacyObservationPrecisionMargin)
                : DateTime.MinValue;
            var existing = await context.RuleFactSnapshots.SingleOrDefaultAsync(
                f => f.GameServerId == gameServerId && f.ConnectionId == request.ConnectionId, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                if (existing.PlayerIpAddress is null && NormalizeIp(request.PlayerIpAddress) is { } newIp)
                {
                    existing.PlayerIpAddress = newIp;
                    existing.IpFirstSeenAtUtc = await FindIpFirstSeenAsync(newIp, historyCutoff, cancellationToken).ConfigureAwait(false);
                    existing.IpHistoryAvailable = true;
                    existing.IpObservationId = request.ObservationId;
                    existing.IpEnrichedAtUtc = DateTime.UtcNow;
                    if (existing.BaselineSessionId is { } baselineSessionId)
                    {
                        existing.IpBaselineFirstSeenAtUtc = await context.SiegeBaselineIps.AsNoTracking()
                            .Where(a => a.SessionId == baselineSessionId && a.Address == newIp)
                            .Select(a => (DateTime?)a.FirstSeenAtUtc)
                            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return new ApiResponse<RuleFactSnapshotDto>(ToDto(existing)).ToApiResult();
            }

            var guid = NormalizeGuid(request.PlayerGuid);
            var ip = NormalizeIp(request.PlayerIpAddress);
            var matchingPlayers = guid is null
                ? new List<PlayerFact>()
                : await context.Players.AsNoTracking()
                    .Where(p => p.GameType == (int)request.GameType && p.Guid != null &&
                        p.Guid.ToLower() == guid.ToLower() && p.FirstSeen < historyCutoff)
                    .Select(p => new PlayerFact(p.PlayerId, p.FirstSeen))
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
            DateTime? guidFirstSeen = matchingPlayers.Count == 0 ? null : matchingPlayers.Min(p => p.FirstSeen);
            var ipFirstSeen = ip is null ? null : await FindIpFirstSeenAsync(ip, historyCutoff, cancellationToken).ConfigureAwait(false);

            var baselineSession = await FindBaselineSessionAsync(gameServerId, request, observedAt, cancellationToken).ConfigureAwait(false);
            if (request.SiegeSessionId is not null && baselineSession is null)
            {
                return new ApiResult<RuleFactSnapshotDto>(HttpStatusCode.NotFound);
            }
            DateTime? guidBaselineFirstSeen = null;
            DateTime? ipBaselineFirstSeen = null;
            if (baselineSession is not null && guid is not null)
            {
                guidBaselineFirstSeen = await context.SiegeBaselineGuids.AsNoTracking()
                    .Where(g => g.SessionId == baselineSession.SessionId && g.PlayerGuid.ToLower() == guid.ToLower())
                    .Select(g => (DateTime?)g.FirstSeenAtUtc).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            }
            if (baselineSession is not null && ip is not null)
            {
                ipBaselineFirstSeen = await context.SiegeBaselineIps.AsNoTracking()
                    .Where(a => a.SessionId == baselineSession.SessionId && a.Address == ip)
                    .Select(a => (DateTime?)a.FirstSeenAtUtc).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            }

            DateTime? profileFirstSeen = null;
            Guid[] tagIds = [];
            if (matchingPlayers.Count > 0)
            {
                var playerIds = matchingPlayers.Select(p => p.PlayerId).ToArray();
                profileFirstSeen = await context.ConnectedPlayerProfiles.AsNoTracking()
                    .Where(p => playerIds.Contains(p.PlayerId) && p.LinkedAtUtc < observedAt)
                    .Select(p => (DateTime?)p.LinkedAtUtc)
                    .MinAsync(cancellationToken).ConfigureAwait(false);
                tagIds = await context.PlayerTags.AsNoTracking()
                    .Where(t => t.PlayerId.HasValue && playerIds.Contains(t.PlayerId.Value) &&
                        t.TagId.HasValue && t.Assigned < historyCutoff)
                    .Select(t => t.TagId!.Value).Distinct().ToArrayAsync(cancellationToken).ConfigureAwait(false);
            }

            var factSnapshot = new RuleFactSnapshot
            {
                SnapshotId = Guid.NewGuid(),
                ObservationId = request.ObservationId,
                GameServerId = gameServerId,
                ConnectionId = request.ConnectionId,
                GameType = (int)request.GameType,
                PlayerGuid = guid,
                PlayerIpAddress = ip,
                CapturedAtUtc = DateTime.UtcNow,
                GuidFirstSeenAtUtc = guidFirstSeen,
                IpFirstSeenAtUtc = ipFirstSeen,
                ProfileFirstSeenAtUtc = profileFirstSeen,
                GuidBaselineFirstSeenAtUtc = guidBaselineFirstSeen,
                IpBaselineFirstSeenAtUtc = ipBaselineFirstSeen,
                BaselineSessionId = baselineSession?.SessionId,
                TagIdsJson = JsonSerializer.Serialize(tagIds, JsonOptions),
                GuidHistoryAvailable = guid is not null,
                IpHistoryAvailable = ip is not null,
                ProfileHistoryAvailable = guid is not null,
                TagsAvailable = guid is not null,
                IpObservationId = ip is null ? null : request.ObservationId,
                IpEnrichedAtUtc = ip is null ? null : DateTime.UtcNow
            };
            context.RuleFactSnapshots.Add(factSnapshot);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse<RuleFactSnapshotDto>(ToDto(factSnapshot)).ToApiResult();
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DateTime?> FindIpFirstSeenAsync(string address, DateTime beforeUtc, CancellationToken cancellationToken)
    {
        var alternate = AlternateIp(address);
        return await context.PlayerIpAddresses.AsNoTracking()
            .Where(ip => ip.Address != null && (ip.Address == address || ip.Address == alternate) && ip.Added < beforeUtc)
            .Select(ip => (DateTime?)ip.Added)
            .MinAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<SiegeSession?> FindBaselineSessionAsync(
        Guid gameServerId, RuleFactSnapshotRequestDto request, DateTime observedAt, CancellationToken cancellationToken)
    {
        var query = context.SiegeSessions.AsNoTracking().Where(s => s.GameServerId == gameServerId &&
            s.GameType == (int)request.GameType && s.StartedAtUtc <= observedAt && s.ExpiresAtUtc > observedAt &&
            (s.EndedAtUtc == null || s.EndedAtUtc > observedAt));
        if (request.SiegeSessionId is { } sessionId)
        {
            query = query.Where(s => s.SessionId == sessionId);
        }
        return await query.OrderByDescending(s => s.StartedAtUtc).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<RuleFeatureExecutionDto> ToDtoAsync(RuleFeatureExecution execution, CancellationToken cancellationToken)
    {
        var actions = await context.RuleActionExecutions.AsNoTracking()
            .Where(a => a.ExecutionId == execution.ExecutionId)
            .OrderBy(a => a.RuleIndex).ThenBy(a => a.ActionIndex)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new RuleFeatureExecutionDto
        {
            ExecutionId = execution.ExecutionId,
            GameServerId = execution.GameServerId,
            ConnectionId = execution.ConnectionId,
            FeatureId = execution.FeatureId,
            SiegeSessionId = execution.SiegeSessionId,
            RuleSetRevision = execution.RuleSetRevision,
            FrozenRulesJson = execution.FrozenRulesJson,
            Trigger = Deserialize<RuleTriggerEnvelope>(execution.TriggerJson),
            FactSnapshotId = execution.FactSnapshotId,
            State = Enum.Parse<RuleFeatureExecutionState>(execution.State),
            RuleIndex = execution.RuleIndex,
            ActionIndex = execution.ActionIndex,
            DecisionSnapshotJson = execution.DecisionSnapshotJson,
            Actions = actions.Select(action => ToActionDto(action, execution)).ToArray(),
            Version = execution.Version,
            CreatedAtUtc = Utc(execution.CreatedAtUtc),
            UpdatedAtUtc = Utc(execution.UpdatedAtUtc)
        };
    }

    private async Task<ApiResult<RuleActionClaimResultDto>> ClaimResultAsync(
        RuleActionExecution action, RuleFeatureExecution execution, bool shouldDispatch, CancellationToken cancellationToken) =>
        new ApiResponse<RuleActionClaimResultDto>(new RuleActionClaimResultDto
        {
            Action = ToActionDto(action, execution),
            Execution = await ToDtoAsync(execution, cancellationToken).ConfigureAwait(false),
            ShouldDispatch = shouldDispatch
        }).ToApiResult();

    private static RuleActionExecutionDto ToActionDto(RuleActionExecution action, RuleFeatureExecution? execution = null)
    {
        var correlation = Deserialize<RuleActionCorrelation>(action.CorrelationJson);
        var command = Deserialize<RuleActionCommand>(action.CommandJson);
        var result = action.ResultJson is { } resultJson ? Deserialize<RuleActionResult>(resultJson) : null;
        var nativeEvidence = action.NativeEvidenceJson is { } nativeEvidenceJson
            ? Deserialize<RuleActionResult>(nativeEvidenceJson)
            : null;
        return new RuleActionExecutionDto
        {
            ExecutionId = action.ExecutionId,
            FeatureId = execution?.FeatureId ?? correlation.FeatureId,
            RuleId = correlation.RuleId,
            ActionId = action.ActionId,
            RuleIndex = action.RuleIndex,
            ActionIndex = action.ActionIndex,
            SiegeSessionId = action.SiegeSessionId,
            LogicalEffectId = action.LogicalEffectId.Trim(),
            Correlation = correlation,
            Command = command,
            GameServerId = action.GameServerId,
            ConnectionId = command.ConnectionId,
            ConnectionGeneration = command.ConnectionGeneration,
            PlayerGuid = command.PlayerGuid,
            SlotId = command.SlotId,
            ActionTypeId = action.ActionTypeId,
            ParametersJson = action.ParametersJson,
            Mode = Enum.Parse<RuleExecutionMode>(action.Mode),
            Outcome = Enum.Parse<RuleActionExecutionOutcome>(action.Outcome),
            ClaimedAtUtc = Utc(action.ClaimedAtUtc),
            OccurredAtUtc = result?.OccurredAtUtc ?? nativeEvidence?.OccurredAtUtc,
            OutcomeAtUtc = action.OutcomeAtUtc is { } outcomeAt ? Utc(outcomeAt) : null,
            UpdatedAtUtc = Utc(action.UpdatedAtUtc),
            ResultJson = action.ResultJson,
            Detail = action.Detail,
            AdminActionId = action.AdminActionId,
            NativeEvidence = nativeEvidence
        };
    }

    private IQueryable<RuleActionExecution> ApplyRuleActionAuditFilters(
        IQueryable<RuleActionExecution> query, RuleActionAuditQueryDto request)
    {
        if (request.SiegeSessionId is { } siegeSessionId)
        {
            query = query.Where(action => action.SiegeSessionId == siegeSessionId);
        }
        if (!string.IsNullOrWhiteSpace(request.FeatureId))
        {
            var executionIds = context.RuleFeatureExecutions.AsNoTracking()
                .Where(execution => execution.FeatureId == request.FeatureId)
                .Select(execution => execution.ExecutionId);
            query = query.Where(action => executionIds.Contains(action.ExecutionId));
        }
        if (request.FromUtc is { } from)
        {
            query = query.Where(action => action.ClaimedAtUtc >= from);
        }
        if (request.ToUtc is { } to)
        {
            query = query.Where(action => action.ClaimedAtUtc < to);
        }
        return query;
    }

    private static bool ValidRuleActionAuditQuery(RuleActionAuditQueryDto? request) =>
        request is not null &&
        request.GameServerIds is { Length: > 0 and <= 1000 } &&
        request.GameServerIds.All(id => id != Guid.Empty) &&
        (request.SiegeSessionId is null || request.SiegeSessionId != Guid.Empty) &&
        (request.FeatureId is null || !string.IsNullOrWhiteSpace(request.FeatureId) && request.FeatureId.Length <= 64) &&
        request.SkipEntries >= 0 &&
        request.TakeEntries is > 0 and <= 100 &&
        (request.FromUtc is null || request.ToUtc is null || request.FromUtc < request.ToUtc);

    private static RuleFactSnapshotDto ToDto(RuleFactSnapshot snapshot) => new()
    {
        SnapshotId = snapshot.SnapshotId,
        GameServerId = snapshot.GameServerId,
        ConnectionId = snapshot.ConnectionId,
        GameType = (GameType)snapshot.GameType,
        PlayerGuid = snapshot.PlayerGuid,
        PlayerIpAddress = snapshot.PlayerIpAddress,
        CapturedAtUtc = Utc(snapshot.CapturedAtUtc),
        GuidFirstSeenAtUtc = snapshot.GuidFirstSeenAtUtc is { } guidAt ? Utc(guidAt) : null,
        IpFirstSeenAtUtc = snapshot.IpFirstSeenAtUtc is { } ipAt ? Utc(ipAt) : null,
        ProfileFirstSeenAtUtc = snapshot.ProfileFirstSeenAtUtc is { } profileAt ? Utc(profileAt) : null,
        GuidBaselineFirstSeenAtUtc = snapshot.GuidBaselineFirstSeenAtUtc is { } guidBaselineAt ? Utc(guidBaselineAt) : null,
        IpBaselineFirstSeenAtUtc = snapshot.IpBaselineFirstSeenAtUtc is { } ipBaselineAt ? Utc(ipBaselineAt) : null,
        SiegeSessionId = snapshot.BaselineSessionId,
        TagIds = Deserialize<Guid[]>(snapshot.TagIdsJson),
        GuidHistoryAvailable = snapshot.GuidHistoryAvailable,
        IpHistoryAvailable = snapshot.IpHistoryAvailable,
        ProfileHistoryAvailable = snapshot.ProfileHistoryAvailable,
        TagsAvailable = snapshot.TagsAvailable,
        GuidBaselineAvailable = snapshot.BaselineSessionId.HasValue,
        IpBaselineAvailable = snapshot.BaselineSessionId.HasValue
    };

    private async Task<T> ExecuteMutationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            if (!context.Database.IsRelational())
            {
                return await operation().ConfigureAwait(false);
            }
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
            var result = await operation().ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }).ConfigureAwait(false);
    }

    private async Task<bool> AcquireTransactionLockAsync(string resource, CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer())
        {
            return true;
        }
        var results = await context.Database.SqlQuery<int>($"""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = 10000;
            SELECT @result AS [Value];
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.Single() >= 0;
    }

    private static bool ValidStartRequest(Guid gameServerId, StartRuleFeatureExecutionDto? request) =>
        gameServerId != Guid.Empty && request is not null &&
        request.Trigger is not null && request.Trigger.GameServerId == gameServerId &&
        request.Trigger.ConnectionId != Guid.Empty && request.Trigger.TriggerOccurrenceId != Guid.Empty &&
        request.Trigger.ObservationId != Guid.Empty && request.Trigger.ProtocolVersion == 1 &&
        Enum.IsDefined(request.Trigger.GameType) && request.Trigger.OccurredAtUtc != default &&
        request.Trigger.TriggerId is "player.connected" or "player.ip-resolved" &&
        ValidTriggerRuntime(request.Trigger) &&
        !string.IsNullOrWhiteSpace(request.FeatureId) && request.FeatureId.Length <= 64 &&
        !string.IsNullOrWhiteSpace(request.RuleSetRevision) && request.RuleSetRevision.Length <= 128 &&
        ValidJsonObject(request.FrozenRulesJson);

    private static bool SameConnectionIdentity(RuleTriggerEnvelope original, RuleTriggerEnvelope incoming) =>
        original.GameServerId == incoming.GameServerId &&
        original.GameType == incoming.GameType &&
        original.ConnectionId == incoming.ConnectionId &&
        original.ConnectionGeneration == incoming.ConnectionGeneration &&
        string.Equals(original.PlayerGuid, incoming.PlayerGuid, StringComparison.Ordinal) &&
        original.SlotId == incoming.SlotId &&
        SameRuntimeIdentity(original.Runtime, incoming.Runtime);

    private static bool SameRuntimeIdentity(RuleRuntimeDescriptor? original, RuleRuntimeDescriptor? incoming) =>
        original is null || incoming is null
            ? original is null && incoming is null
            : original.Id == incoming.Id && original.Version == incoming.Version &&
                original.ProtocolVersion == incoming.ProtocolVersion &&
                original.IdentityModel == incoming.IdentityModel && original.InstanceId == incoming.InstanceId &&
                original.StartedAtUtc == incoming.StartedAtUtc;

    private static bool SameRuntimeDescriptor(RuleRuntimeDescriptor? original, RuleRuntimeDescriptor? incoming) =>
        SameRuntimeIdentity(original, incoming) &&
        (original?.Capabilities ?? []).SequenceEqual(incoming?.Capabilities ?? [], StringComparer.Ordinal);

    private static bool ValidTriggerRuntime(RuleTriggerEnvelope trigger)
    {
        var runtime = trigger.Runtime;
        return runtime is not null && !string.IsNullOrWhiteSpace(runtime.Id) &&
            !string.IsNullOrWhiteSpace(runtime.Version) && runtime.ProtocolVersion == 1 &&
            runtime.InstanceId != Guid.Empty && runtime.StartedAtUtc != default &&
            (runtime.IdentityModel == RuleRuntimeIdentityModel.PluginRuntimeGeneration
                ? trigger.ConnectionGeneration is > 0
                : runtime.IdentityModel == RuleRuntimeIdentityModel.AgentObservedConnection &&
                    trigger.ConnectionGeneration is null);
    }

    private static bool CommandMatchesTrigger(RuleActionCommand command, RuleTriggerEnvelope trigger) =>
        command.ConnectionId == trigger.ConnectionId &&
        command.ConnectionGeneration == trigger.ConnectionGeneration &&
        command.PlayerGuid == trigger.PlayerGuid &&
        command.SlotId == (trigger.SlotId ?? 0) &&
        SameRuntimeDescriptor(command.Runtime, trigger.Runtime);

    private static bool ValidClaimRequest(Guid gameServerId, Guid executionId, ClaimRuleActionDto? request)
    {
        if (request is null || request.ExpectedVersion <= 0 || request.RuleIndex < 0 || request.ActionIndex < 0 ||
            !ValidLogicalEffectId(request.LogicalEffectId) || request.Command is null)
        {
            return false;
        }
        var command = request.Command;
        var correlation = command.Correlation;
        return command.ProtocolVersion == 1 && command.ExecutionId == executionId &&
            command.GameServerId == gameServerId && command.ConnectionId != Guid.Empty &&
            command.Mode is RuleExecutionMode.Enforce or RuleExecutionMode.DryRun &&
            command.Runtime is not null && !string.IsNullOrWhiteSpace(command.Runtime.Id) &&
            !string.IsNullOrWhiteSpace(command.Runtime.Version) && command.Runtime.ProtocolVersion == 1 &&
            command.Runtime.InstanceId != Guid.Empty && command.Runtime.StartedAtUtc != default &&
            command.Runtime.IdentityModel is RuleRuntimeIdentityModel.PluginRuntimeGeneration or RuleRuntimeIdentityModel.AgentObservedConnection &&
            (command.Runtime.IdentityModel == RuleRuntimeIdentityModel.PluginRuntimeGeneration
                ? command.ConnectionGeneration.HasValue
                : command.ConnectionGeneration is null) &&
            command.Effect is not null && command.Effect.Type is RuleNativeEffectType.Unknown or
                RuleNativeEffectType.DropPlayer or RuleNativeEffectType.PublicMessage or RuleNativeEffectType.PrivateMessage &&
            command.LogicalEffectId == request.LogicalEffectId &&
            !string.IsNullOrWhiteSpace(command.ActionId) && command.ActionId.Length <= 64 &&
            correlation is not null && correlation.ConnectionId == command.ConnectionId &&
            correlation.ActionId == command.ActionId && !string.IsNullOrWhiteSpace(correlation.FeatureId) &&
            correlation.FeatureId.Length <= 64 && !string.IsNullOrWhiteSpace(correlation.RuleId) &&
            correlation.RuleId.Length <= 64;
    }

    private static bool SameLogicalAction(
        RuleActionExecution existing, RuleActionCorrelation originalCorrelation,
        RuleFeatureExecution execution, RuleActionCorrelation incoming) =>
        existing.GameServerId == execution.GameServerId &&
        originalCorrelation.ConnectionId == execution.ConnectionId &&
        originalCorrelation.ConnectionId == incoming.ConnectionId &&
        originalCorrelation.FeatureId == incoming.FeatureId &&
        originalCorrelation.RuleId == incoming.RuleId &&
        originalCorrelation.ActionId == incoming.ActionId &&
        originalCorrelation.SiegeSessionId == incoming.SiegeSessionId;

    private static bool CorrelationMatchesExecution(RuleActionCorrelation correlation, RuleFeatureExecution execution) =>
        TryGetContextTrigger(execution, out var trigger) &&
        CorrelationMatchesExecution(correlation, execution, trigger);

    private static bool CorrelationMatchesExecution(
        RuleActionCorrelation correlation, RuleFeatureExecution execution, RuleTriggerEnvelope trigger)
    {
        return correlation.ConnectionId == execution.ConnectionId &&
            correlation.FeatureId == execution.FeatureId &&
            correlation.RuleSetRevision == execution.RuleSetRevision &&
            correlation.SiegeSessionId == execution.SiegeSessionId &&
            correlation.TriggerId == trigger.TriggerId &&
            correlation.TriggerOccurrenceId == trigger.TriggerOccurrenceId;
    }

    private static bool TryGetContextTrigger(RuleFeatureExecution execution, out RuleTriggerEnvelope trigger)
    {
        var original = Deserialize<RuleTriggerEnvelope>(execution.TriggerJson);
        trigger = original;
        if (execution.DecisionSnapshotJson is null)
        {
            return true;
        }

        using var snapshot = JsonDocument.Parse(execution.DecisionSnapshotJson);
        if (snapshot.RootElement.ValueKind != JsonValueKind.Object ||
            !snapshot.RootElement.TryGetProperty("contextTrigger", out var contextElement))
        {
            return true;
        }
        if (contextElement.ValueKind != JsonValueKind.Object ||
            !snapshot.RootElement.TryGetProperty("contextObservedAtUtc", out var observedAtElement) ||
            observedAtElement.ValueKind != JsonValueKind.String ||
            !observedAtElement.TryGetDateTimeOffset(out var contextObservedAtUtc) ||
            contextObservedAtUtc == default)
        {
            return false;
        }

        RuleTriggerEnvelope? context;
        try
        {
            context = JsonSerializer.Deserialize<RuleTriggerEnvelope>(contextElement.GetRawText(), JsonOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        if (context is null || context.ProtocolVersion != 1 ||
            context.TriggerId is not ("player.connected" or "player.ip-resolved") ||
            context.ObservationId == Guid.Empty || context.TriggerOccurrenceId == Guid.Empty ||
            context.OccurredAtUtc < original.OccurredAtUtc ||
            context.OccurredAtUtc > DateTimeOffset.UtcNow.Add(NativeTimestampSkewAllowance) ||
            context.OccurredAtUtc.Offset != TimeSpan.Zero ||
            context.GameServerId != original.GameServerId || context.GameType != original.GameType ||
            context.ConnectionId != original.ConnectionId ||
            !string.Equals(context.PlayerGuid, original.PlayerGuid, StringComparison.Ordinal) ||
            context.SlotId != original.SlotId ||
            context.ConnectionGeneration != original.ConnectionGeneration ||
            context.Runtime is null || context.Runtime.ProtocolVersion != 1 ||
            context.Runtime.IdentityModel is not (RuleRuntimeIdentityModel.PluginRuntimeGeneration or
                RuleRuntimeIdentityModel.AgentObservedConnection) ||
            context.Runtime.InstanceId == Guid.Empty || context.Runtime.StartedAtUtc == default ||
            string.IsNullOrWhiteSpace(context.Runtime.Id) || string.IsNullOrWhiteSpace(context.Runtime.Version) ||
            !SameRuntimeIdentity(context.Runtime, original.Runtime))
        {
            return false;
        }

        if ((context.Runtime.IdentityModel == RuleRuntimeIdentityModel.PluginRuntimeGeneration &&
                context.ConnectionGeneration is not > 0) ||
            (context.Runtime.IdentityModel == RuleRuntimeIdentityModel.AgentObservedConnection &&
                context.ConnectionGeneration is not null))
        {
            return false;
        }

        trigger = context;
        return true;
    }

    private static bool TryResolveFrozenAction(
        RuleFeatureExecution execution, ClaimRuleActionDto request, out FrozenRuleAction action)
    {
        action = default!;
        using var rulesDocument = JsonDocument.Parse(execution.FrozenRulesJson);
        if (rulesDocument.RootElement.ValueKind != JsonValueKind.Object ||
            !rulesDocument.RootElement.TryGetProperty("rules", out var rules) ||
            rules.ValueKind != JsonValueKind.Array ||
            request.RuleIndex >= rules.GetArrayLength())
        {
            return false;
        }

        var rule = rules[request.RuleIndex];
        if (rule.ValueKind != JsonValueKind.Object ||
            !rule.TryGetProperty("id", out var ruleId) ||
            ruleId.ValueKind != JsonValueKind.String ||
            !string.Equals(ruleId.GetString(), request.Command.Correlation.RuleId, StringComparison.Ordinal) ||
            !rule.TryGetProperty("actions", out var actions) ||
            actions.ValueKind != JsonValueKind.Array ||
            request.ActionIndex >= actions.GetArrayLength())
        {
            return false;
        }

        var frozenAction = actions[request.ActionIndex];
        if (frozenAction.ValueKind != JsonValueKind.Object ||
            !frozenAction.TryGetProperty("id", out var actionId) ||
            actionId.ValueKind != JsonValueKind.String ||
            !string.Equals(actionId.GetString(), request.Command.ActionId, StringComparison.Ordinal) ||
            !frozenAction.TryGetProperty("typeId", out var typeId) ||
            typeId.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(typeId.GetString()) ||
            !frozenAction.TryGetProperty("parameters", out var parameters) ||
            parameters.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        action = new FrozenRuleAction(typeId.GetString()!, parameters.GetRawText());
        return true;
    }

    private static bool NativeEffectMatchesAction(string typeId, RuleNativeEffectType effectType) =>
        typeId switch
        {
            "player.kick" or "moderation.kick-with-record" or "moderation.temporary-ban" or "moderation.ban" =>
                effectType == RuleNativeEffectType.DropPlayer,
            "moderation.observation" => effectType == RuleNativeEffectType.Unknown,
            "message.public" => effectType == RuleNativeEffectType.PublicMessage,
            "message.private" => effectType == RuleNativeEffectType.PrivateMessage,
            _ => effectType == RuleNativeEffectType.Unknown
        };

    private static AdminActionType? ExpectedAdminActionType(string actionTypeId) =>
        actionTypeId switch
        {
            "moderation.observation" => AdminActionType.Observation,
            "moderation.kick-with-record" => AdminActionType.Kick,
            "moderation.temporary-ban" => AdminActionType.TempBan,
            "moderation.ban" => AdminActionType.Ban,
            _ => null
        };

    private sealed record FrozenRuleAction(string TypeId, string ParametersJson);

    private static bool ValidOutcomeEcho(
        Guid gameServerId, Guid executionId, string actionId, RuleActionExecution action, RuleActionResult result)
    {
        var correlation = Deserialize<RuleActionCorrelation>(action.CorrelationJson);
        return result.ProtocolVersion == 1 && result.GameServerId == gameServerId &&
            result.ExecutionId == executionId && result.ActionId == actionId &&
            result.LogicalEffectId == action.LogicalEffectId.Trim() && result.Correlation == correlation &&
            result.OccurredAtUtc != default;
    }

    private static bool IsLateAppliedEvidence(RuleActionExecution action, RuleActionResult result)
    {
        var command = Deserialize<RuleActionCommand>(action.CommandJson);
        return command.ExpiresAtUtc <= DateTimeOffset.UtcNow && IsAppliedWithinCommandWindow(action, result);
    }

    private static bool IsAppliedWithinCommandWindow(RuleActionExecution action, RuleActionResult result)
    {
        var command = Deserialize<RuleActionCommand>(action.CommandJson);
        var occurredAt = result.OccurredAtUtc.UtcDateTime;
        var claimedAt = DateTime.SpecifyKind(action.ClaimedAtUtc, DateTimeKind.Utc);
        var expiresAt = command.ExpiresAtUtc.UtcDateTime;

        return command.Mode == RuleExecutionMode.Enforce &&
            command.ExpiresAtUtc != default &&
            IsNativeEffect(command.Effect.Type) &&
            occurredAt >= claimedAt.Subtract(NativeTimestampSkewAllowance) &&
            occurredAt <= expiresAt.Add(NativeTimestampSkewAllowance) &&
            result.OccurredAtUtc <= DateTimeOffset.UtcNow.Add(NativeTimestampSkewAllowance);
    }

    private static bool IsHostAggregateConfirmation(RuleActionResult result) =>
        result.OccurredAtUtc <= DateTimeOffset.UtcNow.Add(NativeTimestampSkewAllowance);

    private static bool ValidNativeEvidence(
        Guid gameServerId, Guid executionId, string actionId, RuleActionExecution action, RuleActionResult evidence)
    {
        if (evidence.Outcome != RuleActionExecutionOutcome.Applied ||
            action.Mode != RuleExecutionMode.Enforce.ToString() ||
            !ValidOutcomeEcho(gameServerId, executionId, actionId, action, evidence))
        {
            return false;
        }

        var command = Deserialize<RuleActionCommand>(action.CommandJson);
        return command.Mode == RuleExecutionMode.Enforce &&
            IsAppliedWithinCommandWindow(action, evidence);
    }

    private static bool IsNativeEffect(RuleNativeEffectType effectType) =>
        effectType is RuleNativeEffectType.DropPlayer or RuleNativeEffectType.PublicMessage or RuleNativeEffectType.PrivateMessage;

    private static bool CanTransitionFromDispatched(RuleActionExecutionOutcome outcome) =>
        outcome is RuleActionExecutionOutcome.Applied or RuleActionExecutionOutcome.Failed or
            RuleActionExecutionOutcome.Stale or RuleActionExecutionOutcome.OutcomeUnknown;

    private static bool ValidOutcome(RuleActionExecutionOutcome outcome) =>
        outcome is RuleActionExecutionOutcome.Dispatched or RuleActionExecutionOutcome.Applied or
            RuleActionExecutionOutcome.Failed or RuleActionExecutionOutcome.Stale or
            RuleActionExecutionOutcome.SkippedUnsupported or RuleActionExecutionOutcome.SkippedDependency or
            RuleActionExecutionOutcome.WouldExecute or RuleActionExecutionOutcome.OutcomeUnknown;

    private static bool OutcomeAllowedForAction(
        RuleActionExecution action,
        RuleActionExecutionOutcome outcome,
        Guid? adminActionId)
    {
        var skipped = outcome is RuleActionExecutionOutcome.SkippedUnsupported or RuleActionExecutionOutcome.SkippedDependency;
        if (skipped)
        {
            return true;
        }
        if (action.Mode == RuleExecutionMode.DryRun.ToString())
        {
            return outcome == RuleActionExecutionOutcome.WouldExecute;
        }
        if (Deserialize<RuleActionCommand>(action.CommandJson).Effect.Type == RuleNativeEffectType.Unknown)
        {
            return outcome is RuleActionExecutionOutcome.Failed or RuleActionExecutionOutcome.Stale or
                RuleActionExecutionOutcome.OutcomeUnknown ||
                outcome == RuleActionExecutionOutcome.Applied &&
                (action.AdminActionId is not null || adminActionId is not null);
        }
        return outcome != RuleActionExecutionOutcome.WouldExecute;
    }

    private static bool ValidExecutionState(RuleFeatureExecutionState state) =>
        state is RuleFeatureExecutionState.Evaluating or RuleFeatureExecutionState.AwaitingActionOutcome or
            RuleFeatureExecutionState.Completed or RuleFeatureExecutionState.Stopped;

    private static bool ValidLogicalEffectId(string? value) =>
        value is { Length: 64 } && value.All(character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static bool ValidJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool ValidJsonObject(string? value)
    {
        if (!ValidJson(value))
        {
            return false;
        }
        using var document = JsonDocument.Parse(value!);
        return document.RootElement.ValueKind == JsonValueKind.Object;
    }

    private static bool ValidOptionalJson(string? value) => value is null || ValidJson(value);

    private static bool ValidFactRequest(Guid gameServerId, RuleFactSnapshotRequestDto? request) =>
        gameServerId != Guid.Empty && request is not null && request.ProtocolVersion == 1 &&
        request.ObservationId != Guid.Empty && request.ConnectionId != Guid.Empty &&
        Enum.IsDefined(request.GameType) && request.ObservedAtUtc != default &&
        request.ObservedAtUtc.Offset == TimeSpan.Zero &&
        (string.IsNullOrWhiteSpace(request.PlayerGuid) || request.PlayerGuid.Trim().Length <= 50) &&
        (string.IsNullOrWhiteSpace(request.PlayerIpAddress) || NormalizeIp(request.PlayerIpAddress) is not null);

    private static string? NormalizeGuid(string? value)
    {
        var guid = value?.Trim();
        return string.IsNullOrEmpty(guid) ? null : guid;
    }

    private static string? NormalizeIp(string? value)
    {
        if (!IPAddress.TryParse(value, out var address))
        {
            return null;
        }
        return (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
    }

    private static string? AlternateIp(string address)
    {
        if (!IPAddress.TryParse(address, out var parsed))
        {
            return null;
        }
        return parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            ? parsed.MapToIPv6().ToString()
            : parsed.IsIPv4MappedToIPv6 ? parsed.MapToIPv4().ToString() : null;
    }

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new InvalidOperationException($"Could not deserialize {typeof(T).Name}.");

    private static DateTimeOffset Utc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero);

    private sealed record PlayerFact(Guid PlayerId, DateTime FirstSeen);
}
