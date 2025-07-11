using MxIO.ApiClient.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IDataMaintenanceApi
{
    Task<ApiResponseDto> PruneChatMessages();
    Task<ApiResponseDto> PruneGameServerEvents();
    Task<ApiResponseDto> PruneGameServerStats();
    Task<ApiResponseDto> PruneRecentPlayers();
    Task<ApiResponseDto> ResetSystemAssignedPlayerTags();
}