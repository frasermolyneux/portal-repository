using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IAdminActionsApi
    {
        Task<ApiResponseDto<AdminActionDto>> GetAdminAction(Guid adminActionId);
        Task<ApiResponseDto<AdminActionCollectionDto>> GetAdminActions(GameType? gameType, Guid? playerId, string? adminId, AdminActionFilter? filter, int skipEntries, int takeEntries, AdminActionOrder? order);

        Task<ApiResponseDto> CreateAdminAction(CreateAdminActionDto createAdminActionDto);

        Task<ApiResponseDto> UpdateAdminAction(EditAdminActionDto editAdminActionDto);

        Task<ApiResponseDto> DeleteAdminAction(Guid adminActionId);
    }
}