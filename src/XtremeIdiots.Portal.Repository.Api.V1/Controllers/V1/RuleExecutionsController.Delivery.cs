using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public sealed partial class RuleExecutionsController
{
    [HttpGet("game-servers/{gameServerId:guid}/rule-connections/{connectionId:guid}/executions")]
    [ProducesResponseType<ApiResponse<RuleFeatureExecutionDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConnectionExecutions(Guid gameServerId, Guid connectionId, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).GetConnectionExecutions(gameServerId, connectionId, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    [HttpPost("game-servers/{gameServerId:guid}/rule-executions/{executionId:guid}/prepared-actions")]
    [ProducesResponseType<ApiResponse<RulePreparedActionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<RulePreparedActionDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> PrepareAction(Guid gameServerId, Guid executionId, PrepareRuleActionDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).PrepareAction(gameServerId, executionId, request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    [HttpPost("game-servers/{gameServerId:guid}/rule-tagged-audience")]
    [ProducesResponseType<ApiResponse<RuleTaggedAudienceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaggedAudience(Guid gameServerId, RuleTaggedAudienceRequestDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).GetTaggedAudience(gameServerId, request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    async Task<ApiResult<RuleFeatureExecutionDto[]>> IRuleExecutionsApi.GetConnectionExecutions(
        Guid gameServerId, Guid connectionId, CancellationToken cancellationToken)
    {
        var executions = await context.RuleFeatureExecutions.AsNoTracking()
            .Where(item => item.GameServerId == gameServerId && item.ConnectionId == connectionId)
            .OrderBy(item => item.CreatedAtUtc).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var results = new List<RuleFeatureExecutionDto>();
        foreach (var execution in executions)
        {
            results.Add(await ToDtoAsync(execution, cancellationToken).ConfigureAwait(false));
        }
        return new ApiResponse<RuleFeatureExecutionDto[]>(results.ToArray()).ToApiResult();
    }

    async Task<ApiResult<RulePreparedActionDto>> IRuleExecutionsApi.PrepareAction(
        Guid gameServerId, Guid executionId, PrepareRuleActionDto request, CancellationToken cancellationToken)
    {
        if (request is null || request.ExpectedVersion <= 0 || request.RuleIndex < 0 || request.ActionIndex < 0 ||
            request.Commands is null || request.Commands.Length > 4096 || !ValidJsonObject(request.ResolvedParametersJson))
        {
            return new ApiResult<RulePreparedActionDto>(HttpStatusCode.BadRequest);
        }
        var gameType = await GetExecutionGameTypeAsync(gameServerId, executionId, cancellationToken).ConfigureAwait(false);
        if (gameType is null)
        {
            return new ApiResult<RulePreparedActionDto>(HttpStatusCode.NotFound);
        }
        return await ExecuteMutationAsync(async () =>
        {
            await AcquireGameHistoryReadLockAsync(gameType.Value, cancellationToken).ConfigureAwait(false);
            if (!await AcquireTransactionLockAsync($"rules-execution-id:{executionId:N}", cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RulePreparedActionDto>(HttpStatusCode.Conflict);
            }
            var execution = await context.RuleFeatureExecutions.SingleAsync(
                item => item.ExecutionId == executionId && item.GameServerId == gameServerId, cancellationToken).ConfigureAwait(false);
            var existing = await context.RulePreparedActions.SingleOrDefaultAsync(item =>
                item.ExecutionId == executionId && item.RuleIndex == request.RuleIndex && item.ActionIndex == request.ActionIndex,
                cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                return new ApiResponse<RulePreparedActionDto>(Deserialize<RulePreparedActionDto>(existing.PlanJson)).ToApiResult();
            }
            if (execution.Version != request.ExpectedVersion || execution.RuleIndex != request.RuleIndex ||
                execution.ActionIndex != request.ActionIndex || execution.State != nameof(RuleFeatureExecutionState.Evaluating) ||
                !TryGetContextTrigger(execution, out var trigger))
            {
                return new ApiResult<RulePreparedActionDto>(HttpStatusCode.Conflict);
            }
            using var rules = JsonDocument.Parse(execution.FrozenRulesJson);
            if (!TryGetActionDefinition(rules.RootElement, request.RuleIndex, request.ActionIndex, out var definition) ||
                definition.TypeId is not ("message.public" or "message.private" or RulesEngineCatalogueV1.PrivateTaggedMessageAction) ||
                !ValidResolvedParameters(definition, request.ResolvedParametersJson) ||
                !ValidDeliveryPlan(execution, trigger, definition, request))
            {
                return new ApiResult<RulePreparedActionDto>(HttpStatusCode.BadRequest);
            }
            var plan = new RulePreparedActionDto
            {
                ExecutionId = executionId,
                RuleIndex = request.RuleIndex,
                ActionIndex = request.ActionIndex,
                ActionId = definition.Id,
                ResolvedParametersJson = request.ResolvedParametersJson,
                Commands = request.Commands,
                AudienceSnapshot = request.AudienceSnapshot,
                PreparedAtUtc = DateTimeOffset.UtcNow
            };
            context.RulePreparedActions.Add(new RulePreparedAction
            {
                PreparationId = Guid.NewGuid(),
                ExecutionId = executionId,
                RuleIndex = request.RuleIndex,
                ActionIndex = request.ActionIndex,
                ActionId = definition.Id,
                PlanJson = JsonSerializer.Serialize(plan, JsonOptions),
                CreatedAtUtc = plan.PreparedAtUtc.UtcDateTime
            });
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse<RulePreparedActionDto>(plan).ToApiResult(HttpStatusCode.Created);
        }, cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<RuleTaggedAudienceDto>> IRuleExecutionsApi.GetTaggedAudience(
        Guid gameServerId, RuleTaggedAudienceRequestDto request, CancellationToken cancellationToken)
    {
        if (request?.TagIds is not { Length: > 0 and <= 100 } ||
            request.TagIds.Any(id => id == Guid.Empty) ||
            request.TagIds.Distinct().Count() != request.TagIds.Length || !Enum.IsDefined(request.Match))
        {
            return new ApiResult<RuleTaggedAudienceDto>(HttpStatusCode.BadRequest);
        }
        var snapshot = await context.RuleConnectionStatusSnapshots.AsNoTracking()
            .SingleOrDefaultAsync(item => item.GameServerId == gameServerId, cancellationToken).ConfigureAwait(false);
        if (snapshot is null || snapshot.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return new ApiResponse<RuleTaggedAudienceDto>(new RuleTaggedAudienceDto
            {
                GameServerId = gameServerId,
                Detail = "A fresh authoritative connection roster is unavailable."
            }).ToApiResult();
        }
        var roster = Deserialize<RuleConnectionStatusSnapshotDto>(snapshot.SnapshotJson);
        var connected = roster.Players.Where(item => item.State == RuleConnectionState.Connected).ToArray();
        var bindings = await context.RuleConnectionBindings.AsNoTracking().Where(item =>
            item.GameServerId == gameServerId && item.SnapshotId == snapshot.SnapshotId &&
            item.State == nameof(RuleConnectionState.Connected) && item.ExpiresAtUtc > DateTime.UtcNow)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var guids = connected.Select(item => item.PlayerGuid?.Trim().ToLowerInvariant()).Where(item => item is not null).ToArray();
        var players = await context.Players.AsNoTracking().Where(item => item.GameType == snapshot.GameType &&
            item.Guid != null && guids.Contains(item.Guid.Trim().ToLower()))
            .Select(item => new { item.PlayerId, item.Guid }).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var playerIds = players.Select(item => item.PlayerId).ToArray();
        var tags = await context.PlayerTags.AsNoTracking().Where(item => item.PlayerId.HasValue &&
            playerIds.Contains(item.PlayerId.Value) && item.TagId.HasValue && request.TagIds.Contains(item.TagId.Value))
            .Select(item => new { PlayerId = item.PlayerId!.Value, TagId = item.TagId!.Value })
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var partial = roster.Players.Any(item => item.State == RuleConnectionState.Unknown);
        var recipients = new List<RuleMessageRecipientTarget>();
        foreach (var candidate in connected)
        {
            var binding = bindings.SingleOrDefault(item => item.SlotId == candidate.SlotId &&
                string.Equals(item.PlayerGuid, candidate.PlayerGuid?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                item.ConnectionId == candidate.ConnectionId && item.RuntimeInstanceId == roster.Runtime.InstanceId);
            var matchingPlayers = players.Where(item =>
                string.Equals(item.Guid?.Trim(), candidate.PlayerGuid?.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
            if (binding is null || matchingPlayers.Length != 1)
            {
                partial = true;
                continue;
            }
            var assigned = tags.Where(item => item.PlayerId == matchingPlayers[0].PlayerId).Select(item => item.TagId).ToHashSet();
            if (request.Match == RuleRecipientTagMatch.All ? request.TagIds.All(assigned.Contains) : request.TagIds.Any(assigned.Contains))
            {
                recipients.Add(new()
                {
                    GameServerId = gameServerId,
                    Runtime = roster.Runtime,
                    ConnectionId = binding.ConnectionId,
                    ConnectionGeneration = binding.ConnectionGeneration is { } generation ? (ulong)generation : null,
                    PlayerGuid = binding.PlayerGuid,
                    SlotId = binding.SlotId
                });
            }
        }
        return new ApiResponse<RuleTaggedAudienceDto>(new RuleTaggedAudienceDto
        {
            GameServerId = gameServerId,
            SnapshotId = snapshot.SnapshotId,
            ObservedAtUtc = Utc(snapshot.ObservedAtUtc),
            Available = true,
            Partial = partial,
            Detail = partial ? "Some roster identities or authoritative player tags are unavailable." : null,
            Recipients = recipients.DistinctBy(item => item.ConnectionId).ToArray()
        }).ToApiResult();
    }

    private static bool ValidDeliveryPlan(
        RuleFeatureExecution execution, RuleTriggerEnvelope trigger, RuleActionDefinition definition, PrepareRuleActionDto request)
    {
        if (definition.TypeId == RulesEngineCatalogueV1.PrivateTaggedMessageAction &&
            request.AudienceSnapshot is not { Available: true, Partial: false })
        {
            return false;
        }
        if (request.Commands.Length == 0)
        {
            return definition.TypeId == RulesEngineCatalogueV1.PrivateTaggedMessageAction &&
                request.AudienceSnapshot is { Available: true, Partial: false, Recipients.Length: 0 };
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var positions = new HashSet<(Guid?, int, int)>();
        foreach (var command in request.Commands)
        {
            if (command?.DeliveryIdentity is not { } identity ||
                !ValidLogicalEffectId(identity.ParentLogicalEffectId) || identity.MessageIndex < 0 || identity.FragmentIndex < 0 ||
                command.LogicalEffectId != identity.ToLogicalEffectId() || !ids.Add(command.LogicalEffectId) ||
                !positions.Add((identity.RecipientConnectionId, identity.MessageIndex, identity.FragmentIndex)) ||
                command.ActionId != definition.Id || !CorrelationMatchesExecution(command.Correlation, execution, trigger) ||
                command.ExpiresAtUtc <= DateTimeOffset.UtcNow ||
                command.ExpiresAtUtc > (execution.CoordinationJson is { } coordinationJson
                    ? Deserialize<RuleExecutionCoordination>(coordinationJson).ExpiresAtUtc : trigger.OccurredAtUtc.AddSeconds(120)) ||
                !CommandMatchesTrigger(command, trigger) || !NativeEffectMatchesAction(definition.TypeId, command.Effect.Type) ||
                !ValidClaimRequest(execution.GameServerId, execution.ExecutionId, new()
                {
                    ExpectedVersion = request.ExpectedVersion, RuleIndex = request.RuleIndex, ActionIndex = request.ActionIndex,
                    LogicalEffectId = command.LogicalEffectId, Command = command
                }) || !ValidNativeMessage(command))
            {
                return false;
            }
            if (definition.TypeId == RulesEngineCatalogueV1.PrivateTaggedMessageAction)
            {
                if (command.RecipientTarget is not { } recipient || identity.RecipientConnectionId != recipient.ConnectionId ||
                    !request.AudienceSnapshot!.Recipients.Any(item =>
                        JsonSerializer.Serialize(item, JsonOptions) == JsonSerializer.Serialize(recipient, JsonOptions)))
                {
                    return false;
                }
            }
            else if (command.RecipientTarget is not null || identity.RecipientConnectionId is not null)
            {
                return false;
            }
        }
        return request.Commands.Select(command => command.DeliveryIdentity!.ParentLogicalEffectId).Distinct(StringComparer.Ordinal).Count() == 1 &&
            request.Commands.GroupBy(command => command.DeliveryIdentity!.RecipientConnectionId).All(group =>
                group.Select(command => (command.DeliveryIdentity!.MessageIndex, command.DeliveryIdentity.FragmentIndex))
                    .SequenceEqual(group.OrderBy(command => command.DeliveryIdentity!.MessageIndex)
                        .ThenBy(command => command.DeliveryIdentity!.FragmentIndex)
                        .Select(command => (command.DeliveryIdentity!.MessageIndex, command.DeliveryIdentity.FragmentIndex)))) &&
            request.Commands.GroupBy(command => (command.DeliveryIdentity!.RecipientConnectionId, command.DeliveryIdentity.MessageIndex))
                .All(group => group.Select(command => command.DeliveryIdentity!.FragmentIndex).SequenceEqual(Enumerable.Range(0, group.Count())));
    }

    private static RulePreparedActionDto WithDeliveryOutcome(RulePreparedActionDto plan, IReadOnlyList<RuleActionExecution> actions)
    {
        var ids = plan.Commands.Select(item => item.LogicalEffectId).ToHashSet(StringComparer.Ordinal);
        var children = actions.Where(item => ids.Contains(item.LogicalEffectId.Trim())).ToArray();
        var completed = children.Count(item => item.Outcome is nameof(RuleActionExecutionOutcome.Applied) or nameof(RuleActionExecutionOutcome.WouldExecute));
        var outcome = plan.Commands.Length == 0 || completed == plan.Commands.Length
            ? RuleActionExecutionOutcome.Applied
            : children.Any(item => item.Outcome == nameof(RuleActionExecutionOutcome.OutcomeUnknown))
                ? RuleActionExecutionOutcome.OutcomeUnknown
                : children.Any(item => item.Outcome is nameof(RuleActionExecutionOutcome.Failed) or nameof(RuleActionExecutionOutcome.Stale) or
                    nameof(RuleActionExecutionOutcome.SkippedUnsupported) or nameof(RuleActionExecutionOutcome.SkippedDependency))
                    ? RuleActionExecutionOutcome.Failed
                    : RuleActionExecutionOutcome.Pending;
        return plan with
        {
            Outcome = outcome,
            Detail = plan.Commands.Length == 0 ? "No recipients." : $"{completed}/{plan.Commands.Length} fragments completed.",
            CompletedFragmentCount = completed,
            TotalFragmentCount = plan.Commands.Length
        };
    }

    private static bool ValidNativeMessage(RuleActionCommand command)
    {
        var text = command.Effect.Message;
        return !string.IsNullOrWhiteSpace(text) && !text.Any(character => character < ' ' || character == '\u007f' ||
            character == '"' || command.Runtime.IdentityModel == RuleRuntimeIdentityModel.AgentObservedConnection &&
                (character > '~' || character is '\\' or ';')) &&
            Encoding.UTF8.GetByteCount(text) <= (command.Runtime.Id == RulesEngineCatalogueV1.Cod4xPluginRuntime ? 149 : 256);
    }

    private static bool TryGetActionDefinition(JsonElement root, int ruleIndex, int actionIndex, out RuleActionDefinition action)
    {
        action = default!;
        if (!root.TryGetProperty("rules", out var rules) || rules.ValueKind != JsonValueKind.Array ||
            ruleIndex >= rules.GetArrayLength() || !rules[ruleIndex].TryGetProperty("actions", out var actions) ||
            actions.ValueKind != JsonValueKind.Array || actionIndex >= actions.GetArrayLength())
        {
            return false;
        }
        try
        {
            action = JsonSerializer.Deserialize<RuleActionDefinition>(actions[actionIndex].GetRawText(), JsonOptions)!;
            return action is not null && action.Parameters is not null && action.TemplateParameters is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool ValidResolvedParameters(RuleActionDefinition definition, string json)
    {
        if (!ValidJsonObject(json))
        {
            return false;
        }
        using var document = JsonDocument.Parse(json);
        var resolved = document.RootElement.EnumerateObject().ToDictionary(item => item.Name, item => item.Value);
        if (resolved.Count != definition.Parameters.Count)
        {
            return false;
        }
        var schema = RulesEngineCatalogueV1.Current.Actions.SingleOrDefault(item => item.ActionTypeId == definition.TypeId);
        foreach (var parameter in definition.Parameters)
        {
            if (!resolved.TryGetValue(parameter.Key, out var value))
            {
                return false;
            }
            if (!definition.TemplateParameters.Contains(parameter.Key, StringComparer.Ordinal))
            {
                if (!JsonElement.DeepEquals(parameter.Value, value))
                {
                    return false;
                }
                continue;
            }
            var constraint = schema?.Parameters.SingleOrDefault(item => item.Name == parameter.Key && item.SupportsTemplates);
            if (constraint is null || constraint.Type == "string" && value.ValueKind != JsonValueKind.String ||
                constraint.Type == "string[]" && value.ValueKind != JsonValueKind.Array)
            {
                return false;
            }
            var texts = value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().ToArray() : [value];
            if (texts.Any(item => item.ValueKind != JsonValueKind.String || item.GetString()!.Length > (constraint.MaxLength ?? int.MaxValue) ||
                constraint.MaxUtf8Bytes is { } bytes && Encoding.UTF8.GetByteCount(item.GetString()!) > bytes ||
                constraint.RejectControlCharacters && item.GetString()!.Any(character => character < ' ' || character == '\u007f')))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ValidRecipient(RuleActionCommand command) =>
        command.RecipientTarget is not { } target ||
        command.Effect.Type == RuleNativeEffectType.PrivateMessage && target.GameServerId == command.GameServerId &&
        target.ConnectionId != Guid.Empty && target.SlotId >= 0 && !string.IsNullOrWhiteSpace(target.PlayerGuid) &&
        target.PlayerGuid.Length <= 50 && SameRuntimeIdentity(target.Runtime, command.Runtime) &&
        (target.Runtime.IdentityModel == RuleRuntimeIdentityModel.PluginRuntimeGeneration
            ? target.ConnectionGeneration is > 0 && target.Runtime.Capabilities.Contains(RulesEngineCatalogueV1.Cod4xPrivateRecipientCapability, StringComparer.Ordinal)
            : target.ConnectionGeneration is null);

    private static bool ValidCoordination(RuleExecutionCoordination? coordination, RuleTriggerEnvelope trigger) =>
        coordination is null ||
        coordination.DueAtUtc >= trigger.OccurredAtUtc && coordination.DueAtUtc <= trigger.OccurredAtUtc.AddSeconds(60) &&
        coordination.ExpiresAtUtc > coordination.DueAtUtc && coordination.ExpiresAtUtc <= trigger.OccurredAtUtc.AddSeconds(900) &&
        coordination.PredecessorExecutionIds is not null &&
        coordination.PredecessorExecutionIds.All(id => id != Guid.Empty) &&
        coordination.PredecessorExecutionIds.Distinct().Count() == coordination.PredecessorExecutionIds.Length &&
        coordination.WaitingReason is not { Length: > 1024 };

    private async Task<bool> DeliveryPrerequisitesSatisfiedAsync(
        RuleFeatureExecution execution, RuleActionCommand command, CancellationToken cancellationToken)
    {
        if (execution.CoordinationJson is not { } json || command.RecipientTarget is not null ||
            command.Effect.Type is not (RuleNativeEffectType.PublicMessage or RuleNativeEffectType.PrivateMessage))
        {
            return true;
        }
        var coordination = Deserialize<RuleExecutionCoordination>(json);
        if (DateTimeOffset.UtcNow < coordination.DueAtUtc || DateTimeOffset.UtcNow >= coordination.ExpiresAtUtc)
        {
            return false;
        }
        if (execution.FeatureId != RulesEngineCatalogueV1.WelcomeMessagesFeature)
        {
            return true;
        }
        var predecessors = await context.RuleFeatureExecutions.AsNoTracking().Where(item =>
            item.GameServerId == execution.GameServerId && item.ConnectionId == execution.ConnectionId &&
            (item.FeatureId == RulesEngineCatalogueV1.VpnProtectionFeature || item.FeatureId == RulesEngineCatalogueV1.SiegeModeFeature))
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
        if (coordination.PredecessorExecutionIds.Any(id => !predecessors.Any(item => item.ExecutionId == id)) ||
            predecessors.Any(item => !coordination.PredecessorExecutionIds.Contains(item.ExecutionId) ||
                item.State is not (nameof(RuleFeatureExecutionState.Completed) or nameof(RuleFeatureExecutionState.Stopped))))
        {
            return false;
        }
        var predecessorIds = predecessors.Select(item => item.ExecutionId).ToArray();
        return !await context.RuleActionExecutions.AsNoTracking().AnyAsync(item => predecessorIds.Contains(item.ExecutionId) &&
            (item.Outcome == nameof(RuleActionExecutionOutcome.Claimed) || item.Outcome == nameof(RuleActionExecutionOutcome.Dispatched) ||
                item.Outcome == nameof(RuleActionExecutionOutcome.Applied) || item.Outcome == nameof(RuleActionExecutionOutcome.OutcomeUnknown)) &&
            (item.ActionTypeId == "player.kick" || item.ActionTypeId == "moderation.kick-with-record" ||
                item.ActionTypeId == "moderation.temporary-ban" || item.ActionTypeId == "moderation.ban"),
            cancellationToken).ConfigureAwait(false);
    }
}
