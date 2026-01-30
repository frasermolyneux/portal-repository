using System.Net;
using Asp.Versioning;

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
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

/// <summary>
/// Provides API endpoints for managing chat messages in the portal repository.
/// </summary>
[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}")]
public class ChatMessagesController : ControllerBase, IChatMessagesApi
{
    private readonly PortalDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessagesController"/> class.
    /// </summary>
    /// <param name="context">The database context for accessing chat messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when context parameter is null.</exception>
    public ChatMessagesController(PortalDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
            this.context = context;
    }

    /// <summary>
    /// Retrieves a specific chat message by its identifier.
    /// </summary>
    /// <param name="chatMessageId">The unique identifier of the chat message to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The chat message if found, otherwise a 404 Not Found response.</returns>
    [HttpGet("chat-messages/{chatMessageId:guid}")]
    [ProducesResponseType<ChatMessageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChatMessage(Guid chatMessageId, CancellationToken cancellationToken = default)
    {
        var response = await ((IChatMessagesApi)this).GetChatMessage(chatMessageId, cancellationToken);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves a specific chat message by its identifier.
    /// </summary>
    /// <param name="chatMessageId">The unique identifier of the chat message to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result containing the chat message if found.</returns>
    async Task<ApiResult<ChatMessageDto>> IChatMessagesApi.GetChatMessage(Guid chatMessageId, CancellationToken cancellationToken)
    {
        var chatLog = await context.ChatMessages
            .Include(cl => cl.GameServer)
            .Include(cl => cl.Player)
            .AsNoTracking()
            .FirstOrDefaultAsync(cl => cl.ChatMessageId == chatMessageId, cancellationToken);

        if (chatLog == null)
            return new ApiResult<ChatMessageDto>(HttpStatusCode.NotFound);

        var result = chatLog.ToDto();

        return new ApiResponse<ChatMessageDto>(result).ToApiResult();
    }

    /// <summary>
    /// Retrieves a paginated list of chat messages with optional filtering and ordering.
    /// </summary>
    /// <param name="gameType">Optional filter by game type.</param>
    /// <param name="gameServerId">Optional filter by game server identifier.</param>
    /// <param name="playerId">Optional filter by player identifier.</param>
    /// <param name="filterString">Optional filter string for message content.</param>
    /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
    /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
    /// <param name="order">Optional ordering for the results.</param>
    /// <param name="lockedOnly">Optional filter to show only locked messages.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A paginated collection of chat messages.</returns>
    [HttpGet("chat-messages")]
    [ProducesResponseType<CollectionModel<ChatMessageDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChatMessages(
        [FromQuery] GameType? gameType = null,
        [FromQuery] Guid? gameServerId = null,
        [FromQuery] Guid? playerId = null,
        [FromQuery] string? filterString = null,
        [FromQuery] int skipEntries = 0,
        [FromQuery] int takeEntries = 20,
        [FromQuery] ChatMessageOrder? order = null,
        [FromQuery] bool? lockedOnly = null,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IChatMessagesApi)this).GetChatMessages(gameType, gameServerId, playerId, filterString, skipEntries, takeEntries, order, lockedOnly, cancellationToken);

        return response.ToHttpResult();
    }

    /// <summary>
    /// Retrieves a paginated list of chat messages with optional filtering and ordering.
    /// </summary>
    /// <param name="gameType">Optional filter by game type.</param>
    /// <param name="gameServerId">Optional filter by game server identifier.</param>
    /// <param name="playerId">Optional filter by player identifier.</param>
    /// <param name="filterString">Optional filter string for message content.</param>
    /// <param name="skipEntries">Number of entries to skip for pagination.</param>
    /// <param name="takeEntries">Number of entries to take for pagination.</param>
    /// <param name="order">Optional ordering for the results.</param>
    /// <param name="lockedOnly">Optional filter to show only locked messages.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result containing a paginated collection of chat messages.</returns>
    async Task<ApiResult<CollectionModel<ChatMessageDto>>> IChatMessagesApi.GetChatMessages(GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, int skipEntries, int takeEntries, ChatMessageOrder? order, bool? lockedOnly, CancellationToken cancellationToken)
    {
        var baseQuery = context.ChatMessages
            .Include(cl => cl.GameServer)
            .Include(cl => cl.Player)
            .AsNoTracking()
            .AsQueryable();

        // Calculate total count before applying filtering
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Apply filtering
        var filteredQuery = ApplyFilter(baseQuery, gameType, gameServerId, playerId, filterString, lockedOnly);
        var filteredCount = await filteredQuery.CountAsync(cancellationToken);

        // Apply ordering and pagination
        var orderedQuery = ApplyOrderAndLimits(filteredQuery, skipEntries, takeEntries, order);
        var results = await orderedQuery.ToListAsync(cancellationToken);

        var entries = results.Select(cm => cm.ToDto()).ToList();

        var data = new CollectionModel<ChatMessageDto>(entries);

        return new ApiResponse<CollectionModel<ChatMessageDto>>(data)
        {
            Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
        }.ToApiResult();
    }

    /// <summary>
    /// Creates a new chat message in the repository.
    /// </summary>
    /// <param name="createChatMessageDto">The chat message data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the success of the operation.</returns>
    async Task<ApiResult> IChatMessagesApi.CreateChatMessage(CreateChatMessageDto createChatMessageDto, CancellationToken cancellationToken)
    {
        var chatMessage = createChatMessageDto.ToEntity();

        context.ChatMessages.Add(chatMessage);
        await context.SaveChangesAsync(cancellationToken);

        return new ApiResponse().ToApiResult(HttpStatusCode.Created);
    }

    /// <summary>
    /// Creates multiple chat messages in the repository.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A result indicating the success of the operation.</returns>
    [HttpPost("chat-messages")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    /// <summary>
    /// Sets the locked status of a chat message (idempotent).
    /// </summary>
    /// <param name="chatMessageId">The unique identifier of the chat message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating the success of the operation.</returns>
    [HttpPatch("chat-messages/{chatMessageId:guid}/lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetLock(Guid chatMessageId, CancellationToken cancellationToken = default)
    {
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        bool? locked = null;
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var anon = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);
                if (anon != null && anon.TryGetValue("locked", out var v) && v != null)
                {
                    if (v is bool b)
                        locked = b;
                    else if (bool.TryParse(v.ToString(), out var parsed))
                        locked = parsed;
                }
            }
            catch { }
        }

        if (locked == null)
            return new ApiResponse().ToBadRequestResult().ToHttpResult();

        var response = await ((IChatMessagesApi)this).SetLock(chatMessageId, locked.Value, cancellationToken);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Toggles the locked status of a chat message.
    /// </summary>
    /// <param name="chatMessageId">The unique identifier of the chat message to toggle.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the success of the operation.</returns>
    async Task<ApiResult> IChatMessagesApi.SetLock(Guid chatMessageId, bool locked, CancellationToken cancellationToken)
    {
        var chatMessage = await context.ChatMessages
            .FirstOrDefaultAsync(cm => cm.ChatMessageId == chatMessageId, cancellationToken);

        if (chatMessage == null)
            return new ApiResult(HttpStatusCode.NotFound);
        chatMessage.Locked = locked;

        await context.SaveChangesAsync(cancellationToken);

        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Creates multiple chat messages in the repository.
    /// </summary>
    /// <param name="createChatMessageDtos">The collection of chat message data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the success of the operation.</returns>
    async Task<ApiResult> IChatMessagesApi.CreateChatMessages(List<CreateChatMessageDto> createChatMessageDtos, CancellationToken cancellationToken)
    {
        var chatLogs = createChatMessageDtos.Select(cm => cm.ToEntity()).ToList();

        context.ChatMessages.AddRange(chatLogs);
        await context.SaveChangesAsync(cancellationToken);

        return new ApiResponse().ToApiResult(HttpStatusCode.Created);
    }

    /// <summary>
    /// Applies filtering criteria to the chat messages query.
    /// </summary>
    /// <param name="query">The base query to apply filters to.</param>
    /// <param name="gameType">Optional filter by game type.</param>
    /// <param name="gameServerId">Optional filter by game server identifier.</param>
    /// <param name="playerId">Optional filter by player identifier.</param>
    /// <param name="filterString">Optional filter string for message content.</param>
    /// <param name="lockedOnly">Optional filter to show only locked messages.</param>
    /// <returns>The filtered query.</returns>
    private IQueryable<ChatMessage> ApplyFilter(IQueryable<ChatMessage> query, GameType? gameType, Guid? gameServerId, Guid? playerId, string? filterString, bool? lockedOnly = null)
    {
        if (gameType.HasValue)
            query = query.Where(cl => cl.GameServer.GameType == gameType.Value.ToGameTypeInt());

        if (gameServerId.HasValue)
            query = query.Where(cl => cl.GameServerId == gameServerId);

        if (playerId.HasValue)
            query = query.Where(cl => cl.PlayerId == playerId);

        if (!string.IsNullOrWhiteSpace(filterString))
        {
            var term = filterString.Trim();

            // For extremely short terms, avoid full-text (likely ignored due to noise word / min length) and use prefix LIKE for some selectivity.
            if (term.Length < 3)
            {
                var like = $"{term}%";
                query = query.Where(m => (m.Message != null && EF.Functions.Like(m.Message, like)) || (m.Username != null && EF.Functions.Like(m.Username, like)));
            }
            else
            {
                // Full-text search over Username + Message (index created in FullTextIndexes.sql)
                var sanitized = term.Replace("\"", "\"\"");
                var ftSearch = $"\"{sanitized}*\""; // prefix search for term

                // Use CONTAINSTABLE to get ranked matches, then join back to ChatMessages. We ignore rank for now (could project later).
                var ftQuery = context.ChatMessages
                    .FromSqlInterpolated($@"SELECT c.*
FROM CONTAINSTABLE(ChatMessages, (Username, Message), {ftSearch}) ft
JOIN ChatMessages c ON c.ChatMessageId = ft.[Key]")
                    .AsNoTracking();

                // Reapply includes (allowed after FromSql).
                ftQuery = ftQuery
                    .Include(cl => cl.GameServer)
                    .Include(cl => cl.Player);

                // Intersect existing filters (already applied above) with full-text result set via inner join semantics.
                // We achieve this by applying any prior filters BEFORE this block (current code order ensures that) then replacing query.
                // To ensure intersection, we filter ftQuery by the keys present in current 'query'. For most cases 'query' still refers to base with prior filters.
                // Optimize: if no previous filters were applied (query == base), we can just take ftQuery directly.
                // Detect if filters applied by comparing expression tree complexity isn't trivial; accept small overhead of Contains when prior filters exist.
                // Simpler: project filtered IDs from current query then constrain ftQuery.
                var currentIds = query.Select(q => q.ChatMessageId);
                query = ftQuery.Where(cm => currentIds.Contains(cm.ChatMessageId));
            }
        }

        if (lockedOnly.HasValue && lockedOnly.Value)
            query = query.Where(m => m.Locked);

        return query;
    }

    /// <summary>
    /// Applies ordering and pagination to the chat messages query.
    /// </summary>
    /// <param name="query">The query to apply ordering and pagination to.</param>
    /// <param name="skipEntries">Number of entries to skip for pagination.</param>
    /// <param name="takeEntries">Number of entries to take for pagination.</param>
    /// <param name="order">Optional ordering for the results.</param>
    /// <returns>The ordered and paginated query.</returns>
    private IQueryable<ChatMessage> ApplyOrderAndLimits(IQueryable<ChatMessage> query, int skipEntries, int takeEntries, ChatMessageOrder? order)
    {
        var orderedQuery = order switch
        {
            ChatMessageOrder.TimestampAsc => query.OrderBy(cl => cl.Timestamp),
            ChatMessageOrder.TimestampDesc => query.OrderByDescending(cl => cl.Timestamp),
            _ => query.OrderByDescending(cl => cl.Timestamp)
        };

        return orderedQuery.Skip(skipEntries).Take(takeEntries);
    }
}

