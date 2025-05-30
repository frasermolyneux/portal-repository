﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.ChatMessages;

namespace XtremeIdiots.Portal.RepositoryApiClient.Api
{
    public class ChatMessagesApi : BaseApi, IChatMessagesApi
    {
        public ChatMessagesApi(ILogger<ChatMessagesApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {

        }

        public async Task<ApiResponseDto<ChatMessageDto>> GetChatMessage(Guid chatMessageId)
        {
            var request = await CreateRequestAsync($"chat-messages/{chatMessageId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<ChatMessageDto>();
        }

        public async Task<ApiResponseDto<ChatMessagesCollectionDto>> GetChatMessages(GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, int skipEntries, int takeEntries, ChatMessageOrder? order, bool? lockedOnly = null)
        {
            var request = await CreateRequestAsync("chat-messages", Method.Get);

            if (gameType.HasValue)
                request.AddQueryParameter("gameType", gameType.ToString());

            if (gameServerId.HasValue)
                request.AddQueryParameter("gameServerId", gameServerId.ToString());

            if (playerId.HasValue)
                request.AddQueryParameter("playerId", playerId.ToString());

            if (!string.IsNullOrWhiteSpace(filterString))
                request.AddQueryParameter("filterString", filterString);

            if (lockedOnly.HasValue)
                request.AddQueryParameter("lockedOnly", lockedOnly.ToString());

            request.AddQueryParameter("takeEntries", takeEntries.ToString());
            request.AddQueryParameter("skipEntries", skipEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<ChatMessagesCollectionDto>();
        }

        public async Task<ApiResponseDto> CreateChatMessage(CreateChatMessageDto createChatMessageDto)
        {
            var request = await CreateRequestAsync("chat-messages", Method.Post);
            request.AddJsonBody(new List<CreateChatMessageDto> { createChatMessageDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> CreateChatMessages(List<CreateChatMessageDto> createChatMessageDtos)
        {
            var request = await CreateRequestAsync("chat-messages", Method.Post);
            request.AddJsonBody(createChatMessageDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> ToggleLockedStatus(Guid chatMessageId)
        {
            var request = await CreateRequestAsync($"chat-messages/{chatMessageId}/toggle-lock", Method.Post);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}