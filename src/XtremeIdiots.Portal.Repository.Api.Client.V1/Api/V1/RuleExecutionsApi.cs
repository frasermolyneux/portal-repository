using Microsoft.Extensions.Logging;
using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
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
    public Task<ApiResult<RuleFeatureExecutionDto[]>> GetConnectionExecutions(
        Guid gameServerId, Guid connectionId, CancellationToken cancellationToken = default) =>
        Send<RuleFeatureExecutionDto[]>($"v1/game-servers/{gameServerId}/rule-connections/{connectionId}/executions", Method.Get, null, cancellationToken);

    public Task<ApiResult<RulePreparedActionDto>> PrepareAction(
        Guid gameServerId, Guid executionId, PrepareRuleActionDto request, CancellationToken cancellationToken = default) =>
        Send<RulePreparedActionDto>($"v1/game-servers/{gameServerId}/rule-executions/{executionId}/prepared-actions", Method.Post, request, cancellationToken);

    public Task<ApiResult<RuleTaggedAudienceDto>> GetTaggedAudience(
        Guid gameServerId, RuleTaggedAudienceRequestDto request, CancellationToken cancellationToken = default) =>
        Send<RuleTaggedAudienceDto>($"v1/game-servers/{gameServerId}/rule-tagged-audience", Method.Post, request, cancellationToken);

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
