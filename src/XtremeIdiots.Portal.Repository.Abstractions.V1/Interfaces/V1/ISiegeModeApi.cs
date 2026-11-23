using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.SiegeMode;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface ISiegeModeApi
{
    Task<ApiResult<SiegeSessionDto>> StartSession(Guid gameServerId, StartSiegeSessionDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<SiegeSessionDto>> GetCurrentSession(Guid gameServerId, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<SiegeSessionDto>>> GetSessions(Guid gameServerId, int skipEntries, int takeEntries, CancellationToken cancellationToken = default);
    Task<ApiResult<SiegeSessionDto>> EndSession(Guid gameServerId, Guid sessionId, ChangeSiegeSessionDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<SiegeSessionDto>> ExtendSession(Guid gameServerId, Guid sessionId, ChangeSiegeSessionDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<SiegeBaselineDto>> GetBaseline(Guid gameServerId, Guid sessionId, SiegeBaselineRequestDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<SiegeActionClaimDto>> ClaimAction(Guid gameServerId, Guid sessionId, ClaimSiegeActionDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<SiegeActionDto>> UpdateActionOutcome(Guid gameServerId, Guid sessionId, Guid actionId, UpdateSiegeActionOutcomeDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<SiegeActionDto>>> GetActions(Guid gameServerId, Guid sessionId, int skipEntries, int takeEntries, CancellationToken cancellationToken = default);
    Task<ApiResult<SiegeCheckDto>> RecordCheck(Guid gameServerId, Guid sessionId, RecordSiegeCheckDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<SiegeCheckDto>>> GetChecks(Guid gameServerId, Guid sessionId, int skipEntries, int takeEntries, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<SiegeSessionEventDto>>> GetSessionEvents(Guid gameServerId, Guid sessionId, int skipEntries, int takeEntries, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<SiegeSessionDto>>> SearchSessions(SiegeAuditQueryDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<SiegeActionDto>>> SearchActions(SiegeAuditQueryDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<SiegeActionSummaryDto>> SummarizeActions(SiegeAuditQueryDto request, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<SiegeCheckDto>>> SearchChecks(SiegeAuditQueryDto request, CancellationToken cancellationToken = default);
}
