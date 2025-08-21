using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IDataMaintenanceApi
{
    Task<ApiResult> PruneChatMessages(CancellationToken cancellationToken = default);
    Task<ApiResult> PruneGameServerEvents(CancellationToken cancellationToken = default);
    Task<ApiResult> PruneGameServerStats(CancellationToken cancellationToken = default);
    Task<ApiResult> PruneRecentPlayers(CancellationToken cancellationToken = default);
    Task<ApiResult> ResetSystemAssignedPlayerTags(CancellationToken cancellationToken = default);
    Task<ApiResult> ValidateMapImages(CancellationToken cancellationToken = default);
}