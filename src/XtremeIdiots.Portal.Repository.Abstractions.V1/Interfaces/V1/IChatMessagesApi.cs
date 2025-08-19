using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ChatMessages;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IChatMessagesApi
    {
        Task<ApiResult<ChatMessageDto>> GetChatMessage(Guid chatMessageId, CancellationToken cancellationToken = default);
        Task<ApiResult<CollectionModel<ChatMessageDto>>> GetChatMessages(GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, int skipEntries, int takeEntries, ChatMessageOrder? order, bool? lockedOnly = null, CancellationToken cancellationToken = default);

        Task<ApiResult> CreateChatMessage(CreateChatMessageDto createChatMessageDto, CancellationToken cancellationToken = default);
        Task<ApiResult> CreateChatMessages(List<CreateChatMessageDto> createChatMessageDtos, CancellationToken cancellationToken = default);
        Task<ApiResult> ToggleLockedStatus(Guid chatMessageId, CancellationToken cancellationToken = default);
    }
}