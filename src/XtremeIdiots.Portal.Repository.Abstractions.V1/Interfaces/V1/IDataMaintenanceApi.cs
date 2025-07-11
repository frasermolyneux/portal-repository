using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IDataMaintenanceApi
{
    Task<ApiResult> PruneChatMessages();
    Task<ApiResult> PruneGameServerEvents();
    Task<ApiResult> PruneGameServerStats();
    Task<ApiResult> PruneRecentPlayers();
    Task<ApiResult> ResetSystemAssignedPlayerTags();
}