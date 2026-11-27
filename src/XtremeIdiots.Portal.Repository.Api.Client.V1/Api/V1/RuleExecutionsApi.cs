using System.Globalization;
using Microsoft.Extensions.Logging;
using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1;

public sealed class RuleExecutionsApi(
    ILogger<BaseApi<RepositoryApiClientOptions>> logger,
    IApiTokenProvider apiTokenProvider,
    IRestClientService restClientService,
    RepositoryApiClientOptions options)
    : BaseApi<RepositoryApiClientOptions>(logger, apiTokenProvider, restClientService, options), IRuleExecutionsApi
{
    public Task<ApiResult<RuleChatEvaluationDto>> CreateChatEvaluation(
        Guid gameServerId, CreateRuleChatEvaluationDto request, CancellationToken cancellationToken = default) =>
        Send<RuleChatEvaluationDto>($"v1/game-servers/{gameServerId}/chat-evaluations", Method.Post, request, cancellationToken);

    public async Task<ApiResult<HistoryPageDto<RuleChatEvaluationDto>>> SearchChatEvaluations(
        RuleChatEvaluationQueryDto request, CancellationToken cancellationToken = default)
    {
        var httpRequest = await CreateRequestAsync("v1/chat-evaluations", Method.Get, cancellationToken).ConfigureAwait(false);
        if (request.GameServerId is { } serverId) { httpRequest.AddQueryParameter("gameServerId", serverId.ToString()); }
        if (request.PlayerId is { } playerId) { httpRequest.AddQueryParameter("playerId", playerId.ToString()); }
        if (request.OccurrenceId is { } occurrenceId) { httpRequest.AddQueryParameter("occurrenceId", occurrenceId.ToString()); }
        if (request.FeatureId is { } featureId) { httpRequest.AddQueryParameter("featureId", featureId); }
        if (request.FromUtc is { } from) { httpRequest.AddQueryParameter("fromUtc", from.ToString("O", CultureInfo.InvariantCulture)); }
        if (request.ToUtc is { } to) { httpRequest.AddQueryParameter("toUtc", to.ToString("O", CultureInfo.InvariantCulture)); }
        if (request.BeforeRecordedAtUtc is { } before) { httpRequest.AddQueryParameter("beforeRecordedAtUtc", before.ToString("O", CultureInfo.InvariantCulture)); }
        if (request.AfterAttemptId is { } after) { httpRequest.AddQueryParameter("afterAttemptId", after.ToString()); }
        httpRequest.AddQueryParameter("skipEntries", request.SkipEntries.ToString(CultureInfo.InvariantCulture));
        httpRequest.AddQueryParameter("takeEntries", request.TakeEntries.ToString(CultureInfo.InvariantCulture));
        return (await ExecuteAsync(httpRequest, cancellationToken).ConfigureAwait(false)).ToApiResult<HistoryPageDto<RuleChatEvaluationDto>>();
    }

    public async Task<ApiResult<HistoryPageDto<RuleOccurrenceEffectDto>>> GetOccurrenceEffects(
        Guid gameServerId, Guid occurrenceId, int skipEntries = 0, int takeEntries = 50, CancellationToken cancellationToken = default)
    {
        var httpRequest = await CreateRequestAsync($"v1/game-servers/{gameServerId}/chat-occurrences/{occurrenceId}/effects", Method.Get, cancellationToken).ConfigureAwait(false);
        httpRequest.AddQueryParameter("skipEntries", skipEntries.ToString(CultureInfo.InvariantCulture));
        httpRequest.AddQueryParameter("takeEntries", takeEntries.ToString(CultureInfo.InvariantCulture));
        return (await ExecuteAsync(httpRequest, cancellationToken).ConfigureAwait(false)).ToApiResult<HistoryPageDto<RuleOccurrenceEffectDto>>();
    }

    public Task<ApiResult<RuleExecutionFlowDto>> GetRuleExecutionFlow(
        Guid executionId, CancellationToken cancellationToken = default) =>
        Send<RuleExecutionFlowDto>($"v1/rule-executions/{executionId}/flow", Method.Get, null, cancellationToken);

    public async Task<ApiResult<CollectionModel<RuleExecutionAuditDto>>> SearchRuleExecutionAudit(
        RuleExecutionAuditQueryDto request, CancellationToken cancellationToken = default)
    {
        var httpRequest = await CreateRequestAsync("v1/rule-executions/audit", Method.Get, cancellationToken).ConfigureAwait(false);
        if (request.GameServerId is { } serverId)
        {
            httpRequest.AddQueryParameter("gameServerId", serverId.ToString());
        }
        if (request.FeatureId is { } featureId)
        {
            httpRequest.AddQueryParameter("featureId", featureId);
        }
        if (request.MatchStatus is { } matchStatus)
        {
            httpRequest.AddQueryParameter("matchStatus", matchStatus.ToString());
        }
        if (request.TriggerOccurrenceId is { } occurrenceId)
        {
            httpRequest.AddQueryParameter("triggerOccurrenceId", occurrenceId.ToString());
        }
        httpRequest.AddQueryParameter("skipEntries", request.SkipEntries.ToString(CultureInfo.InvariantCulture));
        httpRequest.AddQueryParameter("takeEntries", request.TakeEntries.ToString(CultureInfo.InvariantCulture));
        if (request.FromUtc is { } from)
        {
            httpRequest.AddQueryParameter("fromUtc", from.ToString("O", CultureInfo.InvariantCulture));
        }
        if (request.ToUtc is { } to)
        {
            httpRequest.AddQueryParameter("toUtc", to.ToString("O", CultureInfo.InvariantCulture));
        }
        var response = await ExecuteAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        return response.ToApiResult<CollectionModel<RuleExecutionAuditDto>>();
    }

    public async Task<ApiResult<CollectionModel<RuleFeatureExecutionDto>>> SearchRuleExecutions(
        RuleFeatureExecutionQueryDto request, CancellationToken cancellationToken = default)
    {
        var httpRequest = await CreateRequestAsync($"v1/game-servers/{request.GameServerId}/rule-executions", Method.Get, cancellationToken).ConfigureAwait(false);
        httpRequest.AddQueryParameter("featureId", request.FeatureId);
        httpRequest.AddQueryParameter("skipEntries", request.SkipEntries.ToString(CultureInfo.InvariantCulture));
        httpRequest.AddQueryParameter("takeEntries", request.TakeEntries.ToString(CultureInfo.InvariantCulture));
        if (request.FromUtc is { } from)
        {
            httpRequest.AddQueryParameter("fromUtc", from.ToString("O", CultureInfo.InvariantCulture));
        }
        if (request.ToUtc is { } to)
        {
            httpRequest.AddQueryParameter("toUtc", to.ToString("O", CultureInfo.InvariantCulture));
        }
        var response = await ExecuteAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        return response.ToApiResult<CollectionModel<RuleFeatureExecutionDto>>();
    }

    public Task<ApiResult<RuleFeatureExecutionDto[]>> GetConnectionExecutions(
        Guid gameServerId, Guid connectionId, CancellationToken cancellationToken = default) =>
        Send<RuleFeatureExecutionDto[]>($"v1/game-servers/{gameServerId}/rule-connections/{connectionId}/executions", Method.Get, null, cancellationToken);

    public Task<ApiResult<RulePreparedActionDto>> PrepareAction(
        Guid gameServerId, Guid executionId, PrepareRuleActionDto request, CancellationToken cancellationToken = default) =>
        Send<RulePreparedActionDto>($"v1/game-servers/{gameServerId}/rule-executions/{executionId}/prepared-actions", Method.Post, request, cancellationToken);

    public Task<ApiResult<RuleTaggedAudienceDto>> GetTaggedAudience(
        Guid gameServerId, RuleTaggedAudienceRequestDto request, CancellationToken cancellationToken = default) =>
        Send<RuleTaggedAudienceDto>($"v1/game-servers/{gameServerId}/rule-tagged-audience", Method.Post, request, cancellationToken);

    public Task<ApiResult<ClaimRuleOccurrenceEffectResultDto>> ClaimOccurrenceEffect(
        Guid gameServerId, Guid occurrenceId, ClaimRuleOccurrenceEffectDto request, CancellationToken cancellationToken = default) =>
        Send<ClaimRuleOccurrenceEffectResultDto>($"v1/game-servers/{gameServerId}/chat-occurrences/{occurrenceId}/effects", Method.Post, request, cancellationToken);

    public Task<ApiResult<EnsureAutomatedActionResultDto>> EnsureOccurrenceAdminAction(
        Guid gameServerId, Guid occurrenceId, string logicalEffectId, EnsureRuleOccurrenceAdminActionDto request, CancellationToken cancellationToken = default) =>
        Send<EnsureAutomatedActionResultDto>($"v1/game-servers/{gameServerId}/chat-occurrences/{occurrenceId}/effects/{Uri.EscapeDataString(logicalEffectId)}/admin-action", Method.Put, request, cancellationToken);

    public Task<ApiResult<RuleOccurrenceEffectDto>> UpdateOccurrenceEffectOutcome(
        Guid gameServerId, Guid occurrenceId, string logicalEffectId, UpdateRuleOccurrenceEffectOutcomeDto request, CancellationToken cancellationToken = default) =>
        Send<RuleOccurrenceEffectDto>($"v1/game-servers/{gameServerId}/chat-occurrences/{occurrenceId}/effects/{Uri.EscapeDataString(logicalEffectId)}/outcome", Method.Put, request, cancellationToken);

    public Task<ApiResult<CollectionModel<RuleActionExecutionDto>>> SearchRuleActions(
        RuleActionAuditQueryDto request,
        CancellationToken cancellationToken = default) =>
        Send<CollectionModel<RuleActionExecutionDto>>("v1/rule-actions/search", Method.Post, request, cancellationToken);

    public Task<ApiResult<RuleFeatureExecutionDto>> StartExecution(
        Guid gameServerId,
        StartRuleFeatureExecutionDto request,
        CancellationToken cancellationToken = default) =>
        Send<RuleFeatureExecutionDto>($"v1/game-servers/{gameServerId}/rule-executions", Method.Post, request, cancellationToken);

    public Task<ApiResult<RuleFeatureExecutionDto>> GetExecution(
        Guid gameServerId,
        Guid executionId,
        CancellationToken cancellationToken = default) =>
        Send<RuleFeatureExecutionDto>($"v1/game-servers/{gameServerId}/rule-executions/{executionId}", Method.Get, null, cancellationToken);

    public Task<ApiResult<RuleFeatureExecutionDto>> UpdateProgress(
        Guid gameServerId,
        Guid executionId,
        UpdateRuleFeatureExecutionProgressDto request,
        CancellationToken cancellationToken = default) =>
        Send<RuleFeatureExecutionDto>($"v1/game-servers/{gameServerId}/rule-executions/{executionId}/progress", Method.Put, request, cancellationToken);

    public Task<ApiResult<RuleActionClaimResultDto>> ClaimAction(
        Guid gameServerId,
        Guid executionId,
        ClaimRuleActionDto request,
        CancellationToken cancellationToken = default) =>
        Send<RuleActionClaimResultDto>($"v1/game-servers/{gameServerId}/rule-executions/{executionId}/actions", Method.Post, request, cancellationToken);

    public Task<ApiResult<EnsureAutomatedActionResultDto>> EnsureRuleActionAdminAction(
        Guid gameServerId,
        Guid executionId,
        string actionId,
        EnsureRuleActionAdminActionDto request,
        CancellationToken cancellationToken = default) =>
        Send<EnsureAutomatedActionResultDto>(
            $"v1/game-servers/{gameServerId}/rule-executions/{executionId}/actions/{Uri.EscapeDataString(actionId)}/admin-action",
            Method.Post,
            request,
            cancellationToken);

    public Task<ApiResult<RuleActionClaimResultDto>> UpdateActionOutcome(
        Guid gameServerId,
        Guid executionId,
        string actionId,
        UpdateRuleActionOutcomeDto request,
        CancellationToken cancellationToken = default) =>
        Send<RuleActionClaimResultDto>(
            $"v1/game-servers/{gameServerId}/rule-executions/{executionId}/actions/{Uri.EscapeDataString(actionId)}/outcome",
            Method.Put,
            request,
            cancellationToken);

    public Task<ApiResult<RuleFactSnapshotDto>> CreateFactSnapshot(
        Guid gameServerId,
        RuleFactSnapshotRequestDto request,
        CancellationToken cancellationToken = default) =>
        Send<RuleFactSnapshotDto>($"v1/game-servers/{gameServerId}/rule-fact-snapshots", Method.Post, request, cancellationToken);

    private async Task<ApiResult<T>> Send<T>(string path, Method method, object? body, CancellationToken cancellationToken)
    {
        var request = await CreateRequestAsync(path, method, cancellationToken).ConfigureAwait(false);
        if (body is not null)
        {
            request.AddJsonBody(body);
        }
        var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        return response.ToApiResult<T>();
    }
}
