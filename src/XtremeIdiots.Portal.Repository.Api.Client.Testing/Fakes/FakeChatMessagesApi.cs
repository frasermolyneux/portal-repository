using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ChatMessages;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeChatMessagesApi : IChatMessagesApi
{
    private readonly ConcurrentDictionary<Guid, ChatMessageDto> _chatMessages = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);

    public FakeChatMessagesApi AddChatMessage(ChatMessageDto chatMessage) { _chatMessages[chatMessage.ChatMessageId] = chatMessage; return this; }
    public FakeChatMessagesApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeChatMessagesApi Reset() { _chatMessages.Clear(); _errorResponses.Clear(); return this; }

    public Task<ApiResult<ChatMessageDto>> GetChatMessage(Guid chatMessageId, CancellationToken cancellationToken = default)
    {
        if (_chatMessages.TryGetValue(chatMessageId, out var cm))
            return Task.FromResult(new ApiResult<ChatMessageDto>(HttpStatusCode.OK, new ApiResponse<ChatMessageDto>(cm)));
        return Task.FromResult(new ApiResult<ChatMessageDto>(HttpStatusCode.NotFound, new ApiResponse<ChatMessageDto>(new ApiError("NOT_FOUND", "Chat message not found"))));
    }

    public Task<ApiResult<CollectionModel<ChatMessageDto>>> GetChatMessages(GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, int skipEntries, int takeEntries, ChatMessageOrder? order, bool? lockedOnly = null, CancellationToken cancellationToken = default)
    {
        var items = _chatMessages.Values.AsEnumerable();
        if (gameServerId.HasValue) items = items.Where(c => c.GameServerId == gameServerId.Value);
        if (playerId.HasValue) items = items.Where(c => c.PlayerId == playerId.Value);
        if (lockedOnly == true) items = items.Where(c => c.Locked);
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<ChatMessageDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<ChatMessageDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<ChatMessageDto>>(collection)));
    }

    public Task<ApiResult> CreateChatMessage(CreateChatMessageDto createChatMessageDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> CreateChatMessages(List<CreateChatMessageDto> createChatMessageDtos, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> SetLock(Guid chatMessageId, bool locked, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
