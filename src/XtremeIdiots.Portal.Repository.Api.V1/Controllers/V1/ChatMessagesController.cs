using System.Net;
using Asp.Versioning;
using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ChatMessages;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

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
    public async Task<IActionResult> GetChatMessage(Guid chatMessageId, CancellationToken cancellationToken = default)
    {
        var response = await ((IChatMessagesApi)this).GetChatMessage(chatMessageId, cancellationToken);

        return response.ToHttpResult();
    }

    async Task<ApiResult<ChatMessageDto>> IChatMessagesApi.GetChatMessage(Guid chatMessageId, CancellationToken cancellationToken)
    {
        var chatLog = await context.ChatMessages
            .Include(cl => cl.GameServer)
            .Include(cl => cl.Player)
            .SingleOrDefaultAsync(cl => cl.ChatMessageId == chatMessageId, cancellationToken);

        if (chatLog == null)
            return new ApiResult<ChatMessageDto>(HttpStatusCode.NotFound);

        var result = mapper.Map<ChatMessageDto>(chatLog);

        return new ApiResult<ChatMessageDto>(HttpStatusCode.OK, new ApiResponse<ChatMessageDto>(result));
    }

    [HttpGet]
    [Route("chat-messages")]
    public async Task<IActionResult> GetChatMessages(GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, int? skipEntries, int? takeEntries, ChatMessageOrder? order, bool? lockedOnly = null, CancellationToken cancellationToken = default)
    {
        if (!skipEntries.HasValue)
            skipEntries = 0;

        if (!takeEntries.HasValue)
            takeEntries = 20;

        var response = await ((IChatMessagesApi)this).GetChatMessages(gameType, gameServerId, playerId, filterString, skipEntries.Value, takeEntries.Value, order, lockedOnly, cancellationToken);

        return response.ToHttpResult();
    }

    async Task<ApiResult<CollectionModel<ChatMessageDto>>> IChatMessagesApi.GetChatMessages(GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, int skipEntries, int takeEntries, ChatMessageOrder? order, bool? lockedOnly, CancellationToken cancellationToken)
    {
        var query = context.ChatMessages.Include(cl => cl.GameServer).Include(cl => cl.Player).AsQueryable();
        query = ApplyFilter(query, gameType, gameServerId, playerId, string.Empty, lockedOnly);
        var totalCount = await query.CountAsync(cancellationToken);

        query = ApplyFilter(query, gameType, gameServerId, playerId, filterString, lockedOnly);
        var filteredCount = await query.CountAsync(cancellationToken);

        query = ApplyOrderAndLimits(query, skipEntries, takeEntries, order);
        var results = await query.ToListAsync(cancellationToken);

        var entries = results.Select(cm => mapper.Map<ChatMessageDto>(cm)).ToList();

        var result = new CollectionModel<ChatMessageDto>
        {
            TotalCount = totalCount,
            FilteredCount = filteredCount,
            Items = entries
        };

        return new ApiResult<CollectionModel<ChatMessageDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<ChatMessageDto>>(result));
    }

    async Task<ApiResult> IChatMessagesApi.CreateChatMessage(CreateChatMessageDto createChatMessageDto, CancellationToken cancellationToken)
    {
        var chatMessage = mapper.Map<ChatMessage>(createChatMessageDto);

        await context.ChatMessages.AddAsync(chatMessage, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new ApiResponse().ToApiResult(HttpStatusCode.Created);
    }

    [HttpPost]
    [Route("chat-messages")]
    public async Task<IActionResult> CreateChatMessages(CancellationToken cancellationToken = default)
    {
        var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

        List<CreateChatMessageDto>? createChatMessageDtos;
        try
        {
            createChatMessageDtos = JsonConvert.DeserializeObject<List<CreateChatMessageDto>>(requestBody);
        }
        catch
        {
            return new ApiResponse().ToBadRequestResult().ToHttpResult();
        }

        if (createChatMessageDtos == null || !createChatMessageDtos.Any())
            return new ApiResponse().ToBadRequestResult().ToHttpResult();

        var response = await ((IChatMessagesApi)this).CreateChatMessages(createChatMessageDtos, cancellationToken);

        return response.ToHttpResult();
    }

    [HttpPost]
    [Route("chat-messages/{chatMessageId}/toggle-lock")]
    public async Task<IActionResult> ToggleLockedStatus(Guid chatMessageId, CancellationToken cancellationToken = default)
    {
        var response = await ((IChatMessagesApi)this).ToggleLockedStatus(chatMessageId, cancellationToken);

        return response.ToHttpResult();
    }

    async Task<ApiResult> IChatMessagesApi.ToggleLockedStatus(Guid chatMessageId, CancellationToken cancellationToken)
    {
        var chatMessage = await context.ChatMessages
            .SingleOrDefaultAsync(cm => cm.ChatMessageId == chatMessageId, cancellationToken);

        if (chatMessage == null)
            return new ApiResult(HttpStatusCode.NotFound);

        // Toggle the locked status
        chatMessage.Locked = !chatMessage.Locked;

        await context.SaveChangesAsync(cancellationToken);

        return new ApiResponse().ToApiResult();
    }

    async Task<ApiResult> IChatMessagesApi.CreateChatMessages(List<CreateChatMessageDto> createChatMessageDtos, CancellationToken cancellationToken)
    {
        var chatLogs = createChatMessageDtos.Select(cm => mapper.Map<ChatMessage>(cm)).ToList();

        await context.ChatMessages.AddRangeAsync(chatLogs, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new ApiResponse().ToApiResult();
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

