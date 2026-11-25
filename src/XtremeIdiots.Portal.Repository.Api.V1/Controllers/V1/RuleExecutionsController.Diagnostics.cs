using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public sealed partial class RuleExecutionsController
{
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
