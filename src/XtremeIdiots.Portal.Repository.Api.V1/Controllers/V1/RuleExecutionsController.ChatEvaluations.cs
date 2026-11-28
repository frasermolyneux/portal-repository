using System.Net;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public sealed partial class RuleExecutionsController
{
    [HttpPost("game-servers/{gameServerId:guid}/chat-evaluations")]
    [ValidateChatEvaluationJson]
    [ProducesResponseType<ApiResponse<RuleChatEvaluationDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<RuleChatEvaluationDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateChatEvaluation(Guid gameServerId, CreateRuleChatEvaluationDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).CreateChatEvaluation(gameServerId, request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    [HttpGet("chat-evaluations")]
    [ProducesResponseType<ApiResponse<HistoryPageDto<RuleChatEvaluationDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchChatEvaluations([FromQuery] RuleChatEvaluationQueryDto request, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).SearchChatEvaluations(request, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    [HttpGet("game-servers/{gameServerId:guid}/chat-occurrences/{occurrenceId:guid}/effects")]
    [ProducesResponseType<ApiResponse<HistoryPageDto<RuleOccurrenceEffectDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOccurrenceEffects(Guid gameServerId, Guid occurrenceId, int skipEntries = 0, int takeEntries = 50, CancellationToken cancellationToken = default) =>
        (await ((IRuleExecutionsApi)this).GetOccurrenceEffects(gameServerId, occurrenceId, skipEntries, takeEntries, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    async Task<ApiResult<RuleChatEvaluationDto>> IRuleExecutionsApi.CreateChatEvaluation(Guid gameServerId, CreateRuleChatEvaluationDto request, CancellationToken cancellationToken)
    {
        if (gameServerId == Guid.Empty || !ValidChatEvaluation(request))
        {
            return new ApiResult<RuleChatEvaluationDto>(HttpStatusCode.BadRequest);
        }
        var gameType = await GetOccurrenceServerGameTypeAsync(gameServerId, cancellationToken).ConfigureAwait(false);
        if (gameType is null)
        {
            return new ApiResult<RuleChatEvaluationDto>(HttpStatusCode.NotFound);
        }
        var evidenceJson = JsonSerializer.Serialize(request.Evidence, JsonOptions);
        var detailsJson = JsonSerializer.Serialize(request.CheckDetails, JsonOptions);
        return await ExecuteMutationAsync(async () =>
        {
            await AcquireGameHistoryReadLockAsync(gameType.Value, cancellationToken).ConfigureAwait(false);
            if (!await context.GameServers.AnyAsync(server => server.GameServerId == gameServerId &&
                    !server.Deleted && server.GameType == gameType.Value, cancellationToken).ConfigureAwait(false) ||
                !await context.Players.AnyAsync(player => player.PlayerId == request.PlayerId &&
                    player.GameType == gameType.Value, cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleChatEvaluationDto>(HttpStatusCode.NotFound);
            }
            if (!await AcquireTransactionLockAsync($"rules-chat-evaluation:{request.AttemptId:N}", cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<RuleChatEvaluationDto>(HttpStatusCode.Conflict);
            }
            var existing = await context.RuleChatEvaluations.SingleOrDefaultAsync(item => item.AttemptId == request.AttemptId, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                if (existing.GameServerId != gameServerId || existing.PlayerId != request.PlayerId ||
                    existing.OccurrenceId != request.OccurrenceId || existing.FeatureId != request.FeatureId ||
                    existing.RuleSetRevision != request.RuleSetRevision || existing.Mode != request.Mode.ToString() ||
                    existing.MatchStatus != request.MatchStatus.ToString() || existing.EvaluatedAtUtc != request.EvaluatedAtUtc.UtcDateTime ||
                    existing.EvidenceJson != evidenceJson || existing.CheckDetailsJson != detailsJson)
                {
                    return new ApiResult<RuleChatEvaluationDto>(HttpStatusCode.Conflict);
                }
                return new ApiResponse<RuleChatEvaluationDto>(ToChatEvaluationDto(existing)).ToApiResult();
            }
            var item = new RuleChatEvaluation
            {
                AttemptId = request.AttemptId,
                GameServerId = gameServerId,
                PlayerId = request.PlayerId,
                OccurrenceId = request.OccurrenceId,
                FeatureId = request.FeatureId,
                RuleSetRevision = request.RuleSetRevision,
                Mode = request.Mode.ToString(),
                MatchStatus = request.MatchStatus.ToString(),
                EvaluatedAtUtc = request.EvaluatedAtUtc.UtcDateTime,
                RecordedAtUtc = DateTime.UtcNow,
                EvidenceJson = evidenceJson,
                CheckDetailsJson = detailsJson
            };
            context.RuleChatEvaluations.Add(item);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse<RuleChatEvaluationDto>(ToChatEvaluationDto(item)).ToApiResult(HttpStatusCode.Created);
        }, cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<HistoryPageDto<RuleChatEvaluationDto>>> IRuleExecutionsApi.SearchChatEvaluations(RuleChatEvaluationQueryDto request, CancellationToken cancellationToken)
    {
        if (request is null || request.GameServerId == Guid.Empty || request.PlayerId == Guid.Empty ||
            request.OccurrenceId == Guid.Empty || request.FeatureId is { } feature && !ValidChatCode(feature, 64) ||
            request.SkipEntries is < 0 or > 10000 || request.TakeEntries is <= 0 or > 100 ||
            request.FromUtc is { Kind: not DateTimeKind.Utc } || request.ToUtc is { Kind: not DateTimeKind.Utc } ||
            request.BeforeRecordedAtUtc is { Kind: not DateTimeKind.Utc } ||
            (request.BeforeRecordedAtUtc is null) != (request.AfterAttemptId is null) || request.AfterAttemptId == Guid.Empty ||
            request.FromUtc is { } from && request.ToUtc is { } to && from >= to)
        {
            return new ApiResult<HistoryPageDto<RuleChatEvaluationDto>>(HttpStatusCode.BadRequest);
        }
        var query = context.RuleChatEvaluations.AsNoTracking().AsQueryable();
        if (request.GameServerId is { } serverId) { query = query.Where(item => item.GameServerId == serverId); }
        if (request.PlayerId is { } playerId) { query = query.Where(item => item.PlayerId == playerId); }
        if (request.OccurrenceId is { } occurrenceId) { query = query.Where(item => item.OccurrenceId == occurrenceId); }
        if (request.FeatureId is { } featureId) { query = query.Where(item => item.FeatureId == featureId); }
        if (request.FromUtc is { } fromUtc) { query = query.Where(item => item.RecordedAtUtc >= fromUtc); }
        if (request.ToUtc is { } toUtc) { query = query.Where(item => item.RecordedAtUtc < toUtc); }
        if (request.BeforeRecordedAtUtc is { } before && request.AfterAttemptId is { } after)
        {
            query = query.Where(item => item.RecordedAtUtc < before ||
                item.RecordedAtUtc == before && item.AttemptId.CompareTo(after) > 0);
        }
        var rows = await query.OrderByDescending(item => item.RecordedAtUtc).ThenBy(item => item.AttemptId)
            .Skip(request.SkipEntries).Take(request.TakeEntries + 1).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return new ApiResponse<HistoryPageDto<RuleChatEvaluationDto>>(new HistoryPageDto<RuleChatEvaluationDto>(
            rows.Take(request.TakeEntries).Select(ToChatEvaluationDto).ToArray(), rows.Length > request.TakeEntries)).ToApiResult();
    }

    async Task<ApiResult<HistoryPageDto<RuleOccurrenceEffectDto>>> IRuleExecutionsApi.GetOccurrenceEffects(Guid gameServerId, Guid occurrenceId, int skipEntries, int takeEntries, CancellationToken cancellationToken)
    {
        if (gameServerId == Guid.Empty || occurrenceId == Guid.Empty || skipEntries is < 0 or > 10000 || takeEntries is <= 0 or > 100)
        {
            return new ApiResult<HistoryPageDto<RuleOccurrenceEffectDto>>(HttpStatusCode.BadRequest);
        }
        var query = context.RuleOccurrenceEffects.AsNoTracking().Where(item => item.GameServerId == gameServerId && item.OccurrenceId == occurrenceId);
        var rows = await query.OrderBy(item => item.LogicalEffectId).Skip(skipEntries).Take(takeEntries + 1).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return new ApiResponse<HistoryPageDto<RuleOccurrenceEffectDto>>(new HistoryPageDto<RuleOccurrenceEffectDto>(
            rows.Take(takeEntries).Select(ToOccurrenceEffectDto).ToArray(), rows.Length > takeEntries)).ToApiResult();
    }

    private static bool ValidChatEvaluation(CreateRuleChatEvaluationDto? request)
    {
        if (request is null || request.AttemptId == Guid.Empty || request.PlayerId == Guid.Empty || request.OccurrenceId == Guid.Empty ||
            !ValidChatCode(request.FeatureId, 64) || !ValidChatCode(request.RuleSetRevision, 128) ||
            request.Mode is not (RuleExecutionMode.Enforce or RuleExecutionMode.DryRun) ||
            !Enum.IsDefined(request.MatchStatus) || !ValidEvidenceTimestamp(request.EvaluatedAtUtc) ||
            request.Evidence is null || !ValidEvaluationEvidence(request.Evidence) ||
            request.CheckDetails is null || request.CheckDetails.Length > 64)
        {
            return false;
        }
        if (request.Evidence.Nodes.Any(node => !ValidChatIdentifier(node.RuleId) ||
                node.ConditionId is not null && !ValidChatIdentifier(node.ConditionId)) ||
            request.Evidence.ActionDecisions.Any(action => !ValidChatIdentifier(action.RuleId) || !ValidChatIdentifier(action.ActionId)))
        {
            return false;
        }
        var keys = new HashSet<(int, string?, string, RuleChatSafetyCategory?, string?)>();
        foreach (var detail in request.CheckDetails)
        {
            if (detail is null || !ValidChatCode(detail.CheckId, 128) ||
                !request.Evidence.Nodes.Any(node => node.Kind == RuleEvidenceNodeKind.Check &&
                    node.RuleIndex == detail.RuleIndex && node.CheckId == detail.CheckId &&
                    (node.ConditionPath is null ? detail.ConditionPath is null : detail.ConditionPath is not null && node.ConditionPath.SequenceEqual(detail.ConditionPath))) ||
                !keys.Add((detail.RuleIndex, detail.ConditionPath is null ? null : string.Join(',', detail.ConditionPath), detail.CheckId, detail.Category, detail.MatchedEntryId)))
            {
                return false;
            }
            if (detail.Category is { } category)
            {
                if (!Enum.IsDefined(category) || category == RuleChatSafetyCategory.Unrecognized ||
                    detail.ObservedSeverity is < 0 or > 7 || detail.Threshold is < -1 or > 6 ||
                    detail.MatcherVersion is not null || detail.MatchedEntryId is not null) { return false; }
            }
            else if (detail.ObservedSeverity is not null || detail.Threshold is not null ||
                     !ValidChatCode(detail.MatcherVersion, 128) ||
                     detail.MatchedEntryId is not null && !ValidChatIdentifier(detail.MatchedEntryId)) { return false; }
        }
        return true;
    }

    private static bool ValidChatCode(string? value, int maxLength) =>
        value is not null && ValidEvidenceMetadata(value, maxLength);

    private static bool ValidChatIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value != value.Trim())
        {
            return false;
        }
        try
        {
            _ = value.Normalize(NormalizationForm.FormKC);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static RuleChatEvaluationDto ToChatEvaluationDto(RuleChatEvaluation item) => new()
    {
        GameServerId = item.GameServerId,
        RecordedAtUtc = Utc(item.RecordedAtUtc),
        Evaluation = new CreateRuleChatEvaluationDto
        {
            AttemptId = item.AttemptId,
            PlayerId = item.PlayerId,
            OccurrenceId = item.OccurrenceId,
            FeatureId = item.FeatureId,
            RuleSetRevision = item.RuleSetRevision,
            Mode = Enum.Parse<RuleExecutionMode>(item.Mode),
            MatchStatus = Enum.Parse<RuleExecutionMatchStatus>(item.MatchStatus),
            EvaluatedAtUtc = Utc(item.EvaluatedAtUtc),
            Evidence = Deserialize<RuleEvaluationEvidence>(item.EvidenceJson),
            CheckDetails = Deserialize<RuleChatCheckDetail[]>(item.CheckDetailsJson)
        }
    };
}
