using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ChatMessages;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class ChatMessagesApi : BaseApi, IChatMessagesApi
    {
        public ChatMessagesApi(ILogger<ChatMessagesApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {

        }

        public async Task<ApiResponseDto<ChatMessageDto>> GetChatMessage(Guid chatMessageId)
        {
            var request = await CreateRequestAsync($"v1/chat-messages/{chatMessageId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<ChatMessageDto>();
        }

        public async Task<ApiResponseDto<ChatMessagesCollectionDto>> GetChatMessages(GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, int skipEntries, int takeEntries, ChatMessageOrder? order, bool? lockedOnly = null)
        {
            var request = await CreateRequestAsync("v1/chat-messages", Method.Get);

            if (gameType.HasValue)
                request.AddQueryParameter("gameType", gameType.ToString());

            if (gameServerId.HasValue)
                request.AddQueryParameter("gameServerId", gameServerId.ToString());

            if (playerId.HasValue)
                request.AddQueryParameter("playerId", playerId.ToString());

            if (!string.IsNullOrWhiteSpace(filterString))
                request.AddQueryParameter("filterString", filterString);

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            if (lockedOnly.HasValue)
                request.AddQueryParameter("lockedOnly", lockedOnly.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<ChatMessagesCollectionDto>();
        }

        public async Task<ApiResponseDto> CreateChatMessage(CreateChatMessageDto createChatMessageDto)
        {
            var request = await CreateRequestAsync("v1/chat-messages", Method.Post);
            request.AddJsonBody(new List<CreateChatMessageDto> { createChatMessageDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> CreateChatMessages(List<CreateChatMessageDto> createChatMessageDtos)
        {
            var request = await CreateRequestAsync("v1/chat-messages", Method.Post);
            request.AddJsonBody(createChatMessageDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> ToggleLockedStatus(Guid chatMessageId)
        {
            var request = await CreateRequestAsync($"v1/chat-messages/{chatMessageId}/toggle-locked", Method.Patch);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
