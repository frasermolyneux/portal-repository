using System.Net;
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
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public sealed partial class RuleExecutionsController
{
    [HttpPost("game-servers/{gameServerId:guid}/chat-occurrences/{occurrenceId:guid}/effects")]
    [ProducesResponseType<ApiResponse<ClaimRuleOccurrenceEffectResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<ClaimRuleOccurrenceEffectResultDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClaimOccurrenceEffect(
        Guid gameServerId, Guid occurrenceId, ClaimRuleOccurrenceEffectDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).ClaimOccurrenceEffect(gameServerId, occurrenceId, request, cancellationToken)
            .ConfigureAwait(false)).ToHttpResult();

    [HttpPut("game-servers/{gameServerId:guid}/chat-occurrences/{occurrenceId:guid}/effects/{logicalEffectId}/admin-action")]
    [ProducesResponseType<ApiResponse<EnsureAutomatedActionResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<EnsureAutomatedActionResultDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EnsureOccurrenceAdminAction(
        Guid gameServerId, Guid occurrenceId, string logicalEffectId, EnsureRuleOccurrenceAdminActionDto request,
        CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).EnsureOccurrenceAdminAction(
            gameServerId, occurrenceId, logicalEffectId, request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    [HttpPut("game-servers/{gameServerId:guid}/chat-occurrences/{occurrenceId:guid}/effects/{logicalEffectId}/outcome")]
    [ProducesResponseType<ApiResponse<RuleOccurrenceEffectDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOccurrenceEffectOutcome(
        Guid gameServerId, Guid occurrenceId, string logicalEffectId, UpdateRuleOccurrenceEffectOutcomeDto request,
        CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).UpdateOccurrenceEffectOutcome(
            gameServerId, occurrenceId, logicalEffectId, request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    async Task<ApiResult<ClaimRuleOccurrenceEffectResultDto>> IRuleExecutionsApi.ClaimOccurrenceEffect(
        Guid gameServerId, Guid occurrenceId, ClaimRuleOccurrenceEffectDto request, CancellationToken cancellationToken)
    {
        if (!ValidOccurrenceClaim(request) ||
            !RuleOccurrenceEffectIdentity.TryCreate(
                gameServerId, occurrenceId, request.FeatureId, request.RuleId, request.ActionId, out var expectedEffectId) ||
            request.LogicalEffectId != expectedEffectId)
        {
            return new ApiResult<ClaimRuleOccurrenceEffectResultDto>(HttpStatusCode.BadRequest);
        }

        var gameType = await GetOccurrenceServerGameTypeAsync(gameServerId, cancellationToken).ConfigureAwait(false);
        if (gameType is null)
        {
            return new ApiResult<ClaimRuleOccurrenceEffectResultDto>(HttpStatusCode.NotFound);
        }

        return await ExecuteMutationAsync(async () =>
        {
            await AcquireGameHistoryReadLockAsync(gameType.Value, cancellationToken).ConfigureAwait(false);
            if (!await context.GameServers.AnyAsync(server => server.GameServerId == gameServerId &&
                    !server.Deleted && server.GameType == gameType.Value, cancellationToken).ConfigureAwait(false) ||
                !await context.Players.AnyAsync(player => player.PlayerId == request.PlayerId &&
                    player.GameType == gameType.Value, cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<ClaimRuleOccurrenceEffectResultDto>(HttpStatusCode.NotFound);
            }
            if (!await AcquireTransactionLockAsync(
                    $"rules-occurrence-effect:{gameServerId:N}:{occurrenceId:N}:{request.LogicalEffectId}", cancellationToken)
                    .ConfigureAwait(false))
            {
                return new ApiResult<ClaimRuleOccurrenceEffectResultDto>(HttpStatusCode.Conflict);
            }

            if (request.Cooldown is { } requestedPolicy &&
                !await AcquireTransactionLockAsync(
                    $"rules-occurrence-cooldown:{gameServerId:N}:{request.PlayerId:N}:{requestedPolicy.PolicyId.ToUpperInvariant()}",
                    cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<ClaimRuleOccurrenceEffectResultDto>(HttpStatusCode.Conflict);
            }

            var effectQuery = context.Database.IsSqlServer()
                ? context.RuleOccurrenceEffects.FromSqlInterpolated($"""
                    SELECT * FROM [dbo].[RuleOccurrenceEffects] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [GameServerId] = {gameServerId} AND [OccurrenceId] = {occurrenceId}
                        AND [LogicalEffectId] = {request.LogicalEffectId}
                    """)
                : context.RuleOccurrenceEffects.AsQueryable();
            var existing = await effectQuery.SingleOrDefaultAsync(effect =>
                effect.GameServerId == gameServerId && effect.OccurrenceId == occurrenceId &&
                effect.LogicalEffectId == request.LogicalEffectId, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                if (!SameOccurrenceClaim(existing, request))
                {
                    return new ApiResult<ClaimRuleOccurrenceEffectResultDto>(HttpStatusCode.Conflict);
                }
                var disposition = existing.Outcome == nameof(RuleOccurrenceEffectOutcome.CooldownSuppressed)
                    ? RuleOccurrenceEffectDisposition.CooldownSuppressed
                    : RuleOccurrenceEffectDisposition.AlreadyClaimed;
                return OccurrenceClaimResult(existing, disposition, shouldDispatch: false);
            }
            var now = DateTime.UtcNow;
            DateTime? cooldownUntil = null;
            var suppressed = false;
            if (request.Cooldown is { } requestedCooldown)
            {
                now = DateTime.UtcNow;
                var cooldownQuery = context.Database.IsSqlServer()
                    ? context.RuleOccurrenceCooldowns.FromSqlInterpolated($"""
                        SELECT * FROM [dbo].[RuleOccurrenceCooldowns] WITH (UPDLOCK, HOLDLOCK)
                        WHERE [GameServerId] = {gameServerId} AND [PlayerId] = {request.PlayerId}
                            AND [PolicyId] = {requestedCooldown.PolicyId}
                        """)
                    : context.RuleOccurrenceCooldowns.AsQueryable();
                var cooldown = await cooldownQuery.SingleOrDefaultAsync(item =>
                    item.GameServerId == gameServerId && item.PlayerId == request.PlayerId &&
                    item.PolicyId == requestedCooldown.PolicyId, cancellationToken).ConfigureAwait(false);
                if (cooldown is not null && cooldown.NextAllowedAtUtc > now)
                {
                    suppressed = true;
                    cooldownUntil = cooldown.NextAllowedAtUtc;
                }
                else
                {
                    cooldownUntil = now.AddSeconds(requestedCooldown.DurationSeconds);
                    if (cooldown is null)
                    {
                        context.RuleOccurrenceCooldowns.Add(new RuleOccurrenceCooldown
                        {
                            GameServerId = gameServerId,
                            PlayerId = request.PlayerId,
                            PolicyId = requestedCooldown.PolicyId,
                            NextAllowedAtUtc = cooldownUntil.Value
                        });
                    }
                    else
                    {
                        cooldown.NextAllowedAtUtc = cooldownUntil.Value;
                    }
                }
            }

            var effect = new RuleOccurrenceEffect
            {
                GameServerId = gameServerId,
                OccurrenceId = occurrenceId,
                LogicalEffectId = request.LogicalEffectId,
                PlayerId = request.PlayerId,
                FeatureId = request.FeatureId,
                RuleId = request.RuleId,
                RuleIndex = request.RuleIndex,
                ActionId = request.ActionId,
                ActionIndex = request.ActionIndex,
                ActionTypeId = request.ActionTypeId,
                RuleSetRevision = request.RuleSetRevision,
                Mode = request.Mode.ToString(),
                CooldownPolicyId = request.Cooldown?.PolicyId,
                CooldownSeconds = request.Cooldown?.DurationSeconds,
                CooldownUntilUtc = cooldownUntil,
                Outcome = (suppressed ? RuleOccurrenceEffectOutcome.CooldownSuppressed : RuleOccurrenceEffectOutcome.Claimed).ToString(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            context.RuleOccurrenceEffects.Add(effect);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return OccurrenceClaimResult(effect,
                suppressed ? RuleOccurrenceEffectDisposition.CooldownSuppressed : RuleOccurrenceEffectDisposition.Acquired,
                shouldDispatch: !suppressed, suppressed ? HttpStatusCode.OK : HttpStatusCode.Created);
        }, cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<RuleOccurrenceEffectDto>> IRuleExecutionsApi.UpdateOccurrenceEffectOutcome(
        Guid gameServerId, Guid occurrenceId, string logicalEffectId, UpdateRuleOccurrenceEffectOutcomeDto request,
        CancellationToken cancellationToken)
    {
        if (gameServerId == Guid.Empty || occurrenceId == Guid.Empty || !ValidLogicalEffectId(logicalEffectId) ||
            request is null || request.Outcome is not (RuleOccurrenceEffectOutcome.Dispatched or
                RuleOccurrenceEffectOutcome.Failed or RuleOccurrenceEffectOutcome.OutcomeUnknown))
        {
            return new ApiResult<RuleOccurrenceEffectDto>(HttpStatusCode.BadRequest);
        }
        var gameType = await GetOccurrenceServerGameTypeAsync(gameServerId, cancellationToken).ConfigureAwait(false);
        if (gameType is null)
        {
            return new ApiResult<RuleOccurrenceEffectDto>(HttpStatusCode.NotFound);
        }

        return await ExecuteMutationAsync(async () =>
        {
            await AcquireGameHistoryReadLockAsync(gameType.Value, cancellationToken).ConfigureAwait(false);
            if (!await AcquireTransactionLockAsync(
                    $"rules-occurrence-effect:{gameServerId:N}:{occurrenceId:N}:{logicalEffectId}", cancellationToken)
                    .ConfigureAwait(false))
            {
                return new ApiResult<RuleOccurrenceEffectDto>(HttpStatusCode.Conflict);
            }
            var effect = await context.RuleOccurrenceEffects.SingleOrDefaultAsync(item =>
                item.GameServerId == gameServerId && item.OccurrenceId == occurrenceId &&
                item.LogicalEffectId == logicalEffectId, cancellationToken).ConfigureAwait(false);
            if (effect is null)
            {
                return new ApiResult<RuleOccurrenceEffectDto>(HttpStatusCode.NotFound);
            }
            var outcome = request.Outcome.ToString();
            if (effect.Outcome == outcome)
            {
                return new ApiResponse<RuleOccurrenceEffectDto>(ToOccurrenceEffectDto(effect)).ToApiResult();
            }
            if (effect.Outcome != nameof(RuleOccurrenceEffectOutcome.Claimed) &&
                !(effect.Outcome == nameof(RuleOccurrenceEffectOutcome.Dispatched) &&
                    request.Outcome is RuleOccurrenceEffectOutcome.Failed or RuleOccurrenceEffectOutcome.OutcomeUnknown))
            {
                return new ApiResult<RuleOccurrenceEffectDto>(HttpStatusCode.Conflict);
            }
            effect.Outcome = outcome;
            effect.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse<RuleOccurrenceEffectDto>(ToOccurrenceEffectDto(effect)).ToApiResult();
        }, cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<EnsureAutomatedActionResultDto>> IRuleExecutionsApi.EnsureOccurrenceAdminAction(
        Guid gameServerId, Guid occurrenceId, string logicalEffectId, EnsureRuleOccurrenceAdminActionDto request,
        CancellationToken cancellationToken)
    {
        if (gameServerId == Guid.Empty || occurrenceId == Guid.Empty || !ValidLogicalEffectId(logicalEffectId) ||
            request is null || request.Type is not (AdminActionType.Observation or AdminActionType.Kick or
                AdminActionType.TempBan or AdminActionType.Ban) ||
            request.ExistingAdminActionId == Guid.Empty || request.AdminId is { Length: > 128 } ||
            request.ExistingAdminActionId is not null && request.Type is not (AdminActionType.TempBan or AdminActionType.Ban) ||
            request.ExistingAdminActionId is null &&
                (request.Type is AdminActionType.TempBan or AdminActionType.Ban ||
                 string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 512 || request.Expires is not null))
        {
            return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.BadRequest);
        }

        var gameType = await GetOccurrenceServerGameTypeAsync(gameServerId, cancellationToken).ConfigureAwait(false);
        if (gameType is null)
        {
            return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.NotFound);
        }

        return await ExecuteMutationAsync(async () =>
        {
            await AcquireGameHistoryReadLockAsync(gameType.Value, cancellationToken).ConfigureAwait(false);
            if (!await AcquireTransactionLockAsync(
                    $"rules-occurrence-effect:{gameServerId:N}:{occurrenceId:N}:{logicalEffectId}", cancellationToken)
                    .ConfigureAwait(false))
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.Conflict);
            }

            var effect = await context.RuleOccurrenceEffects.SingleOrDefaultAsync(item =>
                item.GameServerId == gameServerId && item.OccurrenceId == occurrenceId &&
                item.LogicalEffectId == logicalEffectId, cancellationToken).ConfigureAwait(false);
            if (effect is null)
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.NotFound);
            }
            if (effect.FeatureId != "chat-moderation" || ExpectedAdminActionType(effect.ActionTypeId) != request.Type)
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.BadRequest);
            }

            if (effect.AdminActionRecorded)
            {
                if (effect.AdminActionId is not { } recordedId ||
                    request.ExistingAdminActionId is { } requestedId && requestedId != recordedId)
                {
                    return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.Conflict);
                }
                var recorded = await context.AdminActions.SingleOrDefaultAsync(
                    action => action.AdminActionId == recordedId, cancellationToken).ConfigureAwait(false);
                return recorded is null
                    ? new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.Conflict)
                    : OccurrenceAdminActionResult(recorded, created: false, HttpStatusCode.OK);
            }
            if (effect.Outcome != nameof(RuleOccurrenceEffectOutcome.Claimed))
            {
                return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.Conflict);
            }

            AdminAction adminAction;
            if (request.ExistingAdminActionId is { } existingId)
            {
                var existing = await context.AdminActions.SingleOrDefaultAsync(
                    action => action.AdminActionId == existingId && action.PlayerId == effect.PlayerId,
                    cancellationToken).ConfigureAwait(false);
                if (existing is null || !CanLinkOccurrenceAdminAction(existing, request.Type.Value))
                {
                    return new ApiResult<EnsureAutomatedActionResultDto>(HttpStatusCode.BadRequest);
                }
                adminAction = existing;
            }
            else
            {
                adminAction = new AdminAction
                {
                    AdminActionId = Guid.NewGuid(),
                    PlayerId = effect.PlayerId,
                    Type = request.Type.Value.ToAdminActionTypeInt(),
                    Text = request.Text!,
                    Created = DateTime.UtcNow,
                    Source = (byte)ActionSource.Automation,
                    AutomationFeature = (int)AutomationFeature.ChatModeration,
                    AutomationRuleId = effect.RuleId
                };
                if (!string.IsNullOrWhiteSpace(request.AdminId))
                {
                    adminAction.UserProfileId = await context.UserProfiles
                        .Where(profile => profile.IdentityOid == request.AdminId || profile.XtremeIdiotsForumId == request.AdminId)
                        .Select(profile => (Guid?)profile.UserProfileId)
                        .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                }
                context.AdminActions.Add(adminAction);
            }

            effect.AdminActionId = adminAction.AdminActionId;
            effect.AdminActionRecorded = true;
            effect.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return OccurrenceAdminActionResult(adminAction, request.ExistingAdminActionId is null,
                request.ExistingAdminActionId is null ? HttpStatusCode.Created : HttpStatusCode.OK);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static bool CanLinkOccurrenceAdminAction(AdminAction action, AdminActionType requestedType)
    {
        var actualType = action.Type.ToAdminActionType();
        if (actualType != requestedType &&
            !(requestedType == AdminActionType.TempBan && actualType == AdminActionType.Ban))
        {
            return false;
        }
        return actualType switch
        {
            AdminActionType.TempBan => action.Expires > DateTime.UtcNow,
            AdminActionType.Ban => action.Expires is null || action.Expires > DateTime.UtcNow,
            _ => true
        };
    }

    private static ApiResult<EnsureAutomatedActionResultDto> OccurrenceAdminActionResult(
        AdminAction action, bool created, HttpStatusCode status) =>
        new ApiResponse<EnsureAutomatedActionResultDto>(new EnsureAutomatedActionResultDto
        {
            Created = created,
            AdminAction = action.ToDto(expand: false)
        }).ToApiResult(status);

    private Task<int?> GetOccurrenceServerGameTypeAsync(Guid gameServerId, CancellationToken cancellationToken) =>
        context.GameServers.AsNoTracking()
            .Where(server => server.GameServerId == gameServerId && !server.Deleted)
            .Select(server => (int?)server.GameType).SingleOrDefaultAsync(cancellationToken);

    private static bool ValidOccurrenceClaim(ClaimRuleOccurrenceEffectDto? request) =>
        request is not null && ValidLogicalEffectId(request.LogicalEffectId) && request.PlayerId != Guid.Empty &&
        request.Mode == RuleExecutionMode.Enforce &&
        request.RuleIndex >= 0 && request.ActionIndex >= 0 &&
        ValidEvidenceId(request.FeatureId) && request.FeatureId.Length <= 64 &&
        ValidEvidenceId(request.RuleId) && ValidEvidenceId(request.ActionId) &&
        ValidRequiredOccurrenceId(request.ActionTypeId, RuleSetConstants.MaxIdLength) &&
        ValidRequiredOccurrenceId(request.RuleSetRevision, 128) &&
        (request.Cooldown is null ||
            ValidRequiredOccurrenceId(request.Cooldown.PolicyId, RuleSetConstants.MaxIdLength) &&
            request.Cooldown.DurationSeconds > 0);

    private static bool ValidRequiredOccurrenceId(string? value, int maximumLength) =>
        value is not null && ValidEvidenceMetadata(value, maximumLength);

    private static bool SameOccurrenceClaim(RuleOccurrenceEffect effect, ClaimRuleOccurrenceEffectDto request) =>
        effect.PlayerId == request.PlayerId && effect.FeatureId == request.FeatureId &&
        effect.RuleId == request.RuleId && effect.RuleIndex == request.RuleIndex &&
        effect.ActionId == request.ActionId && effect.ActionIndex == request.ActionIndex &&
        effect.ActionTypeId == request.ActionTypeId && effect.RuleSetRevision == request.RuleSetRevision &&
        effect.Mode == request.Mode.ToString() &&
        string.Equals(effect.CooldownPolicyId, request.Cooldown?.PolicyId, StringComparison.OrdinalIgnoreCase) &&
        effect.CooldownSeconds == request.Cooldown?.DurationSeconds;

    private static ApiResult<ClaimRuleOccurrenceEffectResultDto> OccurrenceClaimResult(
        RuleOccurrenceEffect effect, RuleOccurrenceEffectDisposition disposition, bool shouldDispatch,
        HttpStatusCode status = HttpStatusCode.OK) =>
        new ApiResponse<ClaimRuleOccurrenceEffectResultDto>(new ClaimRuleOccurrenceEffectResultDto
        {
            Disposition = disposition,
            ShouldDispatch = shouldDispatch,
            Effect = ToOccurrenceEffectDto(effect)
        }).ToApiResult(status);

    private static RuleOccurrenceEffectDto ToOccurrenceEffectDto(RuleOccurrenceEffect effect) => new()
    {
        GameServerId = effect.GameServerId,
        OccurrenceId = effect.OccurrenceId,
        Claim = new ClaimRuleOccurrenceEffectDto
        {
            LogicalEffectId = effect.LogicalEffectId.Trim(),
            PlayerId = effect.PlayerId,
            FeatureId = effect.FeatureId,
            RuleId = effect.RuleId,
            RuleIndex = effect.RuleIndex,
            ActionId = effect.ActionId,
            ActionIndex = effect.ActionIndex,
            ActionTypeId = effect.ActionTypeId,
            RuleSetRevision = effect.RuleSetRevision,
            Mode = Enum.Parse<RuleExecutionMode>(effect.Mode),
            Cooldown = effect.CooldownPolicyId is { } policy
                ? new RuleOccurrenceCooldownDto { PolicyId = policy, DurationSeconds = effect.CooldownSeconds!.Value }
                : null
        },
        AdminActionRecorded = effect.AdminActionRecorded,
        AdminActionId = effect.AdminActionId,
        Outcome = Enum.Parse<RuleOccurrenceEffectOutcome>(effect.Outcome),
        CooldownUntilUtc = effect.CooldownUntilUtc is { } until ? Utc(until) : null,
        CreatedAtUtc = Utc(effect.CreatedAtUtc),
        UpdatedAtUtc = Utc(effect.UpdatedAtUtc)
    };
}
