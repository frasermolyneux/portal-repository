using MxIO.ApiClient.Abstractions;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.ChatMessages;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces
{
    public interface IChatMessagesApi
    {
        Task<ApiResponseDto<ChatMessageDto>> GetChatMessage(Guid chatMessageId);
        Task<ApiResponseDto<ChatMessagesCollectionDto>> GetChatMessages(GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, int skipEntries, int takeEntries, ChatMessageOrder? order, bool? lockedOnly = null);

        Task<ApiResponseDto> CreateChatMessage(CreateChatMessageDto createChatMessageDto);
        Task<ApiResponseDto> CreateChatMessages(List<CreateChatMessageDto> createChatMessageDtos);
        Task<ApiResponseDto> ToggleLockedStatus(Guid chatMessageId);
    }
}