using System.Net;
using Asp.Versioning;
using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.WebExtensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.DataLib;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants.V1;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.ChatMessages;
using XtremeIdiots.Portal.RepositoryWebApi.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}")]
public class ChatMessagesController : ControllerBase, IChatMessagesApi
{
    private readonly PortalDbContext context;
    private readonly IMapper mapper;

    public ChatMessagesController(
        PortalDbContext context,
        IMapper mapper)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    [HttpGet]
    [Route("chat-messages/{chatMessageId}")]
    public async Task<IActionResult> GetChatMessage(Guid chatMessageId)
    {
        var response = await ((IChatMessagesApi)this).GetChatMessage(chatMessageId);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<ChatMessageDto>> IChatMessagesApi.GetChatMessage(Guid chatMessageId)
    {
        var chatLog = await context.ChatMessages
            .Include(cl => cl.GameServer)
            .Include(cl => cl.Player)
            .SingleOrDefaultAsync(cl => cl.ChatMessageId == chatMessageId);

        if (chatLog == null)
            return new ApiResponseDto<ChatMessageDto>(HttpStatusCode.NotFound);

        var result = mapper.Map<ChatMessageDto>(chatLog);

        return new ApiResponseDto<ChatMessageDto>(HttpStatusCode.OK, result);
    }

    [HttpGet]
    [Route("chat-messages")]
    public async Task<IActionResult> GetChatMessages(GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, int? skipEntries, int? takeEntries, ChatMessageOrder? order, bool? lockedOnly = null)
    {
        if (!skipEntries.HasValue)
            skipEntries = 0;

        if (!takeEntries.HasValue)
            takeEntries = 20;

        var response = await ((IChatMessagesApi)this).GetChatMessages(gameType, gameServerId, playerId, filterString, skipEntries.Value, takeEntries.Value, order, lockedOnly);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto<ChatMessagesCollectionDto>> IChatMessagesApi.GetChatMessages(GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, int skipEntries, int takeEntries, ChatMessageOrder? order, bool? lockedOnly = null)
    {
        var query = context.ChatMessages.Include(cl => cl.GameServer).Include(cl => cl.Player).AsQueryable();
        query = ApplyFilter(query, gameType, gameServerId, playerId, string.Empty, lockedOnly);
        var totalCount = await query.CountAsync();

        query = ApplyFilter(query, gameType, gameServerId, playerId, filterString, lockedOnly);
        var filteredCount = await query.CountAsync();

        query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
        var results = await query.ToListAsync();

        var entries = results.Select(cm => mapper.Map<ChatMessageDto>(cm)).ToList();

        var result = new ChatMessagesCollectionDto
        {
            TotalRecords = totalCount,
            FilteredRecords = filteredCount,
            Entries = entries
        };

        return new ApiResponseDto<ChatMessagesCollectionDto>(HttpStatusCode.OK, result);
    }

    Task<ApiResponseDto> IChatMessagesApi.CreateChatMessage(CreateChatMessageDto createChatMessageDto)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    [Route("chat-messages")]
    public async Task<IActionResult> CreateChatMessages()
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

        List<CreateChatMessageDto>? createChatMessageDtos;
        try
        {
            createChatMessageDtos = JsonConvert.DeserializeObject<List<CreateChatMessageDto>>(requestBody);
        }
        catch
        {
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Could not deserialize request body" }).ToHttpResult();
        }

        if (createChatMessageDtos == null || !createChatMessageDtos.Any())
            return new ApiResponseDto(HttpStatusCode.BadRequest, new List<string> { "Request body was null or did not contain any entries" }).ToHttpResult();

        var response = await ((IChatMessagesApi)this).CreateChatMessages(createChatMessageDtos);

        return response.ToHttpResult();
    }

    [HttpPost]
    [Route("chat-messages/{chatMessageId}/toggle-lock")]
    public async Task<IActionResult> ToggleLockedStatus(Guid chatMessageId)
    {
        var response = await ((IChatMessagesApi)this).ToggleLockedStatus(chatMessageId);

        return response.ToHttpResult();
    }

    async Task<ApiResponseDto> IChatMessagesApi.ToggleLockedStatus(Guid chatMessageId)
    {
        var chatMessage = await context.ChatMessages
            .SingleOrDefaultAsync(cm => cm.ChatMessageId == chatMessageId);

        if (chatMessage == null)
            return new ApiResponseDto(HttpStatusCode.NotFound);

        // Toggle the locked status
        chatMessage.Locked = !chatMessage.Locked;

        await context.SaveChangesAsync();

        return new ApiResponseDto(HttpStatusCode.OK);
    }

    async Task<ApiResponseDto> IChatMessagesApi.CreateChatMessages(List<CreateChatMessageDto> createChatMessageDtos)
    {
        var chatLogs = createChatMessageDtos.Select(cm => mapper.Map<ChatMessage>(cm)).ToList();

        await context.ChatMessages.AddRangeAsync(chatLogs);
        await context.SaveChangesAsync();

        return new ApiResponseDto(HttpStatusCode.OK);
    }

    private IQueryable<ChatMessage> ApplyFilter(IQueryable<ChatMessage> query, GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, bool? lockedOnly = null)
    {
        if (gameType.HasValue)
            query = query.Where(cl => cl.GameServer.GameType == gameType.Value.ToGameTypeInt()).AsQueryable();

        if (gameServerId.HasValue)
            query = query.Where(cl => cl.GameServerId == gameServerId).AsQueryable();

        if (playerId.HasValue)
            query = query.Where(cl => cl.PlayerId == playerId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filterString))
            query = query.Where(m => m.Message.Contains(filterString)).AsQueryable();

        if (lockedOnly.HasValue && lockedOnly.Value)
            query = query.Where(m => m.Locked).AsQueryable();

        return query;
    }

    private IQueryable<ChatMessage> ApplyOrderAndLimits(IQueryable<ChatMessage> query, int skipEntries, int takeEntries, ChatMessageOrder? order)
    {
        switch (order)
        {
            case ChatMessageOrder.TimestampAsc:
                query = query.OrderBy(cl => cl.Timestamp).AsQueryable();
                break;
            case ChatMessageOrder.TimestampDesc:
                query = query.OrderByDescending(cl => cl.Timestamp).AsQueryable();
                break;
        }

        query = query.Skip(skipEntries).AsQueryable();
        query = query.Take(takeEntries).AsQueryable();

        return query;
    }
}
