using System.Net;
using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.GameServers;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

/// <summary>
/// Controller for managing game server events operations.
/// </summary>
[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public class GameServersEventsController : ControllerBase, IGameServersEventsApi
{
    private readonly PortalDbContext context;

    /// <summary>
    /// Initializes a new instance of the GameServersEventsController.
    /// </summary>
    /// <param name="context">The database context for portal operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public GameServersEventsController(PortalDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
            this.context = context;
    }

    /// <summary>
    /// Retrieves paginated game server events with optional filtering.
    /// </summary>
    /// <param name="gameType">Optional game type filter.</param>
    /// <param name="gameServerId">Optional game server ID filter.</param>
    /// <param name="eventType">Optional event type filter.</param>
    /// <param name="skipEntries">Number of entries to skip.</param>
    /// <param name="takeEntries">Number of entries to take.</param>
    /// <param name="order">Sort order for results.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A paginated collection of game server events.</returns>
    [HttpGet("game-server-events")]
    [ProducesResponseType<CollectionModel<GameServerEventDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGameServerEvents(
        [FromQuery] GameType? gameType = null,
        [FromQuery] Guid? gameServerId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] int skipEntries = 0,
        [FromQuery] int takeEntries = 20,
        [FromQuery] GameServerEventOrder? order = null,
        CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersEventsApi)this).GetGameServerEvents(gameType, gameServerId, eventType, skipEntries, takeEntries, order, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    async Task<ApiResult<CollectionModel<GameServerEventDto>>> IGameServersEventsApi.GetGameServerEvents(
        GameType? gameType,
        Guid? gameServerId,
        string? eventType,
        int skipEntries,
        int takeEntries,
        GameServerEventOrder? order,
        CancellationToken cancellationToken)
    {
        var baseQuery = context.GameServerEvents.AsNoTracking();

        var hasFilter = gameType.HasValue || gameServerId.HasValue || !string.IsNullOrWhiteSpace(eventType);

        var filteredQuery = ApplyFilter(baseQuery, gameType, gameServerId, eventType);
        var filteredCount = await filteredQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        // When no filter is applied the filtered count equals the total count, so avoid a
        // second full-table COUNT(*) (which is expensive on large GameServerEvents tables).
        var totalCount = hasFilter
            ? await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false)
            : filteredCount;

        var dataQuery = ApplyFilter(
            context.GameServerEvents.Include(gse => gse.GameServer).AsNoTracking(),
            gameType, gameServerId, eventType);

        var orderedQuery = ApplyOrderAndLimits(dataQuery, skipEntries, takeEntries, order);
        var results = await orderedQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

        var entries = results.Select(gse => gse.ToDto()).ToList();
        var data = new CollectionModel<GameServerEventDto>(entries);

        return new ApiResponse<CollectionModel<GameServerEventDto>>(data)
        {
            Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
        }.ToApiResult();
    }

    /// <summary>
    /// Creates a single game server event.
    /// </summary>
    /// <param name="createGameServerEventDto">The game server event data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A success response indicating the game server event was created.</returns>
    [HttpPost("game-server-events/single")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGameServerEvent([FromBody] CreateGameServerEventDto createGameServerEventDto, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersEventsApi)this).CreateGameServerEvent(createGameServerEventDto, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Creates a single game server event.
    /// </summary>
    /// <param name="createGameServerEventDto">The game server event data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the game server event was created.</returns>
    async Task<ApiResult> IGameServersEventsApi.CreateGameServerEvent(CreateGameServerEventDto createGameServerEventDto, CancellationToken cancellationToken)
    {
        var gameServerEvent = createGameServerEventDto.ToEntity();
        gameServerEvent.Timestamp = DateTime.UtcNow;

        context.GameServerEvents.Add(gameServerEvent);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ApiResponse().ToApiResult(HttpStatusCode.Created);
    }

    /// <summary>
    /// Creates multiple game server events in a batch operation.
    /// </summary>
    /// <param name="createGameServerEventDtos">The list of game server event data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A success response indicating the game server events were created.</returns>
    [HttpPost("game-server-events")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGameServerEvents([FromBody] List<CreateGameServerEventDto> createGameServerEventDtos, CancellationToken cancellationToken = default)
    {
        var response = await ((IGameServersEventsApi)this).CreateGameServerEvents(createGameServerEventDtos, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Creates multiple game server events in a batch operation.
    /// </summary>
    /// <param name="createGameServerEventDtos">The list of game server event data to create.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the game server events were created.</returns>
    async Task<ApiResult> IGameServersEventsApi.CreateGameServerEvents(List<CreateGameServerEventDto> createGameServerEventDtos, CancellationToken cancellationToken)
    {
        var currentTimestamp = DateTime.UtcNow;
        var gameServerEvents = createGameServerEventDtos.Select(dto =>
        {
            var gameServerEvent = dto.ToEntity();
            gameServerEvent.Timestamp = currentTimestamp;
            return gameServerEvent;
        }).ToList();

        await context.GameServerEvents.AddRangeAsync(gameServerEvents, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ApiResponse().ToApiResult(HttpStatusCode.Created);
    }

    private static IQueryable<GameServerEvent> ApplyFilter(IQueryable<GameServerEvent> query, GameType? gameType, Guid? gameServerId, string? eventType)
    {
        if (gameType.HasValue)
            query = query.Where(gse => gse.GameServer.GameType == gameType.Value.ToGameTypeInt());

        if (gameServerId.HasValue)
            query = query.Where(gse => gse.GameServerId == gameServerId);

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(gse => gse.EventType == eventType);

        return query;
    }

    private static IQueryable<GameServerEvent> ApplyOrderAndLimits(IQueryable<GameServerEvent> query, int skipEntries, int takeEntries, GameServerEventOrder? order)
    {
        skipEntries = Math.Max(0, skipEntries);
        takeEntries = Math.Clamp(takeEntries, 1, 100);

        var orderedQuery = order switch
        {
            GameServerEventOrder.TimestampAsc => query.OrderBy(gse => gse.Timestamp),
            GameServerEventOrder.TimestampDesc => query.OrderByDescending(gse => gse.Timestamp),
            _ => query.OrderByDescending(gse => gse.Timestamp)
        };

        return orderedQuery.Skip(skipEntries).Take(takeEntries);
    }
}

