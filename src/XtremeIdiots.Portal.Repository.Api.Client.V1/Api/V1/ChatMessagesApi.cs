using Microsoft.Extensions.Logging;



using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ChatMessages;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class ChatMessagesApi : BaseApi<RepositoryApiClientOptions>, IChatMessagesApi
    {
        public ChatMessagesApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {

        }

        public async Task<ApiResult<ChatMessageDto>> GetChatMessage(Guid chatMessageId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/chat-messages/{chatMessageId}", Method.Get, cancellationToken).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<ChatMessageDto>();
        }

        public async Task<ApiResult<CollectionModel<ChatMessageDto>>> GetChatMessages(GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, int skipEntries, int takeEntries, ChatMessageOrder? order, bool? lockedOnly = null, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/chat-messages", Method.Get, cancellationToken).ConfigureAwait(false);

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

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<CollectionModel<ChatMessageDto>>();
        }

        public async Task<ApiResult> CreateChatMessage(CreateChatMessageDto createChatMessageDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/chat-messages", Method.Post, cancellationToken).ConfigureAwait(false);
            request.AddJsonBody(new List<CreateChatMessageDto> { createChatMessageDto });

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> CreateChatMessages(List<CreateChatMessageDto> createChatMessageDtos, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/chat-messages", Method.Post, cancellationToken).ConfigureAwait(false);
            request.AddJsonBody(createChatMessageDtos);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> SetLock(Guid chatMessageId, bool locked, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/chat-messages/{chatMessageId}/lock", Method.Patch, cancellationToken).ConfigureAwait(false);
            request.AddJsonBody(new { locked });
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }
    }
}


