using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IRuleConnectionBindingsApi
{
    Task<ApiResult<RuleConnectionStatusSnapshotResultDto>> ObserveSnapshot(
        Guid gameServerId,
        RuleConnectionStatusSnapshotDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResult<RuleConnectionBindingDto>> GetCurrentBinding(
        Guid gameServerId,
        int slotId,
        string playerGuid,
        CancellationToken cancellationToken = default);

    Task<ApiResult<RuleServerCapabilitiesDto>> GetCurrentCapabilities(
        Guid gameServerId,
        CancellationToken cancellationToken = default);
}
