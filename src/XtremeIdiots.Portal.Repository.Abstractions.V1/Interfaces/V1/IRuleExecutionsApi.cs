using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IRuleExecutionsApi
{
    Task<ApiResult<RuleExecutionFlowDto>> GetRuleExecutionFlow(Guid executionId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Upgrade the Repository API client to view a rule execution flow.");
    Task<ApiResult<CollectionModel<RuleExecutionAuditDto>>> SearchRuleExecutionAudit(RuleExecutionAuditQueryDto request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Upgrade the Repository API client to search the rule execution audit.");
    Task<ApiResult<CollectionModel<RuleFeatureExecutionDto>>> SearchRuleExecutions(RuleFeatureExecutionQueryDto request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Upgrade the Repository API client to search rule executions.");
    Task<ApiResult<RuleFeatureExecutionDto[]>> GetConnectionExecutions(Guid gameServerId, Guid connectionId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Upgrade the Repository API client to use connection execution lookup.");
    Task<ApiResult<RulePreparedActionDto>> PrepareAction(Guid gameServerId, Guid executionId, PrepareRuleActionDto request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Upgrade the Repository API client to prepare rule actions.");
    Task<ApiResult<RuleTaggedAudienceDto>> GetTaggedAudience(Guid gameServerId, RuleTaggedAudienceRequestDto request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Upgrade the Repository API client to resolve tagged audiences.");
    Task<ApiResult<ClaimRuleOccurrenceEffectResultDto>> ClaimOccurrenceEffect(Guid gameServerId, Guid occurrenceId, ClaimRuleOccurrenceEffectDto request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Upgrade the Repository API client to claim occurrence effects.");
    Task<ApiResult<EnsureAutomatedActionResultDto>> EnsureOccurrenceAdminAction(Guid gameServerId, Guid occurrenceId, string logicalEffectId, EnsureRuleOccurrenceAdminActionDto request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Upgrade the Repository API client to link occurrence moderation records.");
    Task<ApiResult<RuleOccurrenceEffectDto>> UpdateOccurrenceEffectOutcome(Guid gameServerId, Guid occurrenceId, string logicalEffectId, UpdateRuleOccurrenceEffectOutcomeDto request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Upgrade the Repository API client to report occurrence effect outcomes.");
    Task<ApiResult<CollectionModel<RuleActionExecutionDto>>> SearchRuleActions(RuleActionAuditQueryDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<RuleFeatureExecutionDto>> StartExecution(Guid gameServerId, StartRuleFeatureExecutionDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<RuleFeatureExecutionDto>> GetExecution(Guid gameServerId, Guid executionId, CancellationToken cancellationToken = default);
    Task<ApiResult<RuleFeatureExecutionDto>> UpdateProgress(Guid gameServerId, Guid executionId, UpdateRuleFeatureExecutionProgressDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<RuleActionClaimResultDto>> ClaimAction(Guid gameServerId, Guid executionId, ClaimRuleActionDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<EnsureAutomatedActionResultDto>> EnsureRuleActionAdminAction(Guid gameServerId, Guid executionId, string actionId, EnsureRuleActionAdminActionDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<RuleActionClaimResultDto>> UpdateActionOutcome(Guid gameServerId, Guid executionId, string actionId, UpdateRuleActionOutcomeDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<RuleFactSnapshotDto>> CreateFactSnapshot(Guid gameServerId, RuleFactSnapshotRequestDto request, CancellationToken cancellationToken = default);
}
